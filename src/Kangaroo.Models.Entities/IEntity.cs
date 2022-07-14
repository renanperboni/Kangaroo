// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Kangaroo.Models;

    public interface IEntity
    {
        public DataState DataState { get; set; }
    }
}
