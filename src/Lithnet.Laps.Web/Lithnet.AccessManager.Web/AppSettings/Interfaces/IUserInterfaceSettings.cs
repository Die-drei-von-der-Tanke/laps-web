﻿namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IUserInterfaceSettings
    {
        string Title { get; }

        AuditReasonFieldState UserSuppliedReason { get; }

        bool AllowLaps { get; }

        bool AllowJit { get; }
    }
}