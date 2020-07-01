﻿using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ApplicationConfigViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private readonly IApplicationConfig model;

        public ApplicationConfigViewModel(IApplicationConfig model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.Items.Add(new HostingViewModel(this.model.Hosting, dialogCoordinator));
            this.Items.Add(new UserInterfaceViewModel(this.model.UserInterface));
            this.Items.Add(new EmailViewModel(this.model.Email));
            this.Items.Add(new RateLimitsViewModel(this.model.RateLimits));
            this.Items.Add(new AuthenticationViewModel(this.model.Authentication));
            this.Items.Add(new IpDetectionViewModel(this.model.ForwardedHeaders, dialogCoordinator));
        }
    }
}
