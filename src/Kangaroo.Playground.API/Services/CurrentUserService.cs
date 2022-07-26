// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.API.Services
{
    using System;
    using Kangaroo.Services;

    public class CurrentUserService : ICurrentUserService
    {
        public Guid GetCurrentUserGuid()
        {
            return Guid.NewGuid();
        }

        public int GetCurrentUserId()
        {
            return default;
        }

        public string GetCurrentUserLogin()
        {
            return string.Empty;
        }

        public string GetCurrentUserName()
        {
            return string.Empty;
        }
    }
}
