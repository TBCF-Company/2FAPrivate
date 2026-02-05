// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 NetKnights GmbH
// Based on privacyIDEA UserIdResolver.py

using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrivacyIdeaServer.Lib.Resolvers
{
    /// <summary>
    /// Base class for UserID Resolvers.
    /// Provides default implementations for all resolver methods.
    /// </summary>
    public abstract class UserIdResolverBase : IUserIdResolver
    {
        /// <inheritdoc/>
        public virtual Dictionary<string, int> Fields { get; protected set; } = new()
        {
            { "username", 1 },
            { "userid", 1 },
            { "description", 0 },
            { "phone", 0 },
            { "mobile", 0 },
            { "email", 0 },
            { "givenname", 0 },
            { "surname", 0 },
            { "gender", 0 }
        };

        /// <inheritdoc/>
        public virtual string Name { get; set; } = string.Empty;

        /// <inheritdoc/>
        public virtual string Id { get; protected set; } = "baseid";

        /// <inheritdoc/>
        public virtual bool Updateable { get; protected set; } = false;

        /// <inheritdoc/>
        public virtual bool Editable => false;

        /// <inheritdoc/>
        public virtual bool HasMultipleLoginNames => false;

        /// <inheritdoc/>
        public virtual Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual string GetResolverClassType()
        {
            return "UserIdResolver";
        }

        /// <inheritdoc/>
        public virtual string GetResolverType()
        {
            return GetResolverClassType();
        }

        /// <inheritdoc/>
        public virtual Dictionary<string, object> GetResolverClassDescriptor()
        {
            var descriptor = new Dictionary<string, object>
            {
                { "clazz", "useridresolver.UserIdResolver" },
                { "config", new Dictionary<string, string>() }
            };

            var typ = GetResolverClassType();
            return new Dictionary<string, object>
            {
                { typ, descriptor }
            };
        }

        /// <inheritdoc/>
        public virtual Task<string> GetUserIdAsync(string loginName)
        {
            return Task.FromResult("dummy_user_id");
        }

        /// <inheritdoc/>
        public virtual Task<string> GetUsernameAsync(string userId)
        {
            return Task.FromResult("dummy_user_name");
        }

        /// <inheritdoc/>
        public virtual Task<Dictionary<string, object>> GetUserInfoAsync(string userId)
        {
            return Task.FromResult(new Dictionary<string, object>());
        }

        /// <inheritdoc/>
        public virtual Task<List<Dictionary<string, object>>> GetUserListAsync(
            Dictionary<string, string>? searchDict = null)
        {
            return Task.FromResult(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>()
            });
        }

        /// <inheritdoc/>
        public virtual string GetResolverId()
        {
            return Id;
        }

        /// <inheritdoc/>
        public virtual Task LoadConfigAsync(Dictionary<string, object> config)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task<bool> CheckPassAsync(string uid, string password)
        {
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public virtual Task<string?> AddUserAsync(Dictionary<string, object>? attributes = null)
        {
            return Task.FromResult<string?>(null);
        }

        /// <inheritdoc/>
        public virtual Task<bool> DeleteUserAsync(string uid)
        {
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public virtual Task<bool> UpdateUserAsync(string uid, Dictionary<string, object>? attributes = null)
        {
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public virtual Task<(bool Success, string Description)> TestConnectionAsync(Dictionary<string, object> param)
        {
            return Task.FromResult((false, "Not implemented"));
        }

        /// <inheritdoc/>
        public virtual Dictionary<string, object> GetConfig()
        {
            return new Dictionary<string, object>();
        }
    }
}
