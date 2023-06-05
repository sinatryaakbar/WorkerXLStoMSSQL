using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using XLStoMSSQL.Net.Interfaces;
using XLStoMSSQL.Net.Models;
using System.IO;
using OfficeOpenXml;
using static System.Net.WebRequestMethods;
using XLStoMSSQL.Net.Utils;
using System.Security.AccessControl;
using System.Net;

namespace XLStoMSSQL.Net.Services
{
    public class WorkService : IWork
    {
        private readonly ILogger<Worker> _logger;
        private WorkerSettings _settings;

        public WorkService(ILogger<Worker> logger,
            WorkerSettings settings) 
        {
            _logger = logger;
            _settings = settings;
        }
        public async Task<bool> UploadTemp()
        {
            string TargetFolder = _settings.TargetDirectory;
            var DirTarget = new DirectoryInfo(TargetFolder);

            if (!DirTarget.Exists)
                DirTarget.Create();

            _logger.LogInformation("Found {0} Files in {1}", DirTarget.GetFiles("*.xls*").Length, DirTarget.FullName);
            if (DirTarget.GetFiles("*.xls*").Length == 0)
                return false;

            string ProcessedFolder = Path.Combine(TargetFolder, "Processed");

            try
            {
                if (!Directory.Exists(ProcessedFolder))
                    Directory.CreateDirectory(ProcessedFolder);

                foreach (FileInfo files in DirTarget.GetFiles("*.xls*"))
                {
                    if (!files.Exists) continue;

                    var upd = await UploadExcel(files);
                    
                    if (upd) files.MoveTo(Path.Combine(ProcessedFolder, $"{files.Name}_{DateTime.Now.ToString("#yyyyMMdd#HHmmss#")}"), true);
                }
                _logger.LogInformation("Success to Upload {0} Files to TempData!", DirTarget.GetFiles("*.xls*").Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Upload Files to TempData!");
                return false;
            }

            return true;
        }

        private async Task<bool> UploadExcel(FileInfo fileInfo)
        {
            var dataLogs = new List<TempDataLog>();
            string qry = "";

            qry = @"select count(1) from TempDataLog where FileName = @FileName and PathLocation = @PathLocation";
            var CheckData = await DataService.ExecuteScalarAsync(qry, new { FileName = fileInfo.Name, PathLocation = fileInfo.FullName });
            if (CheckData == null || (int)CheckData > 0)
                return true;

            using (var excelPack = new ExcelPackage(fileInfo, true))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var currentSheet = excelPack.Workbook.Worksheets;
                var workSheet = currentSheet.First();
                var noOfCol = workSheet.Dimension.End.Column;
                var noOfRow = workSheet.Dimension.End.Row;
                for (int rowIterator = 2; rowIterator <= noOfRow; rowIterator++)
                {
                    dataLogs.Add(new TempDataLog()
                    {
                        FileName = fileInfo.Name,
                        PathLocation = fileInfo.FullName,
                        StatusProcess = 0,
                        UploadTime = DateTime.Now,

                        App_Name = workSheet.Cells[rowIterator, 1].Value?.ToString(),
                        Log_Code = workSheet.Cells[rowIterator, 2].Value?.ToString(),
                        Log_Message = workSheet.Cells[rowIterator, 3].Value?.ToString(),
                        Log_Type = workSheet.Cells[rowIterator, 4].Value?.ToString(),
                        Log_Date = DateTime.FromOADate(Convert.ToDouble(workSheet.Cells[rowIterator, 5].Value)),
                    });
                }
            }

            qry = @"Insert Into TempDataLog([FileName], PathLocation, App_Name, Log_Code, Log_Message, Log_Type, Log_Date, StatusProcess, UploadTime) values
                    (@FileName, @PathLocation, @App_Name, @Log_Code, @Log_Message, @Log_Type, @Log_Date, @StatusProcess, @UploadTime)";

            var exec = await DataService.ExecuteAsync(qry, dataLogs, true);

            return (exec > 0);
        }

        public async Task<bool> SubmitTemp()
        {
            try
            {
                string qry = "Select id, App_Name, [FileName], PathLocation, Log_Code, Log_Message, Log_Type, Log_Date, StatusProcess, UploadTime from TempDataLog where StatusProcess = 0";
                var tempDataLog = await DataService.FindListAsync<TempDataLog>(qry);
                if (tempDataLog == null || tempDataLog.Count == 0)
                {
                    _logger.LogInformation("All Data Temp Are Already Submited");
                    return true;
                }
                _logger.LogInformation("Found {0} Files in Table TempDataLog with StatusProcess = 0", tempDataLog.Count);

                var listQry = new List<DapperTransaction>();
                qry = "update TempDataLog set StatusProcess = 1 where id = @id";
                listQry.Add(new DapperTransaction { Query = qry, QueryParameter = tempDataLog });
                qry = "insert into DataLog(App_Name, Log_Code, Log_Message, Log_Type, Log_Date) values(@App_Name, @Log_Code, @Log_Message, @Log_Type, @Log_Date)";
                listQry.Add(new DapperTransaction { Query = qry, QueryParameter = tempDataLog });
                qry = "update TempDataLog set StatusProcess = 2 where id = @id";
                listQry.Add(new DapperTransaction { Query = qry, QueryParameter = tempDataLog });
                var exec = await DataService.ExecuteMultipleAsync(listQry);

                _logger.LogInformation("Success to Submit {0} Files from TempData!", tempDataLog.Count);
                return (exec > 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Submit TempData!");
                return false;
            }

        }

    }
}
