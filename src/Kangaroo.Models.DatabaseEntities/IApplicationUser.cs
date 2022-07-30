// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.DatabaseEntities
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IApplicationUser
    {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
    }
}
