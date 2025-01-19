// -----------------------------------------------------------------------
// <copyright file="InvoiceStatusResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Invoice status result when calling generate invoice.
    /// </summary>
    public enum InvoiceStatusResult
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// The donation is not payed.
        /// </summary>
        NotPayed = 1,

        /// <summary>
        /// Donation id not found.
        /// </summary>
        DonationNotFound = 2,

        /// <summary>
        /// The user belongs to the donation is not found.
        /// </summary>
        DonationUserNotFound = 3,

        /// <summary>
        /// Confirmed payment is null.
        /// </summary>
        ConfirmedPaymentIsNull = 4,

        /// <summary>
        /// The donation is one year old.
        /// </summary>
        DonationIsOneYearOld = 5,

        /// <summary>
        /// Nif is not valid.
        /// </summary>
        NifNotValid = 6,

        /// <summary>
        /// Donation has a confirmed failed payment status.
        /// </summary>
        ConfirmedFailedPaymentStatus = 7,

        /// <summary>
        /// Invoice generated ok.
        /// </summary>
        GeneratedOk = 8,

        /// <summary>
        /// The invoice was cancelled.
        /// </summary>
        InvoiceCanceled = 9,
    }
}
