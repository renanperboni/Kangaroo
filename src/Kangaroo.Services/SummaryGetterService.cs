// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Threading.Tasks;
    using Kangaroo.Models.Entities;

    public abstract class SummaryGetterService<TSummary, TSummaryGetterRequest, TSummaryGetterResponse> : ISummaryGetterService<TSummary, TSummaryGetterRequest, TSummaryGetterResponse>
        where TSummary : class, ISummary, new()
        where TSummaryGetterRequest : class, ISummaryGetterRequest
        where TSummaryGetterResponse : class, ISummaryGetterResponse<TSummary>, new()
    {
        public async Task<TSummaryGetterResponse> GetAsync(TSummaryGetterRequest summaryGetterRequest, CancellationToken cancellationToken = default)
        {
            if (summaryGetterRequest == null)
            {
                throw new ArgumentNullException();
            }

            var summary = await this.GetSummaryAsync(summaryGetterRequest, cancellationToken);

            return this.CreateResponse(summary);
        }

        protected abstract Task<TSummary> GetSummaryAsync(TSummaryGetterRequest summaryGetterRequest, CancellationToken cancellationToken = default);

        protected virtual TSummaryGetterResponse CreateResponse(TSummary summary)
        {
            return new TSummaryGetterResponse()
            {
                Summary = summary,
            };
        }
    }
}
