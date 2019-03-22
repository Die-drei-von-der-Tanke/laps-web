﻿using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class DemoAuthorizationService: IAuthorizationService
    {
        public AuthorizationResponse CanAccessPassword(IUser user, IComputer computer)
        {
            if (user.SamAccountName == "u0115389" && computer.SamAccountName.ToUpper() == "GBW-L-W0499")
            {
                return AuthorizationResponse.Authorized(new UsersToNotify(), "Demo authorization");
            }

            return AuthorizationResponse.Unauthorized();
        }
    }
}
