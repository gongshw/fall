using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fall_core
{

    class BlockInfo
    {
        public long totalSize;
        public long count;

    }

    class TaskBlock
    {
        private const int BLOCK_STRUCT_SIZE = 17;
        public int id;
        public int size;
        public long offset;
        public bool done;

        public byte[] ToBytes()
        {
            byte[] buf = new byte[BLOCK_STRUCT_SIZE];
            Array.Copy(BitConverter.GetBytes(id), 0, buf, 0, 4);
            Array.Copy(BitConverter.GetBytes(size), 0, buf, 4, 4);
            Array.Copy(BitConverter.GetBytes(offset), 0, buf, 8, 8);
            Array.Copy(BitConverter.GetBytes(done), 0, buf, 16, 1);
            return buf;
        }

        public void Save(string path, object lockObject)
        {
            lock (lockObject)
            {
                using (Stream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    stream.Seek(this.id * BLOCK_STRUCT_SIZE, SeekOrigin.Begin);
                    stream.Write(this.ToBytes(), 0, BLOCK_STRUCT_SIZE);
                }
            }
        }

        public void FromBytes(byte[] buf)
        {
            Debug.Assert(buf.Length == BLOCK_STRUCT_SIZE);
            this.id = BitConverter.ToInt32(buf, 0);
            this.size = BitConverter.ToInt32(buf, 4);
            this.offset = BitConverter.ToInt64(buf, 8);
            this.done = BitConverter.ToBoolean(buf, 16);
        }

        public static TaskBlock[] Read(string path)
        {
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] buf = new byte[BLOCK_STRUCT_SIZE];
            int readCount;
            List<TaskBlock> blocks = new List<TaskBlock>();
            while ((readCount = stream.Read(buf, 0, BLOCK_STRUCT_SIZE)) > 0)
            {
                if (readCount == BLOCK_STRUCT_SIZE)
                {
                    TaskBlock block = new TaskBlock();
                    block.FromBytes(buf);
                    blocks.Add(block);
                }
                else
                {
                    //TODO specify error
                    throw new ApplicationException();
                }
            }
            stream.Close();
            return blocks.ToArray();
        }

    }

    class MultiThreadHttpTask : AbstractDownloadTask
    {

        Logger LOG = Logger.get("MultiThreadHttpTask");

        private BlockInfo blockInfo = new BlockInfo();

        private int maxBlockSize = Utils.GetIntConfig("MaxBlockSize", 2 * 1024 * 1024);

        private ConcurrentQueue<TaskBlock> blockQueue = new ConcurrentQueue<TaskBlock>();

        volatile private bool running = false;

        private int finishedBlocks = 0;

        private long finishedSize = 0;

        private Stream fileOutStream;

        private object fallFileLock = new object();

        override public string GetProtocol()
        {
            return "http";
        }

        override public double GetProcess()
        {
            return this.finishedSize / (double)this.blockInfo.totalSize;
        }

        override public void Start()
        {
            lock (this)
            {
                if (!TryRestoreBlockInfo())
                {
                    Init();
                }
            }
            this.Resume();
        }

        override public void Pause()
        {
            throw new NotImplementedException();
        }

        public override void Resume()
        {
            lock (this)
            {
                if (running)
                {
                    return;
                }
                else
                {
                    this.running = true;
                }
            }
            this.fileOutStream = File.Open(this.GetLocalFile(), FileMode.Open);
            List<Thread> threads = new List<Thread>();
            int threadCount = Math.Min((int)blockInfo.count, Utils.GetIntConfig("MaxThreadNum", 5));
            for (int i = 0; i < threadCount; i++)
            {
                Thread t = new Thread(new ThreadStart(TheadTask));
                t.Start();
                threads.Add(t);
            }
            LOG.log("{0} threads start!", threadCount);
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            this.fileOutStream.Close();
            File.Delete(GetFallFilepath());
            this.NotifyDone();
        }

        override public void Destroy()
        {
            throw new NotImplementedException();
        }

        public override bool Running
        {
            get { return this.running; }
        }

        private void Init()
        {
            //分析任务大小
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.GetRemoteURL());
            request.AddRange(0);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            LOG.logMap(response.Headers);
            blockInfo.totalSize = response.ContentLength;
            long maxBlockSize = 0;
            if (!"bytes".Equals(response.Headers.Get("Accept-Ranges"))
                || !response.StatusCode.Equals(HttpStatusCode.PartialContent))
            {
                blockInfo.count = 1;
                maxBlockSize = blockInfo.totalSize;
            }
            else
            {
                blockInfo.count = (long)Math.Ceiling(blockInfo.totalSize / (double)this.maxBlockSize);
                maxBlockSize = this.maxBlockSize;
            }
            TaskBlock[] blocks = new TaskBlock[blockInfo.count];
            fileOutStream = new FileStream(GetLocalFile(), FileMode.Create, FileAccess.Write, FileShare.Read);
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = new TaskBlock();
                blocks[i].id = i;
                blocks[i].done = false;
                blocks[i].offset = maxBlockSize * i;
                blocks[i].size = (int)((i == blocks.Length - 1) ? (blockInfo.totalSize - blocks[i].offset) : maxBlockSize);
                blockQueue.Enqueue(blocks[i]);
                fileOutStream.Write(new byte[blocks[i].size], 0, blocks[i].size);
                blocks[i].Save(GetFallFilepath(), fallFileLock);
            }
            fileOutStream.Close();
            response.Close();
        }

        private bool TryRestoreBlockInfo()
        {
            FileInfo f = new FileInfo(GetFallFilepath());

            if (f.Exists)
            {
                TaskBlock[] blocks = TaskBlock.Read(GetFallFilepath());
                foreach (TaskBlock block in blocks)
                {
                    this.blockInfo.count += 1;
                    this.blockInfo.totalSize += block.size;
                    if (block.done)
                    {
                        this.finishedBlocks += 1;
                        this.finishedSize += block.size;
                    }
                    else
                    {
                        blockQueue.Enqueue(block);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TheadTask()
        {
            while (running && finishedBlocks < blockInfo.count)
            {
                TaskBlock block;
                blockQueue.TryDequeue(out block);
                if (block != null && !block.done)
                {
                    try
                    {
                        this.DownloadBlock(block);
                        block.done = true;
                        block.Save(GetFallFilepath(), fallFileLock);
                    }
                    catch (WebException e)
                    {
                        LOG.log("error - {0}", e.Message);
                        blockQueue.Enqueue(block);
                    }
                }
                else
                {
                    Thread.Sleep(100);//sleep 100ms to avoid high cpu usage
                }
            }
        }

        private string GetFallFilepath()
        {
            return this.GetLocalFile() + Utils.GetStringConfig("FallFileExtension", ".fall");
        }

        private void DownloadBlock(TaskBlock block)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.GetRemoteURL());
            if (blockInfo.count != 1)
            {
                request.AddRange(block.offset);
            }
            //request.AddRange(block.offset, block.offset + block.size);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                byte[] buf = new byte[block.size];
                int readCount = 0;
                int thisRead = 0;
                do
                {
                    try
                    {
                        thisRead = response.GetResponseStream().Read(buf, readCount, block.size - readCount);
                        readCount += thisRead;
                    }
                    catch (WebException e)
                    {
                        finishedSize -= readCount;
                        throw e;
                    }
                    lock (this)
                    {
                        finishedSize += thisRead;
                    }
                    LOG.log("{0:F5}%", this.GetProcess() * 100);
                    this.NotifyProcessUpdate();
                } while (thisRead != 0);

                if (readCount != block.size)
                {
                    byte[] result = new byte[readCount];
                    Array.Copy(buf, 0, result, 0, readCount);
                    LOG.log("下载错误!" + System.Text.Encoding.UTF8.GetString(result));
                    throw new System.ApplicationException();
                }
                lock (this)
                {
                    fileOutStream.Seek(block.offset, SeekOrigin.Begin);
                    fileOutStream.Write(buf, 0, block.size);
                    finishedBlocks++;
                }
            }
        }

        public override long FinishedSize
        {
            get { return finishedSize; }
        }

        public override long TotalSize
        {
            get { return blockInfo.totalSize; }
        }
    }
}
