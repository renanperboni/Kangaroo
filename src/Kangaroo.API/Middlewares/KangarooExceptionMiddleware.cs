// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.API.Middlewares
{
    using System;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Kangaroo.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    public class KangarooExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<KangarooExceptionMiddleware> logger;

        public KangarooExceptionMiddleware(RequestDelegate next, ILogger<KangarooExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.next(context);
            }
            catch (KangarooException exception)
            {
                this.logger.LogError(exception.ToString());
                await this.HandleExceptionAsync(context, exception.ErrorCode, exception.AdditionalInfo);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.ToString());
                await this.HandleExceptionAsync(context);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, int? errorCode = null, string? additionalInfo = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new KangarooExceptionInfo()
                {
                    ErrorCode = errorCode,
                    AdditionalInfo = additionalInfo,
                }));
        }
    }
}
