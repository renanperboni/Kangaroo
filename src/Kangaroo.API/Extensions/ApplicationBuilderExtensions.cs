// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.API.Extensions
{
    using Kangaroo.API.Middlewares;
    using Microsoft.AspNetCore.Builder;

    public static class ApplicationBuilderExtensions
    {
        public static void UseKangarooException(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<KangarooExceptionMiddleware>();
        }
    }
}
