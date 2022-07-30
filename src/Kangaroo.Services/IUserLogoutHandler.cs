// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IUserLogoutHandler : ITransientService
    {
        public Task LogoutAsync(string email, CancellationToken cancellationToken = default);

        public Task CheckUserIsNotLogoutAsync(string email);
    }
}
