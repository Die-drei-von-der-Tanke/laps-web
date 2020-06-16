using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Management.Automation;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lithnet.AccessManager.Web.AppSettings;
using Microsoft.AspNetCore.Hosting;
using NLog;

namespace Lithnet.AccessManager.Web.Internal
{
    public class PowershellNotificationChannel : NotificationChannel<IPowershellChannelSettings>
    {
        private readonly ILogger logger;

        private readonly IAuditSettings auditSettings;

        private readonly IWebHostEnvironment env;

        public override string Name => "powershell";

        private PowerShell powershell;

        public PowershellNotificationChannel(ILogger logger, IAuditSettings auditSettings, IWebHostEnvironment env, ChannelWriter<Action> queue)
            : base(logger, queue)
        {
            this.logger = logger;
            this.auditSettings = auditSettings;
            this.env = env;
        }

        public override void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels)
        {
            this.ProcessNotification(action, tokens, notificationChannels, this.auditSettings.Channels.Powershell);
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, IPowershellChannelSettings settings)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession(settings);
            }

            this.powershell.ResetState();
            this.powershell
                .AddCommand("Write-AuditLog")
                    .AddParameter("tokens", tokens)
                    .AddParameter("isSuccess", action.IsSuccess)
                    .AddParameter("logger", logger);

            Task task = new Task(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();
            });

            task.Start();
            if (!task.Wait(TimeSpan.FromSeconds(settings.TimeOut)))
            {
                throw new TimeoutException("The PowerShell script did not complete within the configured time");
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }


        private void InitializePowerShellSession(IPowershellChannelSettings settings)
        {
            string path = env.ResolvePath(settings.Script, "Scripts");

            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException("The PowerShell script was not found", path);
            }

            powershell = PowerShell.Create();
            powershell.AddScript(File.ReadAllText(path));
            powershell.Invoke();

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Write-AuditLog", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Write-AuditLog'");
            }

            this.logger.Trace($"The PowerShell script was successfully initialized");
        }
    }
}