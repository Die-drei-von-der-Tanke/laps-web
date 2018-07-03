﻿using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Configuration;
using System.Security.Claims;
using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Threading;
using System.Web;
using System.Web.Helpers;
using Lithnet.Laps.Web.App_LocalResources;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.WsFederation;
using NLog;

namespace Lithnet.Laps.Web
{
    public class Startup
    {
        internal static string ClaimName { get; set; } = "upn";

        internal static IdentityType ClaimType { get; set; } = IdentityType.UserPrincipalName;

        // These values are stored in Web.config. Make sure you update them!
        private readonly string clientId = ConfigurationManager.AppSettings["oidc:ClientId"];
        private readonly string redirectUri = ConfigurationManager.AppSettings["oidc:RedirectUri"];
        private readonly string authority = ConfigurationManager.AppSettings["oidc:Authority"]?.TrimEnd('/');
        private readonly string clientSecret = ConfigurationManager.AppSettings["oidc:ClientSecret"];
        private readonly string postLogoutRedirectUri = ConfigurationManager.AppSettings["oidc:PostLogoutRedirectUri"];

        private readonly string realm = ConfigurationManager.AppSettings["ida:wtrealm"];
        private readonly string metadata = ConfigurationManager.AppSettings["ida:metadata"];

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void ConfigureOpenIDConnect(IAppBuilder app)
        {
            Startup.ClaimName = ConfigurationManager.AppSettings["oidc:claimName"] ?? ClaimTypes.Upn;

            if (Enum.TryParse(ConfigurationManager.AppSettings["oidc:claimType"], out IdentityType claimType))
            {
                Startup.ClaimType = claimType;
            }
            else
            {
                Startup.ClaimType = IdentityType.UserPrincipalName;
            }

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ConfigurationManager.AppSettings["oidc:uniqueClaimTypeIdentifier"] ?? ClaimTypes.NameIdentifier;

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
                PostLogoutRedirectUri = this.postLogoutRedirectUri,
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = async n =>
                    {
                        try
                        {
                            if (responseType == OpenIdConnectResponseType.IdToken)
                            {
                                return;
                            }
                            
                            // Exchange code for access and ID tokens
                            OpenIdConnectConfiguration config = await n.Options.ConfigurationManager.GetConfigurationAsync(n.Request.CallCancelled).ConfigureAwait(false);

                            TokenClient tokenClient = new TokenClient(config.TokenEndpoint, this.clientId, this.clientSecret);
                            TokenResponse tokenResponse = await tokenClient.RequestAuthorizationCodeAsync(n.Code, this.redirectUri).ConfigureAwait(false);

                            if (tokenResponse.IsError)
                            {
                                throw new Exception(tokenResponse.Error);
                            }

                            UserInfoClient userInfoClient = new UserInfoClient(config.UserInfoEndpoint);
                            UserInfoResponse userInfoResponse = await userInfoClient.GetAsync(tokenResponse.AccessToken);
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
                            Reporting.LogErrorEvent(EventIDs.OidcAuthZCodeError, LogMessages.AuthZCodeFlowError, ex);
                            n.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(ex.Message)}");
                        }
                    },
                    SecurityTokenValidated = Startup.FindClaimIdentityInDirectoryOrFail,
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

                        logger.Trace($"Redirecting to IdP for {n.ProtocolMessage.RequestType}");
                        return Task.CompletedTask;
                    },
                    AuthenticationFailed = Startup.HandleAuthNFailed
                },
            });
        }

        public void ConfigureWindowsAuth(IAppBuilder app)
        {
            ClaimName = ClaimTypes.PrimarySid;
            ClaimType = IdentityType.Sid;
        }

        public void ConfigureWsFederation(IAppBuilder app)
        {
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

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ConfigurationManager.AppSettings["ida:uniqueClaimTypeIdentifier"] ?? ClaimTypes.NameIdentifier;

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
                    Notifications = new WsFederationAuthenticationNotifications
                    {
                        SecurityTokenValidated = Startup.FindClaimIdentityInDirectoryOrFail,
                        AuthenticationFailed = Startup.HandleAuthNFailed
                    }
                });
        }

        private static Task HandleAuthNFailed<TMessage, TOptions>(AuthenticationFailedNotification<TMessage, TOptions> context)
        {
            Reporting.LogErrorEvent(EventIDs.OwinAuthNError, LogMessages.AuthNProviderError, context.Exception);

            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(context.Exception?.Message ?? "Unknown error")}");

            return Task.FromResult(0);
        }

        private static Task FindClaimIdentityInDirectoryOrFail<TMessage, TOptions>(SecurityTokenValidatedNotification<TMessage, TOptions> context)
        {
            ClaimsIdentity user = context.AuthenticationTicket.Identity;

            string sid = user.FindUserPrincipalByClaim(Startup.ClaimType, Startup.ClaimName)?.Sid?.Value;

            if (sid == null)
            {
                string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                Reporting.LogErrorEvent(EventIDs.SsoIdentityNotFound, message, null);

                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(UIMessages.SsoIdentityNotFound)}");
                return Task.CompletedTask;
            }

            user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));

            Reporting.LogSuccessEvent(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));

            return Task.CompletedTask;
        }
    }
}