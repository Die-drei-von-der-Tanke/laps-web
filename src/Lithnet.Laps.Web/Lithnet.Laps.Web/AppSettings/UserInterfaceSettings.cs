﻿using Lithnet.Laps.Web.Internal;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class UserInterfaceSettings : IUserInterfaceSettings
    {
        private readonly IConfiguration configuration;

        public UserInterfaceSettings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Title => this.configuration["user-interface:title"] ?? "Lithnet LAPS Web App";

        public AuditReasonFieldState UserSuppliedReason => this.configuration.GetValueOrDefault("user-interface:user-supplied-reason", AuditReasonFieldState.Optional);
    }
}