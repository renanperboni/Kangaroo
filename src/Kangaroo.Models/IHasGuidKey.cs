﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IHasGuidKey
    {
        public Guid GetKey();

        public void SetKey(Guid key);
    }
}
