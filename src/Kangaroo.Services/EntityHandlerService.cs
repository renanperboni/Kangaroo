// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Threading.Tasks;
    using Kangaroo.Models;
    using Kangaroo.Models.Entities;

    public abstract class EntityHandlerService<TEntity, TEntityHandlerRequest, TEntityHandlerResponse> : ServiceBase, IEntityHandlerService<TEntity, TEntityHandlerRequest, TEntityHandlerResponse>
        where TEntity : class, IEntity
        where TEntityHandlerRequest : class, IEntityHandlerRequest<TEntity>
        where TEntityHandlerResponse : class, IEntityHandlerResponse<TEntity>, new()
    {
        private readonly ICurrentUserService currentUserService;

        public EntityHandlerService(ICurrentUserService currentUserService)
        {
            this.currentUserService = currentUserService;
        }

        public async Task<TEntityHandlerResponse> SaveAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default)
        {
            if (entityHandlerRequest == null)
            {
                throw new ArgumentNullException();
            }

            if (entityHandlerRequest is IHasAuditLog auditLog && entityHandlerRequest is IHasDataState dataState)
            {
                if (dataState.DataState == DataState.Inserted)
                {
                    auditLog.CreatedAt = DateTimeOffset.Now;
                    auditLog.CreatedByUserName = this.currentUserService.GetCurrentUserNameToAudit();
                }
                else if (dataState.DataState == DataState.Updated)
                {
                    auditLog.UpdatedAt = DateTimeOffset.Now;
                    auditLog.UpdatedByUserName = this.currentUserService.GetCurrentUserNameToAudit();
                }
            }

            this.BeforeSaving(entityHandlerRequest);
            await this.BeforeSavingAsync(entityHandlerRequest, cancellationToken);

            this.Validate(entityHandlerRequest);
            await this.ValidateAsync(entityHandlerRequest, cancellationToken);

            var entity = await this.SaveToAsync(entityHandlerRequest, cancellationToken);

            this.AfterSaving(entity);
            await this.AfterSavingAsync(entity, cancellationToken);

            entityHandlerRequest.Entity = entity;

            return this.CreateResponse(entity);
        }

        protected virtual void Validate(TEntityHandlerRequest entityHandlerRequest)
        {
        }

        protected virtual Task ValidateAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual void BeforeSaving(TEntityHandlerRequest entityHandlerRequest)
        {
        }

        protected virtual Task BeforeSavingAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected abstract Task<TEntity> SaveToAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default);

        protected virtual void AfterSaving(TEntity entity)
        {
        }

        protected virtual Task AfterSavingAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual TEntityHandlerResponse CreateResponse(TEntity entity)
        {
            return new TEntityHandlerResponse()
            {
                Entity = entity,
            };
        }
    }
}
