// Licensed to Kangaroo under one or more agreements.
// We license this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Infrastructure.DatabaseRepositories
{
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entitites;
    using Microsoft.EntityFrameworkCore;

    public interface IRepository<TDbContext>
        where TDbContext : DbContext
    {
        public TDatabaseEntity ApplyChanges<TDatabaseEntity, TEntity>(TEntity entity)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;

        public Task SaveAsync();

        public Task<IList<TEntity>> GetAllAsync<TDatabaseEntity, TEntity>()
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;

        public Task<IList<TEntity>> GetByConditionAsync<TDatabaseEntity, TEntity>(Func<IQueryable<TDatabaseEntity>, IQueryable<TDatabaseEntity>> queryable)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;
    }
}