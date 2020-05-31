using System;
using System.Configuration;
using Lithnet.Laps.Web.ActiveDirectory;
using Microsoft.Extensions.Configuration;
using Microsoft.Practices.Unity.Configuration;
using NLog;
using Unity;
using Unity.NLog;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Internal;
using Lithnet.Laps.Web.Authorization;

namespace Lithnet.Laps.Web
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container =
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              container.AddNewExtension<NLogExtension>();
              RegisterTypes(container);
              return container;
          });

        /// <summary>
        /// Configured Unity Container.
        /// </summary>
        public static IUnityContainer Container => container.Value;
        #endregion

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>
        /// There is no need to register concrete types such as controllers or
        /// API controllers (unless you want to change the defaults), as Unity
        /// allows resolving a concrete type even if it was not previously
        /// registered.
        /// </remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            if (((UnityConfigurationSection)ConfigurationManager.GetSection("unity"))?.Containers.Count > 0)
            {
                container.LoadConfiguration();
            }

            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("app_data/appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("app_data/appsecrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("laps")
                .Build();

            container.RegisterInstance(configRoot);

            container.RegisterFactory<ILogger>(_ => LogManager.GetCurrentClassLogger());

            // If no container registrations are found in the config file, then load 

            container.RegisterTypeIfMissing<IIwaSettings, IwaSettings>();
            container.RegisterTypeIfMissing<IOidcSettings, OidcSettings>();
            container.RegisterTypeIfMissing<IWsFedSettings, WsFedSettings>();
            container.RegisterTypeIfMissing<IUserInterfaceSettings, UserInterfaceSettings>();
            container.RegisterTypeIfMissing<IRateLimitSettings, RateLimitSettings>();
            container.RegisterTypeIfMissing<IAuthenticationSettings, AuthenticationSettings>();
            container.RegisterTypeIfMissing<IIpResolverSettings, IpResolverSettings>();
            container.RegisterTypeIfMissing<IEmailSettings, EmailSettings>();
            container.RegisterTypeIfMissing<IIpAddressResolver, IpAddressResolver>();
            container.RegisterTypeIfMissing<GlobalAuditSettings, GlobalAuditSettings>();
            container.RegisterTypeIfMissing<IJsonTargetsProvider, JsonFileTargetsProvider>();
            container.RegisterTypeIfMissing<IAuthorizationService, BuiltInAuthorizationService>();
            container.RegisterTypeIfMissing<IAuthorizationSettings, AuthorizationSettings>();
            container.RegisterTypeIfMissing<JsonTargetAuthorizationService, JsonTargetAuthorizationService>();
            container.RegisterTypeIfMissing<PowershellAuthorizationService, PowershellAuthorizationService>();
            container.RegisterTypeIfMissing<IDirectory, ActiveDirectory.ActiveDirectory>();
            container.RegisterTypeIfMissing<IAuthenticationService, AuthenticationService>();
            container.RegisterTypeIfMissing<IReporting, Reporting>();
            container.RegisterTypeIfMissing<ITemplates, TemplatesFromFiles>();
            container.RegisterTypeIfMissing<IRateLimiter, RateLimiter>();
            container.RegisterTypeIfMissing<IMailer, SmtpMailer>();
        }

        private static void RegisterTypeIfMissing<T1,T2>(this IUnityContainer container) where T2 : T1
        {
            if (!container.IsRegistered<T1>())
            {
                container.RegisterType<T1, T2>();
            }
        }
    }
}