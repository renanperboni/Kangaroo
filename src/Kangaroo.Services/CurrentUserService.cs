// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Security.Claims;
    using Kangaroo.Exceptions;
    using Kangaroo.Models.CacheKeys;
    using Kangaroo.Models.Claims;
    using Microsoft.Extensions.Caching.Distributed;

    public class CurrentUserService : ICurrentUserService
    {
        public string CurrentUserId { get; private set; } = string.Empty;

        public string CurrentUserFullName { get; private set; } = string.Empty;

        public string CurrentUserEmail { get; private set; } = string.Empty;

        public string GetCurrentUserNameToAudit()
        {
            return this.CurrentUserFullName;
        }

        public void SetCurrentUser(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new KangarooSecurityException();
            }

            this.CurrentUserEmail = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            this.CurrentUserId = principal.Claims.FirstOrDefault(x => x.Type == KangarooClaims.UserId)?.Value;
            this.CurrentUserFullName = principal.Claims.FirstOrDefault(x => x.Type == KangarooClaims.FullName)?.Value;

            if (string.IsNullOrEmpty(this.CurrentUserEmail)
                || string.IsNullOrEmpty(this.CurrentUserId)
                || string.IsNullOrEmpty(this.CurrentUserFullName))
            {
                throw new KangarooSecurityException();
            }
        }
    }
}
