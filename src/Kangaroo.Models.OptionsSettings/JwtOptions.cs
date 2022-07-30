// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.OptionsSettings
{
    public class JwtOptions
    {
        public const string Jwt = "Jwt";

        public string JwtIssuer { get; set; } = string.Empty;

        public string JwtAudience { get; set; } = string.Empty;

        public int JwtExpiryInMinutes { get; set; }

        public int JwtRefreshTokenExpiryInMinutes { get; set; }

        public string JwtSecurityKey { get; set; } = string.Empty;
    }
}
