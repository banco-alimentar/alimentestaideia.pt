// -----------------------------------------------------------------------
// <copyright file="RuntimeCacheRevision.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;

    /// <summary>
    /// A durable cache invalidation signal shared by instances in one environment.
    /// </summary>
    public class RuntimeCacheRevision
    {
        /// <summary>Gets or sets the environment owning this signal.</summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>Gets or sets the last published revision.</summary>
        public Guid Revision { get; set; }

        /// <summary>Gets or sets the publication time.</summary>
        public DateTimeOffset PublishedAt { get; set; }
    }
}
