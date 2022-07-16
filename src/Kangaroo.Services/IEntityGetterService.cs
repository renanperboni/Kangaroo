// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using Kangaroo.Models.Entities;

    public interface IEntityGetterService<TEntity, TEntityGetterRequest, TEntityGetterResponse> : IService
        where TEntity : class, IEntity, new()
        where TEntityGetterRequest : class, IEntityGetterRequest
        where TEntityGetterResponse : class, IEntityGetterResponse<TEntity>, new()
    {
        public Task<TEntityGetterResponse> GetAsync(TEntityGetterRequest entityGetterRequest, CancellationToken cancellationToken = default);
    }
}