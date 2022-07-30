// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.API.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Kangaroo.Infrastructure.DatabaseRepositories;
    using Kangaroo.Models.OptionsSettings;
    using Kangaroo.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKangarooServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.Scan(x =>
                x.FromAssemblies(assemblies)
                .AddClasses(y =>
                    y.AssignableTo<ITransientService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            services.Scan(x =>
                x.FromAssemblies(assemblies)
                .AddClasses(y =>
                    y.AssignableTo<IScopedService>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(x =>
                x.FromAssemblies(assemblies)
                .AddClasses(y =>
                    y.AssignableTo<ISingletonService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            return services;
        }

        public static IServiceCollection AddKangarooDatabaseRepositories(this IServiceCollection services, params Assembly[] assemblies)
        {
            return services.Scan(x =>
                x.FromAssemblies(assemblies)
                .AddClasses(y =>
                    y.AssignableTo(typeof(IDatabaseRepository<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());
        }

        public static IServiceCollection AddKangarooAuthenticationJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = new JwtOptions();
            configuration.GetSection(JwtOptions.Jwt).Bind(jwtOptions);

            var validIssuer = jwtOptions.JwtIssuer;
            var validAudience = jwtOptions.JwtAudience;
            var secretKey = jwtOptions.JwtSecurityKey;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = validIssuer,
                        ValidAudience = validAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    };
                });

            return services;
        }

        public static IServiceCollection AddKangarooRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var distributedRedisCacheOptions = new DistributedRedisCacheOptions();
            configuration.GetSection(DistributedRedisCacheOptions.DistributedRedisCache).Bind(distributedRedisCacheOptions);
            services.AddDistributedRedisCache(x =>
            {
                x.Configuration = distributedRedisCacheOptions.ConnectionString;
                x.InstanceName = distributedRedisCacheOptions.InstanceName;
            });

            return services;
        }
    }
}
