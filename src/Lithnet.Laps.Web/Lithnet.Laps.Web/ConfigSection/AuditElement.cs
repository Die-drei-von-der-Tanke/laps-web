﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web
{
    public class AuditElement : ConfigurationElement
    {
        private const string PropNotifySuccess = "emailOnSuccess";
        private const string PropNotifyFailure = "emailOnFailure";

        private const string PropEmailAddresses = "emailAddresses";

        [ConfigurationProperty(PropNotifySuccess, IsRequired = false, DefaultValue = true)]
        public bool NotifySuccess => (bool) this[PropNotifySuccess];

        [ConfigurationProperty(PropNotifyFailure, IsRequired = false, DefaultValue = true)]
        public bool NotifyFailure => (bool)this[PropNotifyFailure];
 
        [ConfigurationProperty(PropEmailAddresses, IsRequired = false)]
        public string EmailAddresses => (string) this[PropEmailAddresses];
    }
}