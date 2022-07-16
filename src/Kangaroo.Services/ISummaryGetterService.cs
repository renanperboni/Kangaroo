// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using Kangaroo.Models.Entities;

    public interface ISummaryGetterService<TSummary, TSummaryGetterRequest, TSummaryGetterResponse> : IService
        where TSummary : class, ISummary, new()
        where TSummaryGetterRequest : class, ISummaryGetterRequest
        where TSummaryGetterResponse : class, ISummaryGetterResponse<TSummary>, new()
    {
        public Task<TSummaryGetterResponse> GetAsync(TSummaryGetterRequest entityGetterRequest, CancellationToken cancellationToken = default);
    }
}