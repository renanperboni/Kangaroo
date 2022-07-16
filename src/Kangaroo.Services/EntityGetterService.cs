// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Threading.Tasks;
    using Kangaroo.Models.Entities;

    public abstract class EntityGetterService<TEntity, TEntityGetterRequest, TEntityGetterResponse> : IEntityGetterService<TEntity, TEntityGetterRequest, TEntityGetterResponse>
        where TEntity : class, IEntity, new()
        where TEntityGetterRequest : class, IEntityGetterRequest
        where TEntityGetterResponse : class, IEntityGetterResponse<TEntity>, new()
    {
        public async Task<TEntityGetterResponse> GetAsync(TEntityGetterRequest entityGetterRequest, CancellationToken cancellationToken = default)
        {
            if (entityGetterRequest == null)
            {
                throw new ArgumentNullException();
            }

            var entity = await this.GetEntityAsync(entityGetterRequest, cancellationToken);

            return this.CreateResponse(entity);
        }

        protected abstract Task<TEntity> GetEntityAsync(TEntityGetterRequest entityGetterRequest, CancellationToken cancellationToken = default);

        protected virtual TEntityGetterResponse CreateResponse(TEntity entity)
        {
            return new TEntityGetterResponse()
            {
                Entity = entity,
            };
        }
    }
}
