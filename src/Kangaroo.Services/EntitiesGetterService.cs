// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Threading.Tasks;
    using Kangaroo.Models.Entities;

    public abstract class EntitiesGetterService<TEntity, TEntitiesGetterRequest, TEntitiesGetterResponse> : ServiceBase, IEntitiesGetterService<TEntity, TEntitiesGetterRequest, TEntitiesGetterResponse>
        where TEntity : class, IEntity, new()
        where TEntitiesGetterRequest : class, IEntitiesGetterRequest
        where TEntitiesGetterResponse : class, IEntitiesGetterResponse<TEntity>, new()
    {
        public async Task<TEntitiesGetterResponse> GetAsync(TEntitiesGetterRequest entityGetterRequest, CancellationToken cancellationToken = default)
        {
            if (entityGetterRequest == null)
            {
                throw new ArgumentNullException();
            }

            var entities = await this.GetEntitiesAsync(entityGetterRequest, cancellationToken);

            return this.CreateResponse(entities);
        }

        protected abstract Task<IList<TEntity>> GetEntitiesAsync(TEntitiesGetterRequest entityGetterRequest, CancellationToken cancellationToken = default);

        protected virtual TEntitiesGetterResponse CreateResponse(IList<TEntity> entities)
        {
            return new TEntitiesGetterResponse()
            {
                Entities = entities,
            };
        }
    }
}
