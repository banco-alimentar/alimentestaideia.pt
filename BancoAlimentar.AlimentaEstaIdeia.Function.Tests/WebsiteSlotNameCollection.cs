// -----------------------------------------------------------------------
// <copyright file="WebsiteSlotNameCollection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using Xunit;

    /// <summary>
    /// Serializes tests that set or depend on the process-wide WEBSITE_SLOT_NAME variable.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class WebsiteSlotNameCollection
    {
        /// <summary>
        /// Collection name.
        /// </summary>
        public const string Name = "WEBSITE_SLOT_NAME";
    }
}
