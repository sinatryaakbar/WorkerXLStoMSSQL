using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLStoMSSQL.Net.Models
{
    public class DataLog
    {
        public int Id { get; set; }
        public string? Log_Code { get; set; }
        public string? Log_Message { get; set; }
        public string? Log_Type { get; set; }
        public DateTime Log_Date { get; set; }
        public string? App_Name { get; set; }
    }

    public class TempDataLog : DataLog
    {
        public string FileName { get; set; } = string.Empty;
        public string PathLocation { get; set; } = string.Empty;
        public int StatusProcess { get; set; }
        public DateTime UploadTime { get; set; }
        public DateTime InProcessTime { get; set; }
        public DateTime FinishTime { get; set; }
    }

}
