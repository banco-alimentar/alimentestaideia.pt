// -----------------------------------------------------------------------
// <copyright file="NifApiValidatorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System.Threading.Tasks;
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
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Test_Null_NIF()
        {
            bool result = await this.nifApiValidator.IsValidNif(null);
            Assert.False(result);
        }

        /// <summary>
        /// Test if NifApivalidator returns true with a valid NIFs.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Test_Valid_NIFs()
        {
            // https://www.nif.pt/?q=196807050
            bool result = await this.nifApiValidator.IsValidNif("196807050");
            Assert.True(result);
        }

        /// <summary>
        /// Test if NifApivalidator returns true with a valid International NIFs.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Test_Valid_International_NIF()
        {
            // https://www.nif.pt/?q=303372702
            bool result = await this.nifApiValidator.IsValidNif("303372702");
            Assert.True(result);
        }

        /// <summary>
        /// Test if NifApivalidator returns false with a INvalid NIFs.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task TestInValid_NIFs()
        {
            bool result = await this.nifApiValidator.IsValidNif("196807051");
            Assert.False(result);

            result = await this.nifApiValidator.IsValidNif("303372703");
            Assert.False(result);
        }
    }
}
