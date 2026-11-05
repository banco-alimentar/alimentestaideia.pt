// -----------------------------------------------------------------------
// <copyright file="GroupOverview.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Snapshot of every duplicate group in the target database.
    /// </summary>
    /// <param name="Groups">Groups ordered by ID.</param>
    /// <param name="LoadedAtUtc">When the snapshot was taken.</param>
    /// <param name="Warnings">Consistency warnings.</param>
    public sealed record GroupOverview(IReadOnlyList<AnalyzedGroup> Groups, DateTime LoadedAtUtc, IReadOnlyList<string> Warnings);
}
