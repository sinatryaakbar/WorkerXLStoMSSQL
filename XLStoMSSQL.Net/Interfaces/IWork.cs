using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLStoMSSQL.Net.Interfaces
{
    public interface IWork
    {
        Task<bool> UploadTemp();
        Task<bool> SubmitTemp();
    }
}
