﻿using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class AuthorizationSettings : IAuthorizationSettings
    {
        private IConfigurationRoot configuration;

        public AuthorizationSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public bool JsonProviderEnabled => this.configuration.GetValueOrDefault("authorization:json-provider:enabled", true);

        public bool PowershellProviderEnabled => this.configuration.GetValueOrDefault("authorization:powershell-provider:enabled", true);

        public string PowershellScriptFile => this.configuration["authorization:powershell-provider:script-file"];

        public string JsonAuthorizationFile => this.configuration["authorization:json-provider:authorization-file"];

        public int PowershellScriptTimeout => this.configuration.GetValueOrDefault("authorization:powershell-provider:timeout", 20);
    }
}