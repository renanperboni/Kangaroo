// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.API.ActionFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Kangaroo.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class KangarooAuthorizationActionFilter : IAsyncAuthorizationFilter
    {
        private readonly ICurrentUserService currentUserService;
        private readonly IUserLogoutHandler userLogoutHandler;

        public KangarooAuthorizationActionFilter(
            ICurrentUserService currentUserService,
            IUserLogoutHandler userLogoutHandler)
        {
            this.currentUserService = currentUserService;
            this.userLogoutHandler = userLogoutHandler;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            this.currentUserService.SetCurrentUser(context.HttpContext.User);
            await this.userLogoutHandler.CheckUserIsNotLogoutAsync(this.currentUserService.CurrentUserEmail);
        }
    }
}
