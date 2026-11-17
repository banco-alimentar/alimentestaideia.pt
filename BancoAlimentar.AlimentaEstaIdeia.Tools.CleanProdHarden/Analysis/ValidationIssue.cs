// -----------------------------------------------------------------------
// <copyright file="ValidationIssue.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// A blocking validation failure. Codes match the SQL script's THROW numbers.
    /// </summary>
    /// <param name="Code">SQL script error number.</param>
    /// <param name="Message">Explanation.</param>
    public sealed record ValidationIssue(int Code, string Message);
}
