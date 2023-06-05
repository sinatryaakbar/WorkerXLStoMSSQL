using XLStoMSSQL.Net;
using XLStoMSSQL.Net.Interfaces;
using XLStoMSSQL.Net.Models;
using XLStoMSSQL.Net.Services;
using Serilog;
using Serilog.Events;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string CurrentAppPath = Path.Combine(Directory.GetCurrentDirectory(), "SeriLogger");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(CurrentAppPath)
            .CreateLogger();

        try
        {
            IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Problem on starting up services!");
            return;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<IWork, WorkService>();
            services.AddSingleton<IUpgradeDB, UpgradeDBService>();

            var connectionStrings = new ConnectionStrings();
            hostContext.Configuration.Bind(nameof(ConnectionStrings), connectionStrings);
            XLStoMSSQL.Net.Utils.DataService.ConnectionString = connectionStrings.DefaultConnection;
            services.AddSingleton(connectionStrings);

            var workerSettings = new WorkerSettings();
            hostContext.Configuration.Bind(nameof(WorkerSettings), workerSettings);
            services.AddSingleton(workerSettings);

            services.AddHostedService<Worker>();
        });
}