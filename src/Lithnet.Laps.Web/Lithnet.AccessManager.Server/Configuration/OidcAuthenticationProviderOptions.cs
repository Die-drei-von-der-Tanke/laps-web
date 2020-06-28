﻿using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.AccessManager.Configuration
{
    public class OidcAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public string Authority { get; set; }

        public string ClientID { get; set; }

        public string PostLogoutRedirectUri { get; set; } = "/Home/LoggedOut";

        public string RedirectUri { get; set; }

        public string ResponseType { get; set; } = OpenIdConnectResponseType.CodeIdToken;

        public string Secret { get; set; }

        public override string ClaimName { get; set; } = ClaimTypes.Upn;

        public override bool IdpLogout { get; set; }
    }
}