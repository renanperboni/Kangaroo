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
    using Kangaroo.Services;
    using Microsoft.Extensions.DependencyInjection;

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

        public static IServiceCollection AddKangarooCurrentUserService(this IServiceCollection services, Type implementationType)
        {
            return services.AddTransient(typeof(ICurrentUserService), implementationType);
        }
    }
}
