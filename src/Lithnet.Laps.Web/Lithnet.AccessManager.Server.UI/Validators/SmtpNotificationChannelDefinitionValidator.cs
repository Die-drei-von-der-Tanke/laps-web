﻿using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionValidator : AbstractValidator<SmtpNotificationChannelDefinitionViewModel>
    {
        public SmtpNotificationChannelDefinitionValidator()
        {
            this.RuleFor(r => r.DisplayName).NotEmpty();
            this.RuleFor(r => r.EmailAddresses).NotEmpty();
            this.RuleFor(r => r.TemplateFailure).Custom((item, context) =>
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    context.AddFailure("A template file must be specified");
                }
                else if (!System.IO.File.Exists(item))
                {
                    context.AddFailure("The file does not exist");
                }
            });

            this.RuleFor(r => r.TemplateSuccess).Custom((item, context) =>
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    context.AddFailure("A template file must be specified");
                }
                else if (!System.IO.File.Exists(item))
                {
                    context.AddFailure("The file does not exist");
                }
            });
        }
    }
}
