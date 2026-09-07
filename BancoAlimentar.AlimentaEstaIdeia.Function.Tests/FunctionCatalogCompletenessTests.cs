// -----------------------------------------------------------------------
// <copyright file="FunctionCatalogCompletenessTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Azure.Functions.Worker;
    using Xunit;

    /// <summary>
    /// Prevents Azure Functions or localization entries from silently being omitted from the Admin catalog.
    /// </summary>
    public class FunctionCatalogCompletenessTests
    {
        private static readonly string[] ResourceFiles =
        {
            "AdminSharedResources.resx",
            "AdminSharedResources.en.resx",
            "AdminSharedResources.es.resx",
            "AdminSharedResources.fr.resx",
        };

        /// <summary>
        /// Every timer function declaration must have catalog metadata.
        /// </summary>
        [Fact]
        public void TimerFunctionDeclarations_AreRegisteredInCatalog()
        {
            HashSet<string> declaredFunctions = typeof(MultiTenantFunction).Assembly
                .GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Select(method => new
                {
                    Method = method,
                    Attribute = method.GetCustomAttribute<FunctionAttribute>(),
                })
                .Where(item => item.Attribute != null
                    && item.Method.GetParameters().Any(parameter => parameter.GetCustomAttributes()
                        .Any(attribute => string.Equals(attribute.GetType().Name, "TimerTriggerAttribute", StringComparison.Ordinal))))
                .Select(item => item.Attribute.Name)
                .ToHashSet(StringComparer.Ordinal);

            HashSet<string> catalogFunctions = FunctionExecutionReportCatalog.All
                .Select(item => item.FunctionKey)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Equal(catalogFunctions, declaredFunctions);
            Assert.Equal(5, catalogFunctions.Count);
        }

        /// <summary>
        /// The catalog must expose stable metadata for each function and scope.
        /// </summary>
        [Fact]
        public void CatalogEntries_HaveRequiredMetadata()
        {
            Assert.All(FunctionExecutionReportCatalog.All, descriptor =>
            {
                Assert.False(string.IsNullOrWhiteSpace(descriptor.FunctionKey));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.DisplayNameResourceKey));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.DescriptionResourceKey));
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Schedule));
                Assert.True(descriptor.ScopeKind == FunctionExecutionReportScopeKind.Tenant
                    || descriptor.ScopeKind == FunctionExecutionReportScopeKind.Global);
            });
        }

        /// <summary>
        /// Each catalog display and description key must exist in every supported resource set.
        /// </summary>
        [Fact]
        public void CatalogLocalizationKeys_ExistInAllAdminResourceFiles()
        {
            string repositoryRoot = FindRepositoryRoot();
            var requiredKeys = FunctionExecutionReportCatalog.All
                .SelectMany(item => new[] { item.DisplayNameResourceKey, item.DescriptionResourceKey })
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (string resourceFile in ResourceFiles)
            {
                string path = Path.Combine(repositoryRoot, "BancoAlimentar.AlimentaEstaIdeia.Web", "Resources", resourceFile);
                Assert.True(File.Exists(path), $"Missing resource file: {path}");
                string content = File.ReadAllText(path);

                foreach (string key in requiredKeys)
                {
                    Assert.Contains($"name=\"{key}\"", content, StringComparison.Ordinal);
                }
            }
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
