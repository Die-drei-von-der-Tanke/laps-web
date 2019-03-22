﻿using NLog;
using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly LapsConfigSection configSection;
        private readonly Logger logger;
        private readonly Directory directory;

        public ConfigurationFileAuthorizationService(LapsConfigSection configSection, Logger logger,
            Directory directory)
        {
            this.configSection = configSection;
            this.logger = logger;
            this.directory = directory;
        }

        /// <summary>
        /// Check whether the user with name <paramref name="userName"/> can 
        /// access the password of the computer with name
        /// <paramref name="computerName"/>, based on the reader elements under the targets in Web.Config.
        /// </summary>
        /// <param name="user">a user. FIXME: We shouldn't depend on AD here.</param>
        /// <param name="computerName">name of the computer</param>
        /// <param name="target">Target section in the web.config-file.
        /// FIXME: This shouldn't be in this interface. But I can't leave it out
        /// yet, because the code figuring out the target has way too many dependencies.
        /// </param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        public AuthorizationResponse CanAccessPassword(UserPrincipal user, string computerName, TargetElement target = null)
        {
            // FIXME: This function doesn't even look at computerName, because it assumes this check already happened at some other place.
            foreach (ReaderElement reader in target.Readers.OfType<ReaderElement>())
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.Name}");

                    return new AuthorizationResponse(true, reader.Audit.UsersToNotify, reader.Principal);
                }
            }

            return new AuthorizationResponse(false, new UsersToNotify(), String.Empty);
        }

        private bool IsReaderAuthorized(ReaderElement reader, UserPrincipal currentUser)
        {
            Principal readerPrincipal = directory.GetPrincipal(reader.Principal);

            if (currentUser.Equals(readerPrincipal))
            {
                return true;
            }

            if (readerPrincipal is GroupPrincipal group)
            {
                if (directory.IsPrincipalInGroup(currentUser, group))
                {
                    return true;
                }
            }

            return false;
        }
    }
}