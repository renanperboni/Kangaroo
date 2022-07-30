// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.Infrastructure.DatabaseEntities
{
    using System.ComponentModel.DataAnnotations;
    using Kangaroo.Models.DatabaseEntities;
    using Microsoft.AspNetCore.Identity;

    public partial class ApplicationUser : IdentityUser, IApplicationUser
    {
        [MaxLength(255)]
        public string FullName { get; set; }
    }
}
