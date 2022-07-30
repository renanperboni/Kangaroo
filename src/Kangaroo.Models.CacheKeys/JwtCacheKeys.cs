// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.CacheKeys
{
    using System;

    public static class JwtCacheKeys
    {
        public const string UserHasLogoutKey = "UserHasLogout_{0}";

        public const string RefreshTokenKey = "JWTRefreshToken_{0}";

        public const string RefreshTokenExpirationTimeKey = "JWTRefreshTokenExp_{0}";
    }
}
