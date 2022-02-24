// -----------------------------------------------------------------------
// <copyright file="NifApiValidatorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// This class defines unit tests for NifApiValidator.
    /// </summary>
    public class NifApiValidatorTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly NifApiValidator nifApiValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="NifApiValidatorTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Service list.</param>
        public NifApiValidatorTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.nifApiValidator = this.fixture.ServiceProvider.GetRequiredService<NifApiValidator>();
        }

        /// <summary>
        /// Test if NifApivalidator returns null if null is passed.
        /// </summary>
        [Fact]
        public void TestNullNIF()
        {
            bool result = this.nifApiValidator.IsValidNif(null);
            Assert.False(result);
        }

        /// <summary>
        /// Test if NifApivalidator returns true with a valid NIFs.
        /// </summary>
        [Fact]
        public void TestValidNIFs()
        {
            bool result;
            string[] valid_nifs = { "505030799", "509639038", "510075983", "505945789", "506094200", "510622810", "509899595", "508321239", "505331063", "504496522", "504776126", "509281508", "509002650", "505061899", "505847183", "506618455", "503997617", "508438063", "502447176", "504123408", "510397433", "509985092", "505055279", "501483314", "507749170", "505186543", "505640872", "500227306", "504558706", "502657057", "163676895" };
            foreach (string nif in valid_nifs)
            {
                result = this.nifApiValidator.IsValidNif(nif);
                Assert.True(result);
            }
        }

        /// <summary>
        /// Test if NifApivalidator returns true with a valid International NIFs.
        /// </summary>
        [Fact]
        public void TestValidInternationalNIF()
        {
            bool result = this.nifApiValidator.IsValidNif("303372702");
            Assert.True(result);
        }

        /// <summary>
        /// Test if NifApivalidator returns false with a INvalid NIFs.
        /// </summary>
        [Fact]
        public void TestInValidNIFs()
        {
            bool result;
            string[] valid_nifs = { "505030790", "509639031", "510075982", "505945781", "506094202", "510622811", "509899592", "508321233", "505331061", "504496521", "123", "123123123", "12312312" };
            foreach (string nif in valid_nifs)
            {
                result = this.nifApiValidator.IsValidNif(nif);
                Assert.False(result);
            }
        }
    }
}
