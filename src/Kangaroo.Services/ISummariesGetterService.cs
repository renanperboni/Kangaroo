// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using Kangaroo.Models.Entities;

    public interface ISummariesGetterService<TSummary, TSummariesGetterRequest, TSummariesGetterResponse> : ITransientService
        where TSummary : class, ISummary, new()
        where TSummariesGetterRequest : class, ISummariesGetterRequest
        where TSummariesGetterResponse : class, ISummariesGetterResponse<TSummary>, new()
    {
        public Task<TSummariesGetterResponse> GetAsync(TSummariesGetterRequest entitiesGetterRequest, CancellationToken cancellationToken = default);
    }
}