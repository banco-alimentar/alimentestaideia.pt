// -----------------------------------------------------------------------
// <copyright file="PaymentDependency.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Rows in another table that reference a payment through a foreign key.
    /// </summary>
    /// <param name="PaymentId">Referenced payment.</param>
    /// <param name="ReferencingTable">Schema-qualified referencing table.</param>
    /// <param name="ReferencingColumn">Referencing column.</param>
    /// <param name="DeleteAction">Foreign key delete action.</param>
    /// <param name="ReferenceCount">Number of referencing rows.</param>
    public sealed record PaymentDependency(int PaymentId, string ReferencingTable, string ReferencingColumn, string DeleteAction, long ReferenceCount);
}
