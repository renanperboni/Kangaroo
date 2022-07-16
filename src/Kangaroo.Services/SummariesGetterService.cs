// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Threading.Tasks;
    using Kangaroo.Models.Entities;

    public abstract class SummariesGetterService<TSummary, TSummariesGetterRequest, TSummariesGetterResponse> : ISummariesGetterService<TSummary, TSummariesGetterRequest, TSummariesGetterResponse>
        where TSummary : class, ISummary, new()
        where TSummariesGetterRequest : class, ISummariesGetterRequest
        where TSummariesGetterResponse : class, ISummariesGetterResponse<TSummary>, new()
    {
        public async Task<TSummariesGetterResponse> GetAsync(TSummariesGetterRequest entityGetterRequest, CancellationToken cancellationToken = default)
        {
            if (entityGetterRequest == null)
            {
                throw new ArgumentNullException();
            }

            var summaries = await this.GetSummariesAsync(entityGetterRequest, cancellationToken);

            return this.CreateResponse(summaries);
        }

        protected abstract Task<IList<TSummary>> GetSummariesAsync(TSummariesGetterRequest entityGetterRequest, CancellationToken cancellationToken = default);

        protected virtual TSummariesGetterResponse CreateResponse(IList<TSummary> summaries)
        {
            return new TSummariesGetterResponse()
            {
                Summaries = summaries,
            };
        }
    }
}
