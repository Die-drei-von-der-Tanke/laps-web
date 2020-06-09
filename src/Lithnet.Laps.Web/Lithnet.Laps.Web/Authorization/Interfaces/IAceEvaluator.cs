﻿using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAceEvaluator
    {
        bool IsMatchingAce(IAce ace, IComputer computer, IUser user);
    }
}