// -----------------------------------------------------------------------
// <copyright file="StaticFileMapper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Microsoft.AspNetCore.Http;

    /// <inheritdoc />
    public class StaticFileMapper : IStaticFileMapper
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileMapper"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        public StaticFileMapper(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Uri MapStaticFile(string filePath)
        {
            BlobContainerClient? client = this.httpContextAccessor.GetBlobServiceClient();
            filePath = string.Concat("/wwwroot", filePath);
            return client!.GetBlobClient(filePath).Uri;
        }
    }
}
