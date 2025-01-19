// -----------------------------------------------------------------------
// <copyright file="FunctionInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Reflection;
    using Azure.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    internal class FunctionInitializer
    {
        /// <summary>
        /// Create the unit of work.
        /// </summary>
        /// <returns>Tuple.</returns>
        public static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(
            TelemetryClient telemetryClient,
            IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(context, telemetryClient, null, new NifApiValidator());
            return (unitOfWork, context);
        }
    }
}
