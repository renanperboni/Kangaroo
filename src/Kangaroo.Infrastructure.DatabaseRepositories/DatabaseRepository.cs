// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Infrastructure.DatabaseRepositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Kangaroo.Models;
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;
    using Microsoft.EntityFrameworkCore;

    public abstract class DatabaseRepository<TDbContext> : IDatabaseRepository<TDbContext>
        where TDbContext : DbContext
    {
        private readonly TDbContext dbContext;
        private readonly IMapper mapper;

        public DatabaseRepository(TDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public TDatabaseEntity ApplyChanges<TDatabaseEntity, TEntity>(TEntity entity)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity
        {
            var databaseEntity = this.mapper.Map<TDatabaseEntity>(entity);
            this.dbContext.ChangeTracker.TrackGraph(databaseEntity, x =>
            {
                if (x.Entry.State == EntityState.Detached
                    && x.Entry.Entity is IHasDataState trackedEntity)
                {
                    switch (trackedEntity.DataState)
                    {
                        case DataState.Unchanged:
                            x.Entry.State = EntityState.Unchanged;
                            break;
                        case DataState.Inserted:
                            x.Entry.State = EntityState.Added;
                            break;
                        case DataState.Updated:
                            x.Entry.State = EntityState.Modified;
                            break;
                        case DataState.Deleted:
                            x.Entry.State = EntityState.Deleted;
                            break;
                        default:
                            break;
                    }
                }
            });

            return databaseEntity;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IList<TEntity>> GetAllAsync<TDatabaseEntity, TEntity>(CancellationToken cancellationToken = default)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity
        {
            return await this.mapper.ProjectTo<TEntity>(this.dbContext.Set<TDatabaseEntity>().AsQueryable()).ToListAsync(cancellationToken);
        }

        public async Task<IList<TEntity>> GetByConditionAsync<TDatabaseEntity, TEntity>(Func<IQueryable<TDatabaseEntity>, IQueryable<TDatabaseEntity>> queryable, CancellationToken cancellationToken = default)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity
        {
            return await this.mapper.ProjectTo<TEntity>(queryable(this.dbContext.Set<TDatabaseEntity>().AsQueryable())).ToListAsync(cancellationToken);
        }
    }
}
