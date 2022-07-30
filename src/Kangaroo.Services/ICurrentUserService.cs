// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System.Security.Claims;
    using Kangaroo.Models.DatabaseEntities;

    public interface ICurrentUserService : IScopedService
    {
        public string CurrentUserId { get; }

        public string CurrentUserFullName { get; }

        public string CurrentUserEmail { get; }

        public string GetCurrentUserNameToAudit();

        public void SetCurrentUser(ClaimsPrincipal principal);
    }
}