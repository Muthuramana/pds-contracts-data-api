﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pds.Contracts.Data.Repository.Context;
using Pds.Contracts.Data.Repository.DependencyInjection;
using Pds.Contracts.Data.Services.Implementations;
using Pds.Contracts.Data.Services.Interfaces;

namespace Pds.Contracts.Data.Services.DependencyInjection
{
    /// <summary>
    /// Extensions class for <see cref="IServiceCollection"/> for registering the feature's services.
    /// </summary>
    public static class FeatureServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for the current feature to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add the feature's services to.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>
        /// A reference to this instance after the operation has completed.
        /// </returns>
        public static IServiceCollection AddFeatureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PdsContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("contracts"));
            });

            services.AddRepositoriesServices(configuration);
            services.AddAutoMapper(typeof(FeatureServiceCollectionExtensions).Assembly);

            services.AddScoped<IContractService, ContractService>();

            return services;
        }
    }
}