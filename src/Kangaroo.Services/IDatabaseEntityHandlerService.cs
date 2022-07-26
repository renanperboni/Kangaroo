// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;

    public interface IDatabaseEntityHandlerService<TDatabaseEntity, TEntity, TEntityHandlerRequest, TEntityHandlerResponse> : ITransientService
        where TDatabaseEntity : class, IDatabaseEntity
        where TEntity : class, IEntity
        where TEntityHandlerRequest : class, IEntityHandlerRequest<TEntity>
        where TEntityHandlerResponse : class, IEntityHandlerResponse<TEntity>, new()
    {
        public Task<TEntityHandlerResponse> SaveAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default);
    }
}