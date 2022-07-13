// Licensed to Kangaroo under one or more agreements.
// We license this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public enum DataState
    {
        Unchanged = 0,
        Inserted = 1,
        Updated = 2,
        Deleted = 3,
    }
}
