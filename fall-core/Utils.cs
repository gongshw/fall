using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.IO;

namespace fall_core
{
    public static class Utils
    {
        public static string GetFileMD5(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty).ToLower();
                }
            }
        }

        public static string GetFileNameFromUri(string remoteUrl)
        {
            Uri uri;
            if (Uri.TryCreate(remoteUrl, UriKind.Absolute, out uri))
            {
                return System.IO.Path.GetFileName(uri.LocalPath);
            }
            else
            {
                return "";
            }
        }

        public static string FormatSize(long byteSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (byteSize >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                byteSize = byteSize / 1024;
            }
            string result = String.Format("{0:0.##} {1}", byteSize, sizes[order]);
            return result;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static int GetIntConfig(string name, int defaultValue = 0)
        {
            var value = ConfigurationManager.AppSettings[name];
            return value != null ? Int32.Parse(ConfigurationManager.AppSettings[name]) : defaultValue;
        }

        public static bool GetBoolConfig(string name, bool defaultValue = false)
        {
            var value = ConfigurationManager.AppSettings[name];
            return value != null ? Boolean.Parse(ConfigurationManager.AppSettings[name]) : defaultValue;
        }

        public static string GetStringConfig(string name, string defaultValue = "")
        {
            var value = ConfigurationManager.AppSettings[name];
            return value != null ? value : defaultValue;
        }
    }

    public class Logger
    {
        static public Logger get(string name)
        {
            return new Logger(name);
        }
        private String name;

        private Logger(string name)
        {
            this.name = name;
        }

        public void log(string message)
        {
            if (Utils.GetBoolConfig("EnableLog"))
            {
                Console.WriteLine(name + ": " + message);
            }
        }

        public void log(string message, params object[] args)
        {
            if (Utils.GetBoolConfig("EnableLog"))
            {
                Console.WriteLine(name + ": " + message, args);
            }
        }

        public void logMap(NameValueCollection map)
        {
            foreach (string key in map.AllKeys)
            {
                this.log("{0}: {1}", key, map[key]);
            }
        }
    }
}
