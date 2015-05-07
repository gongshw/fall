using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fall_core
{
    public interface DownloadTask
    {
        string GetProtocol();
        double GetProcess();
        void Create(string remoteUrl, string localFilePath);
        void BindTaskListener(TaskListener listener);
        void Start();
        void Pause();
        void Resume();
        void Destroy();
        bool Running { get; }
        string GetRemoteURL();
        string GetLocalFile();
        long FinishedSize { get; }
        long TotalSize { get; }
        double Speed { get; }
        DateTime GetAddTime();
    }

    public interface TaskListener
    {
        void OnException(DownloadTask task, Exception exception);
        void OnDone(DownloadTask task);
        void OnProcessUpdate(DownloadTask task);
    }

    public class EmptyTaskListener : TaskListener
    {
        public void OnException(DownloadTask task, Exception exception) { }
        public void OnDone(DownloadTask task) { }
        public void OnProcessUpdate(DownloadTask task) { }
    }

    abstract class AbstractDownloadTask : DownloadTask
    {
        private DateTime addTime;
        private String remoteUrl;
        private String localFilePath;
        private TaskListener listener = new EmptyTaskListener();
        private SpeedSampler speedSamper = new SpeedSampler();
        public void Create(String remoteUrl, String localFilePath)
        {
            this.remoteUrl = remoteUrl;
            this.localFilePath = localFilePath;
            this.addTime = DateTime.Now;
        }
        public String GetRemoteURL()
        {
            return this.remoteUrl;
        }
        public String GetLocalFile()
        {
            return this.localFilePath;
        }
        public DateTime GetAddTime()
        {
            return this.addTime;
        }
        public void BindTaskListener(TaskListener listener)
        {
            this.listener = listener;
        }

        private double lastRecordProcess = -1;
        protected void NotifyProcessUpdate()
        {
            speedSamper.RecordNow(this.FinishedSize);
            if (this.GetProcess() > lastRecordProcess)
            {
                lastRecordProcess = this.GetProcess();
                this.listener.OnProcessUpdate(this);
            }
        }

        protected void NotifyDone()
        {
            this.listener.OnDone(this);
        }

        protected void NotifyException(Exception e)
        {
            this.listener.OnException(this, e);
        }


        public double Speed { get { return speedSamper.getSpeedIn(50000); } }


        abstract public String GetProtocol();
        abstract public double GetProcess();
        abstract public void Start();
        abstract public void Pause();
        abstract public void Resume();
        abstract public void Destroy();
        abstract public bool Running { get; }
        abstract public long FinishedSize { get; }
        abstract public long TotalSize { get; }
    }

    public class DownloadTaskFactory
    {
        public DownloadTask create(String remoteUrl, String localFilePath)
        {
            if (remoteUrl.StartsWith("http://"))
            {
                DownloadTask task = new MultiThreadHttpTask();
                task.Create(remoteUrl, localFilePath);
                return task;
            }
            return null;
        }
    }
}
