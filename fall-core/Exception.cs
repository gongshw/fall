using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fall_core
{
    public class DownloadWarning : Exception
    {
        public DownloadWarning(String msg)
            : base(msg)
        {

        }

        public DownloadWarning(String msg, Exception e)
            : base(msg, e)
        {

        }
    }

    public class DownloadError : Exception
    {
        public DownloadError(String msg)
            : base(msg)
        {

        }
        public DownloadError(String msg, Exception e)
            : base(msg, e)
        {

        }
    }
}
