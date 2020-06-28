﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Lithnet.AccessManager.Configuration;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class ApplicationConfig
    {
        public HostingOptions Hosting { get; set; }

        public AuthenticationOptions Authentication { get; set; }

        public AuditOptions Auditing { get; set; }

        public EmailOptions Email { get; set; }

        public RateLimitOptions RateLimits { get; set; }

        public UserInterfaceOptions UserInterface { get; set; }

        public void Save(string file)
        {
            string data = JsonConvert.SerializeObject(this);
            File.WriteAllText(file, data);
        }

        public static ApplicationConfig Load(string file)
        {
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<ApplicationConfig>(data);
        }
    }
}
