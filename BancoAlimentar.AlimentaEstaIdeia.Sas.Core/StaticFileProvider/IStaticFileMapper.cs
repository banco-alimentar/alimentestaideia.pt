// -----------------------------------------------------------------------
// <copyright file="IStaticFileMapper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Static file mapper.
/// </summary>
public interface IStaticFileMapper
{
    /// <summary>
    /// Map the local file to the Azure Storage File.
    /// </summary>
    /// <param name="filePath">Expected local file.</param>
    /// <returns>A <see cref="Uri"/> that contains the Azure Storage File.</returns>
    Uri MapStaticFile(string filePath);
}
