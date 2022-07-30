// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Kangaroo.Exceptions;
    using Kangaroo.Models.CacheKeys;
    using Kangaroo.Models.Claims;
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.OptionsSettings;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    public abstract class AuthServiceBase<TApplicationUser> : ServiceBase, IUserLogoutHandler
        where TApplicationUser : IdentityUser, IApplicationUser, new()
    {
        public AuthServiceBase(
            UserManager<TApplicationUser> userManager,
            SignInManager<TApplicationUser> signInManager,
            IOptions<JwtOptions> jwtOptions,
            ICurrentUserService currentUserService,
            IDistributedCache distributedCache)
        {
            this.UserManager = userManager;
            this.SignInManager = signInManager;
            this.JwtOptions = jwtOptions.Value;
            this.CurrentUserService = currentUserService;
            this.DistributedCache = distributedCache;
        }

        protected UserManager<TApplicationUser> UserManager { get; }

        protected SignInManager<TApplicationUser> SignInManager { get; }

        protected JwtOptions JwtOptions { get; }

        protected ICurrentUserService CurrentUserService { get; }

        protected IDistributedCache DistributedCache { get; }

        public async Task LogoutAsync(string email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await this.DistributedCache.SetStringAsync(string.Format(JwtCacheKeys.UserHasLogoutKey, email), "true");
            await this.DistributedCache.RemoveAsync(string.Format(JwtCacheKeys.RefreshTokenKey, email));
            await this.DistributedCache.RemoveAsync(string.Format(JwtCacheKeys.RefreshTokenExpirationTimeKey, email));
        }

        public async Task CheckUserIsNotLogoutAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new KangarooSecurityException();
            }

            var token = await this.DistributedCache.GetStringAsync(string.Format(JwtCacheKeys.UserHasLogoutKey, email));

            if (!string.IsNullOrEmpty(token))
            {
                throw new KangarooSecurityException();
            }
        }

        protected async Task InsertApplicationUserAsync(TApplicationUser applicationUser, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await this.UserManager.CreateAsync(applicationUser, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                throw new KangarooException(internalErrorCode: KangarooErrorCode.InvalidPassword, additionalInfo: errors);
            }
        }

        protected async Task<(string Token, string RefreshToken)> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var applicationUser = await this.UserManager.FindByEmailAsync(email);

            if (applicationUser == null)
            {
                throw new KangarooSecurityException();
            }

            var result = await this.SignInManager.PasswordSignInAsync(email, password, false, false);

            if (!result.Succeeded)
            {
                throw new KangarooSecurityException();
            }

            return await this.GenerateTokenAsync(applicationUser);
        }

        protected async Task<(string Token, string RefreshToken)> RefreshTokenAsync(string currentToken, string currentRefreshToken, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentUserId = this.CurrentUserService.CurrentUserId;
            var principal = this.GetPrincipalFromToken(currentToken);

            var email = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email).Value;
            var applicationUser = await this.UserManager.FindByEmailAsync(email);

            if (applicationUser == null
                || applicationUser.Id != currentUserId)
            {
                throw new KangarooSecurityException();
            }

            var refreshToken = await this.DistributedCache.GetStringAsync(string.Format(JwtCacheKeys.RefreshTokenKey, applicationUser.Email));
            var refreshTokenExpiration = await this.DistributedCache.GetStringAsync(string.Format(JwtCacheKeys.RefreshTokenExpirationTimeKey, applicationUser.Email));

            if (string.IsNullOrEmpty(refreshToken)
                || string.IsNullOrEmpty(refreshTokenExpiration)
                || currentRefreshToken != refreshToken)
            {
                throw new KangarooSecurityException();
            }

            if (!DateTime.TryParse(refreshTokenExpiration, out var refreshTokenExpirationDateTime))
            {
                throw new KangarooSecurityException();
            }

            if (refreshTokenExpirationDateTime < DateTime.Now)
            {
                throw new KangarooSecurityException();
            }

            return await this.GenerateTokenAsync(applicationUser);
        }

        protected async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentUserId = this.CurrentUserService.CurrentUserId;
            var applicationUser = await this.UserManager.FindByIdAsync(currentUserId);

            if (applicationUser == null)
            {
                throw new KangarooSecurityException();
            }

            var result = await this.UserManager.ChangePasswordAsync(
                applicationUser,
                currentPassword,
                newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                throw new KangarooException(internalErrorCode: KangarooErrorCode.InvalidPassword, additionalInfo: errors);
            }
        }

        private async Task<(string Token, string RefreshToken)> GenerateTokenAsync(TApplicationUser applicationUser)
        {
            var validIssuer = this.JwtOptions.JwtIssuer;
            var validAudience = this.JwtOptions.JwtAudience;
            var tokenExpirationInMinutes = this.JwtOptions.JwtExpiryInMinutes;
            var refreshTokenExpirationInMinutes = this.JwtOptions.JwtRefreshTokenExpiryInMinutes;
            var secretKey = this.JwtOptions.JwtSecurityKey;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenExpiration = DateTime.Now.AddMinutes(tokenExpirationInMinutes);
            var refreshTokenExpiration = DateTime.Now.AddMinutes(refreshTokenExpirationInMinutes);

            var claims = new List<Claim>()
            {
                new Claim(KangarooClaims.UserId, applicationUser.Id),
                new Claim(KangarooClaims.FullName, applicationUser.FullName),
                new Claim(ClaimTypes.Name, applicationUser.UserName),
                new Claim(ClaimTypes.Email, applicationUser.Email),
            };

            var roles = await this.UserManager.GetRolesAsync(applicationUser);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtSecurityToken = new JwtSecurityToken(
                validIssuer,
                validAudience,
                claims,
                expires: tokenExpiration,
                signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var refreshToken = this.GenerateRefreshToken();

            var tokenDistributedCacheEntryOptions = new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = tokenExpiration,
            };

            var refreshTokenDistributedCacheEntryOptions = new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = refreshTokenExpiration,
            };

            await this.DistributedCache.SetStringAsync(string.Format(JwtCacheKeys.RefreshTokenKey, applicationUser.Email), refreshToken, refreshTokenDistributedCacheEntryOptions);
            await this.DistributedCache.SetStringAsync(
                string.Format(JwtCacheKeys.RefreshTokenExpirationTimeKey, applicationUser.Email),
                DateTime.Now.AddMinutes(this.JwtOptions.JwtRefreshTokenExpiryInMinutes).ToString(),
                refreshTokenDistributedCacheEntryOptions);
            await this.DistributedCache.RemoveAsync(string.Format(JwtCacheKeys.UserHasLogoutKey, applicationUser.Email));

            return (Token: token, RefreshToken: refreshToken);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var validIssuer = this.JwtOptions.JwtIssuer;
            var validAudience = this.JwtOptions.JwtAudience;
            var secretKey = this.JwtOptions.JwtSecurityKey;

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false,
                ValidIssuer = validIssuer,
                ValidAudience = validAudience,
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(
                token,
                tokenValidationParameters,
                out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new KangarooSecurityException();
            }

            return principal;
        }
    }
}