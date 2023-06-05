using System.Threading;
using XLStoMSSQL.Net.Interfaces;
using XLStoMSSQL.Net.Models;
using XLStoMSSQL.Net.Utils;

namespace XLStoMSSQL.Net
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerSettings _settings;
        private ConnectionStrings _constr;
        private readonly IWork _work;
        private readonly IUpgradeDB _upgradeDB;
        public Worker(ILogger<Worker> logger,
                      WorkerSettings settings,
                      ConnectionStrings constr,
                      IWork work,
                      IUpgradeDB upgradeDB)
        {
            _logger = logger;
            _settings = settings;
            _constr = constr;
            _work = work;
            _upgradeDB = upgradeDB;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            DataService.ConnectionString = _constr.DefaultConnection;
            
            string CurrentAppPath = Path.Combine(Directory.GetCurrentDirectory() , "Logger");
            if (string.IsNullOrEmpty(_settings.TargetDirectory))
            {
                _logger.LogWarning("Default TargetDirectory Is Empty! Directory will be redirect to {1}", CurrentAppPath);
                _settings.TargetDirectory = CurrentAppPath;
            }
            _upgradeDB.Execute();

            _logger.LogInformation("Worker starting up...: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker has been stopped...: {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker prepare to begin: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                bool CheckConn = false;
                while (!CheckConn)
                    CheckConn = await CheckConnection(stoppingToken);
                
                _logger.LogInformation("Worker begin executing at: {time}", DateTimeOffset.Now);
                await _work.UploadTemp();
                await _work.SubmitTemp();
                await Task.Delay(_settings.DelayMilliseconds, stoppingToken);
            }

            _logger.LogInformation("Worker end executing at: {time}", DateTimeOffset.Now);
        }

        private async Task<bool> CheckConnection(CancellationToken stoppingToken)
        {
            bool CheckConn = false;
            try
            {
                if (!string.IsNullOrEmpty(_constr.DefaultConnection))
                {
                    _logger.LogInformation("test database connection: {time}", DateTimeOffset.Now);
                    CheckConn = await DataService.CheckConnectionAsync();
                }
                else
                {
                    _logger.LogError("Connection string is empty: {time}", DateTimeOffset.Now);
                    CheckConn = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect database: {time}", DateTimeOffset.Now);
                CheckConn = false;
            }
            if (!CheckConn)
            {
                _logger.LogError("Worker failed to begin. check your connectionstring on appsetting.json: {time}", DateTimeOffset.Now);
                await Task.Delay(_settings.DelayMilliseconds, stoppingToken);
            }

            return CheckConn;
        }
    }
}