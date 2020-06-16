using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddTransient<IDirectory, ActiveDirectory>();
                    services.AddTransient<ISettingsProvider, RegistrySettingsProvider>();
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                        builder.AddNLog("nlog.config");
                    });
                }
              ).UseWindowsService();
        }
    }
}