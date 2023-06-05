using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLStoMSSQL.Net.Interfaces;
using XLStoMSSQL.Net.Models;
using XLStoMSSQL.Net.Utils;

namespace XLStoMSSQL.Net.Services
{
    public class UpgradeDBService : IUpgradeDB
    {
        private readonly ILogger<Worker> _logger;
        public UpgradeDBService(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public void Execute()
        {
            try
            {
                var CheckDBExists = DataService.CheckDatabase();
                if (CheckDBExists)
                {
                    string sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "DB");
                    var sqlDirs = Directory.GetDirectories(sqlPath);
                    foreach (var dir in sqlDirs)
                    {
                        var files = Directory.GetFiles(dir, "*.sql");
                        foreach (var file in files)
                        {
                            try
                            {
                                _logger.LogInformation("Execute file {0}", file);
                                DataService.Execute(File.ReadAllText(file), true);
                                string archPath = file.Replace("DB", "DB_Archive");

                                if (File.Exists(archPath))
                                    File.Delete(archPath);

                                var arcDir = Path.GetDirectoryName(archPath);
                                if (!string.IsNullOrEmpty(arcDir) && !Directory.Exists(arcDir))
                                    Directory.CreateDirectory(arcDir);

                                File.Move(file, archPath);

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed execute file {0}", file);
                                throw;
                            }
                        }
                    }
                }
                else
                    throw new Exception("Database Is Not Exists or Failed to create to this server!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpgraeDB Execution Failed!");
            }
        }
    }
}
