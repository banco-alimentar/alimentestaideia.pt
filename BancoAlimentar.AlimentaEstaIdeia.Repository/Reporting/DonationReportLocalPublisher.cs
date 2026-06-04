// -----------------------------------------------------------------------
// <copyright file="DonationReportLocalPublisher.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Writes generated report files to a local directory (for development).
    /// </summary>
    public class DonationReportLocalPublisher
    {
        /// <summary>
        /// Writes report files to a directory.
        /// </summary>
        /// <param name="outputDirectory">Target folder (e.g. Web wwwroot/report).</param>
        /// <param name="files">Map of relative file path to content.</param>
        /// <returns>Number of files written.</returns>
        public int PublishToDirectory(string outputDirectory, IReadOnlyDictionary<string, string> files)
        {
            Directory.CreateDirectory(outputDirectory);
            int written = 0;

            foreach (KeyValuePair<string, string> file in files)
            {
                string targetPath = Path.Combine(outputDirectory, file.Key.Replace('/', Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(targetPath, file.Value, Encoding.UTF8);
                written++;
            }

            return written;
        }
    }
}
