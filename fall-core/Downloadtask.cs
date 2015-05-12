using System;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text.RegularExpressions;

namespace fall_core
{
    public interface DownloadTask
    {
        string GetProtocol();
        double Process{get;}
        void Create(string remoteUrl, string localFilePath);
        void BindTaskListener(TaskListener listener);
        void Start();
        void Pause();
        void Resume();
        void Destroy();
        bool Running { get; }
        string RemoteURL { get; }
        string LocalFile { get; }
        long FinishedSize { get; }
        long TotalSize { get; }
        double Speed { get; }
        DateTime AddTime { get; }
    }

    public interface TaskListener
    {
        void OnError(DownloadTask task, DownloadError exception);
        void OnDone(DownloadTask task);
        void OnProcessUpdate(DownloadTask task);
    }

    public class EmptyTaskListener : TaskListener
    {
        public void OnError(DownloadTask task, DownloadError exception) { }
        public void OnDone(DownloadTask task) { }
        public void OnProcessUpdate(DownloadTask task) { }
    }

    abstract class AbstractDownloadTask : DownloadTask
    {
        private DateTime addTime;
        private String _remoteUrl;
        private String localFilePath;
        private TaskListener listener = new EmptyTaskListener();
        private SpeedSampler speedSamper = new SpeedSampler();
        public void Create(String remoteUrl, String localFilePath)
        {
            this._remoteUrl = remoteUrl;
            this.localFilePath = localFilePath;
            this.addTime = DateTime.Now;
        }
        public String RemoteURL { get { return this._remoteUrl; } }
        public String LocalFile
        {
            get
            {
                return this.localFilePath;
            }
        }
        public DateTime AddTime
        {
            get
            {
                return this.addTime;
            }
        }
        public void BindTaskListener(TaskListener listener)
        {
            this.listener = listener;
        }

        private double lastRecordProcess = -1;
        protected void NotifyProcessUpdate()
        {
            speedSamper.RecordNow(this.FinishedSize);
            if (this.Process > lastRecordProcess)
            {
                lastRecordProcess = this.Process;
                this.listener.OnProcessUpdate(this);
            }
        }

        protected void NotifyDone()
        {
            this.listener.OnDone(this);
        }

        protected void NotifyError(DownloadError e)
        {
            this.listener.OnError(this, e);
        }

        public double Speed
        {
            get
            {
                return this.Process == 100.0 ? 0 : speedSamper.getSpeedIn(50000);
            }
        }

        abstract public String GetProtocol();
        abstract public double Process { get; }
        abstract public void Start();
        abstract public void Pause();
        abstract public void Resume();
        abstract public void Destroy();
        abstract public bool Running { get; }
        abstract public long FinishedSize { get; }
        abstract public long TotalSize { get; }
    }

    public class DownloadTaskAnalyzer
    {

        public DownloadTask create(FileLink link, String localFilePath)
        {
            return create(link.Link, localFilePath);
        }

        public DownloadTask create(String remoteUrl, String localFilePath)
        {
            if (remoteUrl.StartsWith("http://"))
            {
                DownloadTask task = new MultiThreadHttpTask();
                task.Create(remoteUrl, localFilePath);
                return DownloadTaskProxy.Create(task);
            }
            return null;
        }
    }

    public class FileLink
    {

        private string _link;

        private string _name;

        public FileLink(string link)
        {
            this._link = link;
            this._name = Utils.GetFileNameFromUri(link);
            if (_link.StartsWith("http://"))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string fileDes = response.Headers.Get("content-disposition");
                if (null != fileDes)
                {
                    string pattern = @"attachment; filename=(.+)";
                    MatchCollection matches = Regex.Matches(fileDes, pattern, RegexOptions.IgnoreCase);
                    if (matches.Count == 1)
                    {
                        this._name = matches[0].Groups[1].Value;
                    }
                }
                response.Close();
            }
        }

        public string FileName { get { return this._name; } }

        public string Link { get { return this._link; } }
    }

    public class DownloadTaskProxy : RealProxy
    {
        private DownloadTask proxy;
        private TaskListener listener = new EmptyTaskListener();
        private DownloadTaskProxy(DownloadTask instance)
            : base(typeof(DownloadTask))
        {
            proxy = instance;
        }

        public static DownloadTask Create(DownloadTask instance)
        {
            return (DownloadTask)new DownloadTaskProxy(instance).GetTransparentProxy();
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = (IMethodCallMessage)msg;
            var method = (MethodInfo)methodCall.MethodBase;

            try
            {
                if (method.Name.Equals("BindTaskListener") && methodCall.ArgCount == 1 && (methodCall.Args[0] is TaskListener))
                {
                    listener = (TaskListener)methodCall.Args[0];
                }
                var result = method.Invoke(proxy, methodCall.InArgs);
                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException && e.InnerException != null)
                {
                    listener.OnError(DownloadTaskProxy.Create(proxy), new DownloadError(e.InnerException.Message, e.InnerException));
                    return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
                }

                return new ReturnMessage(e, msg as IMethodCallMessage);
            }
        }

    }
}
