// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface ISummaryGetterResponse<TSummary> : IResponse
        where TSummary : class, ISummary
    {
        public TSummary Summary { get; set; }
    }
}
