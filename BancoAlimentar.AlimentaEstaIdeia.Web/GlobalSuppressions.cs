// -----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Not wanting this")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "ASP.NET Core")]
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This code generate Images using System.Drawing and only works on Windows", Scope = "Type", Target = "~T:BancoAlimentar.AlimentaEstaIdeia.Web.Pages.QrCodeGeneratorModel")]
