using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using fall_core;

namespace fall_console
{
    class ConsoleMain
    {
        public static void Main(string[] args)
        {
            ConsoleUI ui = ConsoleUI.GetInstance();
            if (args.Length == 0)
            {
                ui.SendMessage("请输入下载地址!");
            }
            else
            {
                String remoteUrl = args[0];
                String localFilePath = Utils.GetFileNameFromUri(remoteUrl);
                DownloadTaskAnalyzer factory = new DownloadTaskAnalyzer();
                DownloadTask task = factory.create(remoteUrl, localFilePath);
                if (task != null)
                {
                    task.BindTaskListener(new ConsoleTaskListener() { });
                    task.Start();
                }
                else
                {
                    ui.SendMessage("不能处理这个url!");
                }
            }
            ui.SendMessage("按任意键退出!");
            Console.ReadKey();
        }
    }

    class ConsoleTaskListener : TaskListener
    {

        private ConsoleUI ui = ConsoleUI.GetInstance();

        public void OnError(DownloadTask task, DownloadError exception)
        {
            Console.Error.WriteLine("Job Error - {0}!", exception.Message);
        }

        public void OnDone(DownloadTask task)
        {
            Console.WriteLine("Job {0} Done!\nMD5: {1}", task.GetLocalFile(), Utils.GetFileMD5(task.GetLocalFile()));
        }

        private object consoleLock = new object();
        public void OnProcessUpdate(DownloadTask task)
        {
            ui.Refresh(task);
        }
    }
}
