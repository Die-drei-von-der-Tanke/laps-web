﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using Lithnet.Laps.Web.AppSettings;
using Microsoft.AspNetCore.Http;

namespace Lithnet.Laps.Web.Internal
{
    public class IpAddressResolver : IIpAddressResolver
    {
        private readonly IIpResolverSettings config;

        public IpAddressResolver(IIpResolverSettings config)
        {
            this.config = config;
        }

        public string GetRequestIP(HttpRequest request)
        {
            if (request == null)
            {
                return null;
            }

            return this.GetRequestIP(request.HttpContext.Connection.RemoteIpAddress.ToString(), request.Headers);
        }

        private string GetRequestIP(string originalIP, IHeaderDictionary headers)
        {
            if (this.config.Mode == IpResolverMode.Default)
            {
                return originalIP;
            }

            switch (this.config.Mode)
            {
                case IpResolverMode.Xff:
                    return this.GetXffIp(originalIP, headers);

                case IpResolverMode.ClientIP:
                    return this.GetClientIp(originalIP, headers);

                default:
                    return originalIP;
            }
        }

        private string GetXffIpTrustedProxy(string originalIP, List<string> hostList)
        {
            List<string> proxies = this.config.Xff.TrustedProxies.ToList();

            if (proxies.Count > 0)
            {
                for (int i = hostList.Count - 1; i >= 0; i--)
                {
                    if (!proxies.Any(t => t.Equals(hostList[i], StringComparison.OrdinalIgnoreCase)))
                    {
                        return hostList[i];
                    }
                }
            }

            return originalIP;
        }

        private string GetXffIpProxyDepth(string originalIP, List<string> hostList)
        {
            if (this.config.Xff.ProxyDepth == 0)
            {
                return originalIP;
            }

            if (this.config.Xff.ProxyDepth >= hostList.Count)
            {
                return hostList[0];
            }
            else
            {
                return hostList[hostList.Count - this.config.Xff.ProxyDepth - 1];
            }
        }

        private string GetXffIp(string originalIP, IHeaderDictionary headers)
        {
            if (string.IsNullOrWhiteSpace(this.config.Xff.HeaderName))
            {
                return originalIP;
            }

            string headerValue = headers[this.config.Xff.HeaderName];

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                return originalIP;
            }

            List<string> hostList = headerValue.Split(',').ToList() ?? new List<string>();

            if (hostList.Count == 0)
            {
                return originalIP;
            }

            if (this.config.Xff.Mode == AppSettings.XffResolverMode.ProxyDepth)
            {
                return this.GetXffIpProxyDepth(originalIP, hostList);
            }
            else
            {
                return this.GetXffIpTrustedProxy(originalIP, hostList);
            }
        }

        private string GetClientIp(string originalIP, IHeaderDictionary headers)
        {
            if (this.config.ClientIP.HeaderName == null)
            {
                return originalIP;
            }

            string knownClientIP = headers[this.config.ClientIP.HeaderName];

            return !string.IsNullOrWhiteSpace(knownClientIP) ? knownClientIP : originalIP;
        }
    }
}