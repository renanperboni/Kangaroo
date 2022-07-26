// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class KangarooExceptionInfo
    {
        public int? ErrorCode { get; set; }

        public string AdditionalInfo { get; set; }
    }
}
