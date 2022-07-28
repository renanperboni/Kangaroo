// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.API.Services
{
    using System;
    using Kangaroo.Services;

    public class CurrentUserService : ICurrentUserService
    {
        public string CurrentUserId { get; } = string.Empty;

        public string CurrentUserName { get; } = string.Empty;

        public string CurrentUserEmail { get; } = string.Empty;

        public string GetCurrentUserNameToAudit()
        {
            return string.Empty;
        }
    }
}
