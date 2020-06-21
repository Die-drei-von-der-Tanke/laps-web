﻿using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Web
{
    public interface IPasswordProvider
    {
        IList<PasswordEntry> GetPasswordEntries(IComputer computer, DateTime? newExpiry, bool getHistory);
    }
}