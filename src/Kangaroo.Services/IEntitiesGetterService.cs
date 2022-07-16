// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using Kangaroo.Models.Entities;

    public interface IEntitiesGetterService<TEntity, TEntitiesGetterRequest, TEntitiesGetterResponse> : IService
        where TEntity : class, IEntity, new()
        where TEntitiesGetterRequest : class, IEntitiesGetterRequest
        where TEntitiesGetterResponse : class, IEntitiesGetterResponse<TEntity>, new()
    {
        public Task<TEntitiesGetterResponse> GetAsync(TEntitiesGetterRequest entitiesGetterRequest, CancellationToken cancellationToken = default);
    }
}