// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Infrastructure.DatabaseRepositories
{
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;
    using Microsoft.EntityFrameworkCore;

    public interface IDatabaseRepository<TDbContext>
        where TDbContext : DbContext
    {
        public TDatabaseEntity ApplyChanges<TDatabaseEntity, TEntity>(TEntity entity)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;

        public Task SaveAsync(CancellationToken cancellationToken = default);

        public Task<IList<TEntity>> GetAllAsync<TDatabaseEntity, TEntity>(CancellationToken cancellationToken = default)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;

        public Task<IList<TEntity>> GetByConditionAsync<TDatabaseEntity, TEntity>(Func<IQueryable<TDatabaseEntity>, IQueryable<TDatabaseEntity>> queryable, CancellationToken cancellationToken = default)
            where TDatabaseEntity : class, IDatabaseEntity
            where TEntity : class, IEntity;
    }
}