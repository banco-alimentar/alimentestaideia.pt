// -----------------------------------------------------------------------
// <copyright file="ExecutionAbortedException.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Stops an execution and rolls back its transaction.
    /// </summary>
    internal sealed class ExecutionAbortedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionAbortedException"/> class.
        /// </summary>
        /// <param name="issues">Blocking issues.</param>
        public ExecutionAbortedException(IReadOnlyList<ValidationIssue> issues)
            : base(issues.Count > 0 ? issues[0].Message : "Execution aborted.")
        {
            this.Issues = issues;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionAbortedException"/> class.
        /// </summary>
        /// <param name="code">Issue code.</param>
        /// <param name="message">Issue message.</param>
        public ExecutionAbortedException(int code, string message)
            : this(new[] { new ValidationIssue(code, message) })
        {
        }

        /// <summary>
        /// Gets the blocking issues.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues { get; }
    }
}
