﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Internal;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.App_LocalResources;

namespace Lithnet.Laps.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IExternalAuthProviderSettings authSettings;

        public HomeController(IExternalAuthProviderSettings authSettings)
        {
            this.authSettings = authSettings;
        }

        public IActionResult Index()
        {
            return this.RedirectToAction("Get", "Lap");
        }

        public IActionResult AuthNError(AuthNFailureMessageID messageID)
        {
            ErrorModel model = new ErrorModel();
         
            switch (messageID)
            {
                case AuthNFailureMessageID.SsoIdentityNotFound:
                    model.Heading = UIMessages.AccessDenied;
                    model.Message = UIMessages.SsoIdentityNotFound;
                    break;

                case AuthNFailureMessageID.ExternalAuthNProviderDenied:
                    model.Heading = UIMessages.AccessDenied;
                    model.Message = UIMessages.ExternalAuthNAccessDenied;
                    break;

                case AuthNFailureMessageID.UnknownFailure:
                case AuthNFailureMessageID.ExternalAuthNProviderError:
                default:
                    model.Heading = UIMessages.AuthNError;
                    model.Message = UIMessages.UnexpectedError;
                    break;
            }

            return this.View(model);
        }

        public async Task Login(string returnUrl = "/")
        {
            await HttpContext.ChallengeAsync("laps", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task<IActionResult> Logout()
        {
            if (this.Request.HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (authSettings.IdpLogout)
                {
                    return SignOut("laps");
                }
            }

            return this.RedirectToAction("LoggedOut", "Home");
        }

        public IActionResult LoggedOut()
        {
            return this.View();
        }
    }
}