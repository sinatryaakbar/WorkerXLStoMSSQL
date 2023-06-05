using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLStoMSSQL.Net.Models
{
    public class WorkerSettings
    {
        public int DelayMilliseconds { get; set; }
        public string TargetDirectory { get; set; } = string.Empty;
    }

    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
