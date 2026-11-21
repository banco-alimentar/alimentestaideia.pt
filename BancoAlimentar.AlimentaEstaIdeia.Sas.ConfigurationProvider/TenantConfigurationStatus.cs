// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;

    /// <summary>
    /// A secret-free snapshot of tenant configuration readiness.
    /// </summary>
    public sealed record TenantConfigurationStatus(
        int TenantId,
        string? TenantName,
        Guid Generation,
        bool BootstrapReady,
        bool ClientReady,
        bool SecretsLoaded,
        DateTimeOffset? LastLoadedAt,
        string? FailureStage,
        DateTimeOffset? RetryAt);
}
