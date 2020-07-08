﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Configuration
{
    public class SecurityDescriptorTarget
    {
        public string Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TargetType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorizationMode AuthorizationMode { get; set; } = AuthorizationMode.SecurityDescriptor;

        public string SecurityDescriptor { get; set; } = "O:SYD:";

        public string Script { get; set; }

        public SecurityDescriptorTargetJitDetails Jit { get; set; } = new SecurityDescriptorTargetJitDetails();

        public SecurityDescriptorTargetLapsDetails Laps { get; set; } = new SecurityDescriptorTargetLapsDetails();

        public AuditNotificationChannels Notifications { get; set; } = new AuditNotificationChannels();
    }
}