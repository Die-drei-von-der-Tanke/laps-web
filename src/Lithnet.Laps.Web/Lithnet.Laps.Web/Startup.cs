﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using IdentityModel.Client;
using Lithnet.Laps.Web;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.WsFederation;
using NLog;
using Owin;
using Unity;

//[assembly: OwinStartup(typeof(Startup), "Configure")]

namespace Lithnet.Laps.Web
{
    public class Startup
    {
        internal static bool CanLogout = false;

        internal static string ClaimName { get; set; } = "upn";

        internal static IdentityType ClaimType { get; set; } = IdentityType.UserPrincipalName;

        // These values are stored in Web.config. Make sure you update them!
        private readonly string clientId = ConfigurationManager.AppSettings["oidc:ClientId"];
        private readonly string redirectUri = ConfigurationManager.AppSettings["oidc:RedirectUri"];
        private readonly string authority = ConfigurationManager.AppSettings["oidc:Authority"]?.TrimEnd('/');
        private readonly string clientSecret = ConfigurationManager.AppSettings["oidc:ClientSecret"];
        private readonly string postLogoutRedirectUri = ConfigurationManager.AppSettings["oidc:PostLogoutRedirectUri"];

        private readonly string realm = ConfigurationManager.AppSettings["ida:wtrealm"];
        private readonly string signOutWreply = ConfigurationManager.AppSettings["ida:signOutWreply"];
        private readonly string metadata = ConfigurationManager.AppSettings["ida:metadata"];

        private readonly ILogger logger;

        private readonly IReporting reporting;

        private readonly IDirectory directory;

        /// <summary>
        /// Explicitly get the logger and the reporting from the DI-container.
        /// Constructor injection doesn't work for this class.
        /// </summary>
        public Startup()
        {
            IUnityContainer container = UnityConfig.Container;
            container.RegisterInstance<ILapsConfig>((LapsConfigSection)ConfigurationManager.GetSection(LapsConfigSection.SectionName));

            this.logger = container.Resolve<ILogger>();
            this.reporting = container.Resolve<IReporting>();
            this.directory = container.Resolve<IDirectory>();
        }

        public void Configure(IAppBuilder app)
        {
            string mode = ConfigurationManager.AppSettings["authentication:mode"]?.ToLower();

            if (mode == "wsfed")
            {
                this.ConfigureWsFederation(app);
            }
            else if (mode == "oidc")
            {
                this.ConfigureOpenIDConnect(app);
            }
            else
            {
                this.ConfigureWindowsAuth(app);
            }
        }

        public void ConfigureOpenIDConnect(IAppBuilder app)
        {
            Startup.CanLogout = true;

            Startup.ClaimName = ConfigurationManager.AppSettings["oidc:claimName"] ?? ClaimTypes.Upn;

            if (Enum.TryParse(ConfigurationManager.AppSettings["oidc:claimType"], out IdentityType claimType))
            {
                Startup.ClaimType = claimType;
            }
            else
            {
                Startup.ClaimType = IdentityType.UserPrincipalName;
            }

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ConfigurationManager.AppSettings["oidc:uniqueClaimTypeIdentifier"] ?? ClaimTypes.PrimarySid;

            string responseType = ConfigurationManager.AppSettings["oidc:responseType"] ?? OpenIdConnectResponseType.IdToken;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager(),
                AuthenticationType = "Cookies"
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = this.clientId,
                ClientSecret = this.clientSecret,
                Authority = this.authority,
                RedirectUri = this.redirectUri,
                ResponseType = responseType,
                Scope = OpenIdConnectScope.OpenIdProfile,
                PostLogoutRedirectUri = this.postLogoutRedirectUri ?? new Uri(new Uri(this.redirectUri.Trim('/', '\\')), "Home/LogOut").ToString(),
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    SaveSigninToken = true
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = async n =>
                    {
                        try
                        {
                            OpenIdConnectConfiguration config = await n.Options.ConfigurationManager.GetConfigurationAsync(n.Request.CallCancelled).ConfigureAwait(false);

                            var client = new HttpClient();

                            var tokenResponse = await client.RequestTokenAsync(new TokenRequest
                            {
                                Address = config.TokenEndpoint,
                                ClientId = this.clientId,
                                ClientSecret = this.clientSecret,
                            });

                            if (tokenResponse.IsError)
                            {
                                throw new Exception(tokenResponse.Error);
                            }

                            UserInfoResponse userInfoResponse = await client.GetUserInfoAsync(new UserInfoRequest
                            {
                                Address = config.UserInfoEndpoint,
                                Token = tokenResponse.AccessToken
                            });

                            if (userInfoResponse.IsError)
                            {
                                throw new OpenIdConnectProtocolException(userInfoResponse.Error);
                            }

                            List<Claim> claims = new List<Claim>();
                            claims.AddRange(userInfoResponse.Claims);
                            claims.Add(new Claim("id_token", tokenResponse.IdentityToken));
                            claims.Add(new Claim("access_token", tokenResponse.AccessToken));

                            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                            {
                                claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                            }

                            n.AuthenticationTicket.Identity.AddClaims(claims);
                        }
                        catch (Exception ex)
                        {
                            this.reporting.LogErrorEvent(EventIDs.OidcAuthZCodeError, LogMessages.AuthZCodeFlowError, ex);
                            n.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(ex.Message)}");
                        }
                    },
                    SecurityTokenValidated = n =>
                    {
                        ClaimsIdentity user = n.AuthenticationTicket.Identity;
                        user.AddClaim(new Claim("id_token", n.ProtocolMessage.IdToken));
                        return this.FindClaimIdentityInDirectoryOrFail(n);
                    },
                    RedirectToIdentityProvider = n =>
                    {
                        // If signing out, add the id_token_hint
                        if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
                        {
                            Claim idTokenClaim = n.OwinContext.Authentication.User.FindFirst("id_token");

                            if (idTokenClaim != null)
                            {
                                n.ProtocolMessage.IdTokenHint = idTokenClaim.Value;
                            }
                        }

                        this.logger.Trace($"Redirecting to IdP for {n.ProtocolMessage.RequestType}");
                        return Task.CompletedTask;
                    },
                    AuthenticationFailed = this.HandleAuthNFailed
                },
            });
        }

        public void ConfigureWindowsAuth(IAppBuilder app)
        {
            Startup.CanLogout = false;
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ConfigurationManager.AppSettings["ida:uniqueClaimTypeIdentifier"] ?? ClaimTypes.PrimarySid;
            Startup.ClaimName = ClaimTypes.PrimarySid;
            Startup.ClaimType = IdentityType.Sid;
        }

        public void ConfigureWsFederation(IAppBuilder app)
        {
            Startup.CanLogout = true;
            Startup.ClaimName = ConfigurationManager.AppSettings["ida:claimName"] ?? ClaimTypes.Upn;

            if (Enum.TryParse(ConfigurationManager.AppSettings["ida:claimType"], out IdentityType claimType))
            {
                Startup.ClaimType = claimType;
            }
            else
            {
                Startup.ClaimType = IdentityType.UserPrincipalName;
            }

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ConfigurationManager.AppSettings["ida:uniqueClaimTypeIdentifier"] ?? ClaimTypes.PrimarySid;

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager(),
                AuthenticationType = "Cookies"
            });

            IdentityModelEventSource.ShowPII = true;

            app.UseWsFederationAuthentication(
                new WsFederationAuthenticationOptions
                {
                    Wtrealm = this.realm,
                    MetadataAddress = this.metadata,
                    SignOutWreply = this.signOutWreply ?? new Uri(new Uri(this.realm.Trim('/', '\\')), "Home/LogOut").ToString(),
                    Notifications = new WsFederationAuthenticationNotifications
                    {
                        SecurityTokenValidated = this.FindClaimIdentityInDirectoryOrFail,
                        AuthenticationFailed = this.HandleAuthNFailed
                    }
                });
        }

        private Task HandleAuthNFailed<TMessage, TOptions>(AuthenticationFailedNotification<TMessage, TOptions> context)
        {
            this.reporting.LogErrorEvent(EventIDs.OwinAuthNError, LogMessages.AuthNProviderError, context.Exception);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(context.Exception?.Message ?? "Unknown error")}");

            return Task.FromResult(0);
        }

        private Task FindClaimIdentityInDirectoryOrFail<TMessage, TOptions>(SecurityTokenValidatedNotification<TMessage, TOptions> context)
        {
            ClaimsIdentity user = context.AuthenticationTicket.Identity;

            string sid = this.FindUserByClaim(user, Startup.ClaimName)?.Sid?.Value;

            if (sid == null)
            {
                string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                this.reporting.LogErrorEvent(EventIDs.SsoIdentityNotFound, message, null);

                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(UIMessages.SsoIdentityNotFound)}");
                return Task.CompletedTask;
            }

            user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));

            this.reporting.LogSuccessEvent(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));

            return Task.CompletedTask;
        }

        private IUser FindUserByClaim(ClaimsIdentity p, string claimName)
        {
            Claim c = p.FindFirst(claimName);

            if (c != null)
            {
                return this.directory.GetUser(c.Value);
            }

            return null;
        }
    }
}