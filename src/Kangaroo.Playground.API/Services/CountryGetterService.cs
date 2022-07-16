// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.Services
{
    using System.Threading.Tasks;
    using Kangaroo.Playground.Entities;
    using Kangaroo.Playground.Infrastructure.DatabaseEntities;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories;
    using Kangaroo.Services;

    public partial class CountryGetterService : EntityGetterService<Country, CountryGetterRequest, CountryGetterResponse>
    {
        private readonly IApplicationDatabaseRepository applicationDatabaseRepository;

        public CountryGetterService(IApplicationDatabaseRepository applicationDatabaseRepository)
        {
            this.applicationDatabaseRepository = applicationDatabaseRepository;
        }

        protected override async Task<Country> GetEntityAsync(CountryGetterRequest entityGetterRequest, CancellationToken cancellationToken = default)
        {
            return (await this.applicationDatabaseRepository.GetByConditionAsync<TbCountry, Country>(
                x => x.Where(y => y.CountryId == entityGetterRequest.CountryId), cancellationToken))?.FirstOrDefault();
        }
    }
}
