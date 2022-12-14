// <autogenerated>


namespace Kangaroo.Playground.Entities.Validators
{
    using FluentValidation;

    public partial class CountryHandlerRequestValidator : AbstractValidator<CountryHandlerRequest>
    {
        partial void SetCustomRules()
        {
            //RuleFor(x => x.Entity).SetValidator(x => new CountryValidator());
        }
    }
}

namespace Kangaroo.Playground.API
{
    using AutoMapper;
    using FluentValidation;
    using FluentValidation.AspNetCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories.DBContexts;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories.EntityTypeConfiguration;
    using Kangaroo.Playground.Infrastructure.DatabaseRepositories.Mapper;
    using Kangaroo.Playground.Infrastructure.DatabaseEntities;
    using Kangaroo.Playground.Entities;
    using Kangaroo.Playground.Entities.Validators;
    using Kangaroo.Playground.Services;
    using Kangaroo.Models.DatabaseEntities;
    using Kangaroo.Models.Entities;
    using Kangaroo.Models;
    using Kangaroo.Exceptions;
    using Kangaroo.API.Middlewares;
    using Kangaroo.API.Extensions;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Kangaroo.API.ActionFilters;
    using Kangaroo.Models.OptionsSettings;

    #region API

    public static class APIExtensions
    {
        public static void AddServiceCollection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddKangarooServices(typeof(APIExtensions).Assembly);
            services.AddKangarooDatabaseRepositories(typeof(APIExtensions).Assembly);
            services.AddKangarooAuthenticationJwt(configuration);
            services.AddKangarooRedis(configuration);

            services.AddDbContext<ApplicationDbContext>(x => x.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Database=MyDbTest;Trusted_Connection=True;",
                    y => y.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking))
                .AddLogging(x => x.AddDebug())
                .AddAutoMapper(typeof(ApplicationAutoMapperProfile));

            services.AddMvc(x =>
            {
                x.Filters.Add(typeof(KangarooAuthorizationActionFilter));
                x.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            }).AddFluentValidation();

            services.AddCors(policy =>
            {
                policy.AddPolicy("CorsPolicy", opt => opt
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Pagination"));
            });

            services.AddValidatorsFromAssembly(typeof(APIExtensions).Assembly);
        }

        public static async Task ConfigureDatabase(this IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var myDbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

                await myDbContext.Database.EnsureDeletedAsync();
                await myDbContext.Database.EnsureCreatedAsync();

                myDbContext.Countries.Add(new TbCountry { Name = "Country 1", CreatedByUserName = "Admin", CreatedAt = DateTimeOffset.Now });
                myDbContext.Countries.Add(new TbCountry { Name = "Country 2", CreatedByUserName = "Admin", CreatedAt = DateTimeOffset.Now });
                myDbContext.Countries.Add(new TbCountry { Name = "Country 3", CreatedByUserName = "Admin", CreatedAt = DateTimeOffset.Now });
                await myDbContext.SaveChangesAsync();
                myDbContext.ChangeTracker.Clear();
            }
        }
    }

    [ApiController]
    [Route("[controller]/[action]")]
    public partial class CountryController : ControllerBase
    {
        private readonly ILogger<CountryController> logger;
        private readonly ICountryHandlerService countryHandlerService;

        public CountryController(
            ILogger<CountryController> logger,
            ICountryHandlerService countryHandlerService)
        {
            this.logger = logger;
            this.countryHandlerService = countryHandlerService;
        }

        [HttpPost]
        public async Task<IActionResult> PostNewCountry(CountryHandlerRequest country, CancellationToken cancellationToken)
        {
            await this.countryHandlerService.SaveAsync(country);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var countryHandlerRequest = new CountryHandlerRequest()
            {
                Entity = new Country()
                {
                    CountryId = Guid.NewGuid(),
                    Name = "This is my correct name",
                    DataState = DataState.Updated,
                }
            };

            try
            {
                await this.countryHandlerService.SaveAsync(countryHandlerRequest);
            }
            catch (DbUpdateConcurrencyException concurrencyException)
            {
            }
            catch (Exception ex)
            {

            }

            return Ok();
        }
    }

    public partial class CountryController : ControllerBase
    {

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PostAnotherNewCountry([FromQuery] CountryHandlerRequest country, CancellationToken cancellationToken)
        {
            return Ok(await this.countryHandlerService.SaveAsync(country, cancellationToken));
        }
    }
    #endregion API
}
