// -----------------------------------------------------------------------
// <copyright file="TestFileWatching.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;

    /// <summary>
    /// Keeps test hosts from consuming Linux inotify instances.
    /// </summary>
    internal static class TestFileWatching
    {
        /// <summary>
        /// Environment variable read by every <c>PhysicalFileProvider</c> when it is constructed.
        /// </summary>
        internal const string PollingVariable = "DOTNET_USE_POLLING_FILE_WATCHER";

        /// <summary>
        /// Makes file providers created later in this process poll instead of opening inotify watchers.
        /// Every test host watches its web root (for example through <c>asp-append-version</c>), and those
        /// watchers outlive the tests, exhausting the agent user's inotify instance limit (128) on Linux.
        /// An explicit value, such as one set by the .NET SDK container images, is preserved.
        /// </summary>
        internal static void UsePolling()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(PollingVariable)))
            {
                Environment.SetEnvironmentVariable(PollingVariable, "true");
            }
        }
    }
}
