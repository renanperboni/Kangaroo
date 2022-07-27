// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Playground.Tests
{
    using Kangaroo.Models;
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;
    using Kangaroo.Playground.Entities;
    using Kangaroo.Playground.Infrastructure.DatabaseEntities;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories;
    using Kangaroo.Playground.Services;
    using Moq;

    public class CountryTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task ShouldFindCountryById()
        {
            var expectedCountryId = Guid.NewGuid();

            var applicationDatabaseRepositoryMock = new Mock<IApplicationDatabaseRepository>();

            var countries = new List<TbCountry>()
            {
                new TbCountry() { CountryId = Guid.NewGuid() },
                new TbCountry() { CountryId = expectedCountryId },
            };

            TbCountry selectedCountry = new TbCountry();

            applicationDatabaseRepositoryMock.Setup(x => x.GetByConditionAsync<TbCountry, Country>(It.IsAny<Func<IQueryable<TbCountry>, IQueryable<TbCountry>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<IQueryable<TbCountry>, IQueryable<TbCountry>>, CancellationToken>((queryable, cancellationToken) =>
                {
                    selectedCountry = queryable.Invoke(countries.AsQueryable()).FirstOrDefault();
                })
                .ReturnsAsync(() => new List<Country>() { new Country() { CountryId = selectedCountry.CountryId } });

            var countryGetterService = new CountryGetterService(applicationDatabaseRepositoryMock.Object);
            var result = await countryGetterService.GetAsync(new CountryGetterRequest() { CountryId = expectedCountryId });

            Assert.That(result.Entity.CountryId, Is.EqualTo(expectedCountryId));
        }

        [Test]
        public async Task ShouldFindCountryByIdUsingExtensions()
        {
            var expectedCountryId = Guid.NewGuid();

            var applicationDatabaseRepositoryMock = new Mock<IApplicationDatabaseRepository>();
            applicationDatabaseRepositoryMock.SetupGetByCondition<TbCountry, Country>(expectedCountryId);

            var countryGetterService = new CountryGetterService(applicationDatabaseRepositoryMock.Object);
            var result = await countryGetterService.GetAsync(new CountryGetterRequest() { CountryId = expectedCountryId });

            Assert.That(result.Entity.CountryId, Is.EqualTo(expectedCountryId));
        }
    }

    public static class ApplicationDatabaseRepositoryMockExtensions
    {
        public static void SetupGetByCondition<TDatabaseEntity, TEntity>(this Mock<IApplicationDatabaseRepository> mock, Guid expectedId)
            where TDatabaseEntity : class, IDatabaseEntity, new()
            where TEntity : class, IEntity, new()
        {
            var records = new List<TDatabaseEntity>();

            var firstRecord = new TDatabaseEntity();

            if (firstRecord is IHasGuidKey firstKey)
            {
                firstKey.SetKey(Guid.NewGuid());

                records.Add(firstRecord);
            }

            var secondRecord = new TDatabaseEntity();

            if (secondRecord is IHasGuidKey secondKey)
            {
                secondKey.SetKey(expectedId);

                records.Add(secondRecord);
            }

            var selectedRecord = new TDatabaseEntity();

            mock.Setup(x => x.GetByConditionAsync<TDatabaseEntity, TEntity>(It.IsAny<Func<IQueryable<TDatabaseEntity>, IQueryable<TDatabaseEntity>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<IQueryable<TDatabaseEntity>, IQueryable<TDatabaseEntity>>, CancellationToken>((queryable, cancellationToken) =>
                {
                    selectedRecord = queryable.Invoke(records.AsQueryable()).FirstOrDefault();
                })
                .ReturnsAsync(() =>
                {
                    var entity = new TEntity();

                    if (selectedRecord is IHasGuidKey selectedKey && entity is IHasGuidKey entityKey)
                    {
                        entityKey.SetKey(selectedKey.GetKey());
                    }

                    return new List<TEntity>() { entity };
                });
        }
    }
}