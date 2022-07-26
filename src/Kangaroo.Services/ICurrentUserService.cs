// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    public interface ICurrentUserService : IScopedService
    {
        public int GetCurrentUserId();

        public Guid GetCurrentUserGuid();

        public string GetCurrentUserName();

        public string GetCurrentUserLogin();
    }
}