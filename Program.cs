using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NLog.Web;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Dataflow.SaicEnergyTracker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
         .ConfigureAppConfiguration((hostingContext, configureDelagate) =>
         {
             configureDelagate.AddJsonFile($"appsettings.secrets.json", optional: true, reloadOnChange: true);
         })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole().AddDebug().AddAzureWebAppDiagnostics();
            })
            .UseNLog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseIISIntegration()
                .UseStartup<Startup>();
            });
    }
}
