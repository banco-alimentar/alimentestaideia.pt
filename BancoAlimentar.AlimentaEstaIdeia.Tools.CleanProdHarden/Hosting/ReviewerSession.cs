// -----------------------------------------------------------------------
// <copyright file="ReviewerSession.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting
{
    using System;

    /// <summary>
    /// Holds the reviewer name for one browser circuit.
    /// </summary>
    public sealed class ReviewerSession
    {
        /// <summary>
        /// Browser storage key.
        /// </summary>
        public const string StorageKey = "cleanprodharden.reviewer";

        private string reviewerName = string.Empty;

        /// <summary>
        /// Raised when the reviewer name changes.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Gets or sets the reviewer name.
        /// </summary>
        public string ReviewerName
        {
            get => this.reviewerName;
            set
            {
                string normalized = (value ?? string.Empty).Trim();
                if (normalized == this.reviewerName)
                {
                    return;
                }

                this.reviewerName = normalized;
                this.Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reviewer entered a name.
        /// </summary>
        public bool HasReviewer => this.reviewerName.Length > 0;
    }
}
