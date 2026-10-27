// <copyright file="SeleniumRunnerTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System.Diagnostics;
    using Xunit;

    [Trait("Category", "Offline")]
    public sealed class SeleniumRunnerTests
    {
        [Theory]
        [InlineData("local", "", "Category=Referral|Category=LocalEmail|Category=LocalBrowser")]
        [InlineData("local", "true", "Category=Referral|Category=LocalEmail|Category=LocalBrowser")]
        [InlineData("local", "TRUE", "Category=Referral|Category=LocalEmail|Category=LocalBrowser")]
        [InlineData("local", "false", "Category=Referral|Category=LocalEmail")]
        [InlineData("local-email", "true", "Category=LocalEmail")]
        [InlineData("developer", "true", "Category=Referral|Category=LivePayment")]
        [InlineData("offline", "true", "Category=Offline")]
        public async Task Runner_SelectsOnlyTheRequestedTargetCases(string selection, string selfContained, string expectedFilter)
        {
            var result = await RunSelectionAsync(selection, selfContained);
            Assert.Equal(0, result.ExitCode);
            string[] arguments = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int filterIndex = Array.IndexOf(arguments, "--filter");
            Assert.True(filterIndex >= 0);
            Assert.Equal(expectedFilter, arguments[filterIndex + 1]);
            Assert.Contains("--list-tests", arguments);
            Assert.Equal(1, arguments.Count(argument => argument == "--filter"));
        }

        [Theory]
        [InlineData("--filter=Category=LivePayment")]
        [InlineData("--filter")]
        [InlineData("--settings=unsafe.runsettings")]
        [InlineData("-s")]
        public async Task Runner_RejectsArgumentsThatCouldOverrideItsSafetyFilter(string argument)
        {
            var result = await RunSelectionAsync("local", "true", argument);
            Assert.Equal(2, result.ExitCode);
            Assert.Empty(result.Output);
        }

        private static async Task<(int ExitCode, string Output)> RunSelectionAsync(string selection, string selfContained, string? extra = null)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "scripts", "test-selenium.sh")))
            {
                root = root.Parent;
            }

            Assert.NotNull(root);
            string temporary = Directory.CreateTempSubdirectory("selenium-runner-").FullName;
            try
            {
                string fakeDotnet = Path.Combine(temporary, "dotnet");
                await File.WriteAllTextAsync(fakeDotnet, "#!/bin/bash\nprintf '%s\\n' \"$@\"\n");
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(fakeDotnet, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }

                var start = new ProcessStartInfo("bash")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                start.ArgumentList.Add(Path.Combine(root.FullName, "scripts", "test-selenium.sh"));
                start.ArgumentList.Add(selection);
                start.ArgumentList.Add("--list-tests");
                if (extra != null)
                {
                    start.ArgumentList.Add(extra);
                }

                start.Environment["PATH"] = temporary + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
                start.Environment["SeleniumTest__Local__SelfContained"] = selfContained;
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    await process.WaitForExitAsync(deadline.Token);
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }

                await error;
                return (process.ExitCode, await output);
            }
            finally
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }
}
