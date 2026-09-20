// -----------------------------------------------------------------------
// <copyright file="FiscalReservationSeed.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    /// <summary>
    /// Stores IDs created for a concurrent fiscal reservation test.
    /// </summary>
    /// <param name="SeriesId">The series identifier.</param>
    /// <param name="FirstDonationId">The first donation identifier.</param>
    /// <param name="SecondDonationId">The second donation identifier.</param>
    public sealed record FiscalReservationSeed(int SeriesId, int FirstDonationId, int SecondDonationId);
}
