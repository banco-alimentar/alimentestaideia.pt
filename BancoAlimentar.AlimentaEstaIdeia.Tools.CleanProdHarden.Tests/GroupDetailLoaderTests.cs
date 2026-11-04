// -----------------------------------------------------------------------
// <copyright file="GroupDetailLoaderTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using Xunit;

    /// <summary>
    /// Tests how raw <c>FOR JSON</c> values are shown.
    /// </summary>
    public class GroupDetailLoaderTests
    {
        /// <summary>
        /// <c>real</c> and <c>float</c> columns arrive in exponent form and are shown as plain decimals;
        /// other numbers, and numbers outside the decimal range, are unchanged.
        /// </summary>
        [Theory]
        [InlineData("1.5000000e+001", "15")]
        [InlineData("1.2000000e-001", "0.12")]
        [InlineData("1.1000000e-001", "0.11")]
        [InlineData("0.0000000e+000", "0")]
        [InlineData("1.0040000e+001", "10.04")]
        [InlineData("-1.5000000e+001", "-15")]
        [InlineData("1.0000000e-007", "0.0000001")]
        [InlineData("1.234567891250000e+008", "123456789.125")]
        [InlineData("3.4028235e+038", "3.4028235e+038")]
        [InlineData("2.30", "2.30")]
        [InlineData("45552", "45552")]
        public void FormatRawNumberShowsPlainDecimals(string rawText, string expected)
        {
            Assert.Equal(expected, GroupDetailLoader.FormatRawNumber(rawText));
        }
    }
}
