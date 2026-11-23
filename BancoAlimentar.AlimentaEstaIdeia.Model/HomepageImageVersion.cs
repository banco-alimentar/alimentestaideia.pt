// -----------------------------------------------------------------------
// <copyright file="HomepageImageVersion.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// An uploaded version of the homepage central image.
    /// </summary>
    public class HomepageImageVersion
    {
        /// <summary>
        /// Gets or sets the image version identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the generated server-side file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the content type of the image.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the UTC upload date and time.
        /// </summary>
        public DateTime UploadedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the authenticated account identifier of the uploader.
        /// </summary>
        public string UploadedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the uploader name captured at upload time.
        /// </summary>
        public string UploadedByName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this version is currently selected.
        /// </summary>
        public bool IsCurrent { get; set; }
    }
}
