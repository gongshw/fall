﻿using fall_core;
using System;

namespace fall_console
{
    class ConsoleUI
    {

        private object consoleLock = new object();

        private ConsoleUI() { }

        private static ConsoleUI ui;

        public static ConsoleUI GetInstance()
        {
            return ui != null ? ui : (ui = new ConsoleUI());
        }

        public void Refresh(DownloadTask task)
        {
            lock (consoleLock)
            {
                int consoleTop = Math.Max(2, Console.CursorTop);
                clearLine(0);
                clearLine(1);
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("{0} [{1}/{2}] {3:g}", task.LocalFile, Utils.FormatSize(task.FinishedSize),
                    Utils.FormatSize(task.TotalSize), (DateTime.Now - task.AddTime));
                Console.SetCursorPosition(0, 1);
                Console.WriteLine("{0:F3}% - {1}/s   ", task.Process * 100, Utils.FormatSize((long)task.Speed));
                Console.SetCursorPosition(0, consoleTop);
            }
        }

        public void clearLine(int line)
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, line);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public void SendMessage(string message)
        {
            lock (consoleLock)
            {
                Console.WriteLine(message);
            }
        }
    }
}
