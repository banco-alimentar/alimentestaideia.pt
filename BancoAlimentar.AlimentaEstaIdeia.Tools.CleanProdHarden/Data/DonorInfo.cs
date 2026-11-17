// -----------------------------------------------------------------------
// <copyright file="DonorInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    /// <summary>
    /// Donor details.
    /// </summary>
    /// <param name="UserId">User ID.</param>
    /// <param name="FullName">Full name.</param>
    /// <param name="Email">Email.</param>
    /// <param name="Nif">Tax number.</param>
    /// <param name="PhoneNumber">Phone number.</param>
    /// <param name="CompanyName">Company name.</param>
    /// <param name="IsAnonymous">Whether the user is the anonymous donor.</param>
    public sealed record DonorInfo(string UserId, string? FullName, string? Email, string? Nif, string? PhoneNumber, string? CompanyName, bool IsAnonymous);
}
