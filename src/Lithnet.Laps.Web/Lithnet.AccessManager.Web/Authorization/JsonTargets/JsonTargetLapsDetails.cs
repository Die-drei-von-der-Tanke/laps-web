﻿using System;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonTargetLapsDetails
    {
        [JsonProperty("expire-after")]
        public TimeSpan ExpireAfter { get; set; }
    }
}
