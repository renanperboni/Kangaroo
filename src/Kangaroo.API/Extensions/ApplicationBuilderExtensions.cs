// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.API.Extensions
{
    using Kangaroo.API.Middlewares;
    using Kangaroo.Models.OptionsSettings;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class ApplicationBuilderExtensions
    {
        public static WebApplicationBuilder ConfigureKangarooOptionsSettings(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(JwtOptions.Jwt));

            return builder;
        }

        public static IApplicationBuilder UseKangarooException(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<KangarooExceptionMiddleware>();

            return builder;
        }
    }
}
