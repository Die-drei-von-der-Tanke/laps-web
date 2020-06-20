using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Test")]

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
                    services.AddTransient<IAgentSettings, AgentRegistrySettings>();
                    services.AddTransient<IJitSettings, JitRegistrySettings>();
                    services.AddTransient<IJitAgent, JitAgent>();
                    services.AddTransient<ILapsSettings, LapsRegistrySettings>();
                    services.AddTransient<ILapsAgent, LapsAgent>();
                    services.AddTransient<ILocalSam, LocalSam>();
                    services.AddTransient<IPasswordGenerator, RandomPasswordGenerator>();
                    services.AddTransient<IAppDataProvider, MsDsAppConfigurationProvider>();
                    services.AddSingleton<RNGCryptoServiceProvider>();
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddNLog("nlog.config");
                    });
                }
              ).UseWindowsService();
        }
    }
}