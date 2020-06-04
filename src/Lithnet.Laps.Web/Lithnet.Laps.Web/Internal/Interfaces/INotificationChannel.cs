using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lithnet.Laps.Web.Internal
{
    public interface INotificationChannel
    {
        void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels);

        string Name { get; }
    }
}