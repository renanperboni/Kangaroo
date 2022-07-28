// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoMapper;
    using Kangaroo.Infrastructure.DatabaseRepositories;
    using Kangaroo.Models;
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;
    using Microsoft.EntityFrameworkCore;

    public abstract class DatabaseEntityHandlerService<TDbContext, TDatabaseEntity, TEntity, TEntityHandlerRequest, TEntityHandlerResponse> : ServiceBase, IDatabaseEntityHandlerService<TDatabaseEntity, TEntity, TEntityHandlerRequest, TEntityHandlerResponse>
        where TDbContext : DbContext
        where TDatabaseEntity : class, IDatabaseEntity
        where TEntity : class, IEntity
        where TEntityHandlerRequest : class, IEntityHandlerRequest<TEntity>
        where TEntityHandlerResponse : class, IEntityHandlerResponse<TEntity>, new()
    {
        private readonly IDatabaseRepository<TDbContext> databaseRepository;
        private readonly IMapper mapper;
        private readonly ICurrentUserService currentUserService;

        public DatabaseEntityHandlerService(
            IDatabaseRepository<TDbContext> databaseRepository,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            this.databaseRepository = databaseRepository;
            this.mapper = mapper;
            this.currentUserService = currentUserService;
        }

        protected IDatabaseRepository<TDbContext> DatabaseRepository => this.databaseRepository;

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

            var databaseEntity = await this.SaveToDatabaseAsync(entityHandlerRequest, cancellationToken);

            var entity = this.mapper.Map<TEntity>(databaseEntity);

            this.AfterSaving(databaseEntity, entity);
            await this.AfterSavingAsync(databaseEntity, entity, cancellationToken);

            entityHandlerRequest.Entity = entity;

            return this.CreateResponse(databaseEntity, entity);
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

        protected virtual async Task<TDatabaseEntity> SaveToDatabaseAsync(TEntityHandlerRequest entityHandlerRequest, CancellationToken cancellationToken = default)
        {
            var databaseEntity = this.DatabaseRepository.ApplyChanges<TDatabaseEntity, TEntity>(entityHandlerRequest.Entity);
            await this.DatabaseRepository.SaveAsync(cancellationToken);

            return databaseEntity;
        }

        protected virtual void AfterSaving(TDatabaseEntity databaseEntity, TEntity entity)
        {
        }

        protected virtual Task AfterSavingAsync(TDatabaseEntity databaseEntity, TEntity entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual TEntityHandlerResponse CreateResponse(TDatabaseEntity databaseEntity, TEntity entity)
        {
            return new TEntityHandlerResponse()
            {
                Entity = entity,
            };
        }
    }
}
