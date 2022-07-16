// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.Services
{
    using System.Threading.Tasks;
    using Kangaroo.Playground.Entities;
    using Kangaroo.Playground.Infrastructure.DatabaseEntities;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories;
    using Kangaroo.Services;

    public partial class CountriesGetterService : EntitiesGetterService<Country, CountriesGetterRequest, CountriesGetterResponse>
    {
        private readonly IApplicationDatabaseRepository applicationDatabaseRepository;

        public CountriesGetterService(IApplicationDatabaseRepository applicationDatabaseRepository)
        {
            this.applicationDatabaseRepository = applicationDatabaseRepository;
        }

        protected override async Task<IList<Country>> GetEntitiesAsync(CountriesGetterRequest entityGetterRequest, CancellationToken cancellationToken = default)
        {
            return await this.applicationDatabaseRepository.GetAllAsync<TbCountry, Country>(cancellationToken);
        }
    }
}
