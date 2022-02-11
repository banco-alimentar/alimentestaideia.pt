// -----------------------------------------------------------------------
// <copyright file="TenantNotFoundException.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core
{
    using System;

    /// <summary>
    /// Tenant not found exception.
    /// </summary>
    [Serializable]
    public class TenantNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
        /// </summary>
        public TenantNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public TenantNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public TenantNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected TenantNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
