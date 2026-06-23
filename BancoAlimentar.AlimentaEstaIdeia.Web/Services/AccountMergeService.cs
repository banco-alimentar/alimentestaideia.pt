// -----------------------------------------------------------------------
// <copyright file="AccountMergeService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Evaluates and performs safe merges when an external login is already linked to another account.
    /// </summary>
    public class AccountMergeService
    {
        private static readonly string[] ProtectedRoles = { "Admin", "Manager", "SuperAdmin" };

        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<WebUser> userManager;
        private readonly ILogger<AccountMergeService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountMergeService"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="logger">Logger.</param>
        public AccountMergeService(
            ApplicationDbContext dbContext,
            UserManager<WebUser> userManager,
            ILogger<AccountMergeService> logger)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.logger = logger;
        }

        /// <summary>
        /// Evaluates whether the signed-in user may merge the account that owns the external login.
        /// </summary>
        /// <param name="targetUser">The account that should be kept.</param>
        /// <param name="externalLoginInfo">Fresh external login information from the provider.</param>
        /// <returns>Merge eligibility details.</returns>
        public async Task<AccountMergeEligibility> EvaluateMergeAsync(
            WebUser targetUser,
            ExternalLoginInfo externalLoginInfo)
        {
            if (targetUser == null || externalLoginInfo == null)
            {
                return AccountMergeEligibility.Blocked(AccountMergeBlockReason.MissingInformation);
            }

            var sourceUser = await userManager.FindByLoginAsync(
                externalLoginInfo.LoginProvider,
                externalLoginInfo.ProviderKey);

            if (sourceUser == null)
            {
                return AccountMergeEligibility.Blocked(AccountMergeBlockReason.SourceAccountNotFound);
            }

            if (sourceUser.Id == targetUser.Id)
            {
                return AccountMergeEligibility.Blocked(AccountMergeBlockReason.SameAccount);
            }

            var externalEmail = externalLoginInfo.Principal?.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(externalEmail))
            {
                return AccountMergeEligibility.Blocked(AccountMergeBlockReason.MissingExternalEmail);
            }

            var normalizedExternalEmail = userManager.NormalizeEmail(externalEmail);
            var normalizedTargetEmail = userManager.NormalizeEmail(targetUser.Email);
            var normalizedSourceEmail = userManager.NormalizeEmail(sourceUser.Email);

            if (normalizedTargetEmail != normalizedExternalEmail
                || normalizedSourceEmail != normalizedExternalEmail)
            {
                return AccountMergeEligibility.Blocked(
                    AccountMergeBlockReason.EmailMismatch,
                    externalLoginInfo.ProviderDisplayName,
                    MaskEmail(externalEmail),
                    MaskEmail(sourceUser.Email));
            }

            if (await this.HasProtectedRoleAsync(targetUser) || await this.HasProtectedRoleAsync(sourceUser))
            {
                return AccountMergeEligibility.Blocked(AccountMergeBlockReason.ProtectedAccount);
            }

            return AccountMergeEligibility.Allowed(
                sourceUser,
                externalLoginInfo.ProviderDisplayName,
                MaskEmail(sourceUser.Email));
        }

        /// <summary>
        /// Links an external provider to the target account, merging a duplicate account when required.
        /// </summary>
        /// <param name="targetUser">The account that should keep the external login.</param>
        /// <param name="externalLoginInfo">Fresh external login information from the provider.</param>
        /// <returns>The link attempt result.</returns>
        public async Task<ExternalLoginLinkAttempt> TryLinkExternalLoginAsync(
            WebUser targetUser,
            ExternalLoginInfo externalLoginInfo)
        {
            if (targetUser == null || externalLoginInfo == null)
            {
                return ExternalLoginLinkAttempt.Blocked(
                    AccountMergeEligibility.Blocked(AccountMergeBlockReason.MissingInformation));
            }

            var addResult = await this.userManager.AddLoginAsync(targetUser, externalLoginInfo);
            if (addResult.Succeeded)
            {
                return ExternalLoginLinkAttempt.Linked();
            }

            var eligibility = await this.EvaluateMergeAsync(targetUser, externalLoginInfo);
            if (eligibility.BlockReason == AccountMergeBlockReason.SameAccount)
            {
                return ExternalLoginLinkAttempt.Linked();
            }

            if (eligibility.CanMerge)
            {
                var mergeResult = await this.MergeExternalLoginAccountsAsync(targetUser, externalLoginInfo);
                if (mergeResult.Succeeded)
                {
                    return ExternalLoginLinkAttempt.Merged();
                }

                return ExternalLoginLinkAttempt.Blocked(eligibility, mergeResult);
            }

            return ExternalLoginLinkAttempt.Blocked(eligibility, addResult);
        }

        /// <summary>
        /// Merges the source account into the target account and links the external login.
        /// </summary>
        /// <param name="targetUser">The account to keep.</param>
        /// <param name="externalLoginInfo">External login information from the provider.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The identity result of the merge operation.</returns>
        public async Task<IdentityResult> MergeExternalLoginAccountsAsync(
            WebUser targetUser,
            ExternalLoginInfo externalLoginInfo,
            CancellationToken cancellationToken = default)
        {
            var eligibility = await this.EvaluateMergeAsync(targetUser, externalLoginInfo);
            if (!eligibility.CanMerge)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "AccountMergeNotAllowed",
                    Description = eligibility.BlockReason.ToString(),
                });
            }

            var sourceUser = eligibility.SourceUser;
            var strategy = this.dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await this.dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await this.ReassignUserOwnedDataAsync(sourceUser.Id, targetUser.Id, cancellationToken);
                    await this.MoveExternalLoginsAsync(sourceUser.Id, targetUser.Id, cancellationToken);
                    await this.MergeProfileFieldsAsync(targetUser, sourceUser, cancellationToken);

                    var deleteResult = await userManager.DeleteAsync(sourceUser);
                    if (!deleteResult.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return deleteResult;
                    }

                    await transaction.CommitAsync(cancellationToken);

                    this.logger.LogInformation(
                        "Merged account {SourceUserId} into {TargetUserId} while linking {Provider}.",
                        sourceUser.Id,
                        targetUser.Id,
                        externalLoginInfo.LoginProvider);

                    return IdentityResult.Success;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    this.logger.LogError(
                        ex,
                        "Failed to merge account {SourceUserId} into {TargetUserId}.",
                        sourceUser.Id,
                        targetUser.Id);
                    throw;
                }
            });
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return string.Empty;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                return email;
            }

            return email[0] + new string('*', Math.Min(atIndex - 1, 5)) + email.Substring(atIndex);
        }

        private async Task<bool> HasProtectedRoleAsync(WebUser user)
        {
            foreach (var role in ProtectedRoles)
            {
                if (await userManager.IsInRoleAsync(user, role))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task ReassignUserOwnedDataAsync(
            string sourceUserId,
            string targetUserId,
            CancellationToken cancellationToken)
        {
            await this.dbContext.Donations
                .Where(d => EF.Property<string>(d, "UserId") == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(d => EF.Property<string>(d, "UserId"), targetUserId),
                    cancellationToken);

            await this.dbContext.Subscriptions
                .Where(s => EF.Property<string>(s, "UserId") == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(s => EF.Property<string>(s, "UserId"), targetUserId),
                    cancellationToken);

            await this.dbContext.Referrals
                .Where(r => EF.Property<string>(r, "UserId") == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(r => EF.Property<string>(r, "UserId"), targetUserId),
                    cancellationToken);

            await this.dbContext.Invoices
                .Where(i => EF.Property<string>(i, "UserId") == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(i => EF.Property<string>(i, "UserId"), targetUserId),
                    cancellationToken);

            await this.dbContext.PaymentNotifications
                .Where(p => EF.Property<string>(p, "UserId") == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(p => EF.Property<string>(p, "UserId"), targetUserId),
                    cancellationToken);

            await this.dbContext.UserLoginEvents
                .Where(e => e.UserId == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(e => e.UserId, targetUserId),
                    cancellationToken);
        }

        private async Task MoveExternalLoginsAsync(
            string sourceUserId,
            string targetUserId,
            CancellationToken cancellationToken)
        {
            await this.dbContext.UserLogins
                .Where(login => login.UserId == sourceUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(login => login.UserId, targetUserId),
                    cancellationToken);
        }

        private async Task MergeProfileFieldsAsync(
            WebUser targetUser,
            WebUser sourceUser,
            CancellationToken cancellationToken)
        {
            var target = await this.dbContext.Users
                .Include(u => u.Address)
                .FirstAsync(u => u.Id == targetUser.Id, cancellationToken);
            var source = await this.dbContext.Users
                .Include(u => u.Address)
                .AsNoTracking()
                .FirstAsync(u => u.Id == sourceUser.Id, cancellationToken);

            target.FullName ??= source.FullName;
            target.PhoneNumber ??= source.PhoneNumber;
            target.Nif ??= source.Nif;
            target.CompanyName ??= source.CompanyName;
            target.RegisteredAtUtc ??= source.RegisteredAtUtc;
            target.RegistrationCampaignId ??= source.RegistrationCampaignId;
            target.RegistrationLoginProvider ??= source.RegistrationLoginProvider;

            if (target.Address == null && source.Address != null)
            {
                target.Address = new DonorAddress
                {
                    Address1 = source.Address.Address1,
                    Address2 = source.Address.Address2,
                    City = source.Address.City,
                    PostalCode = source.Address.PostalCode,
                    Country = source.Address.Country,
                };
            }
            else if (target.Address != null && source.Address != null)
            {
                target.Address.Address1 ??= source.Address.Address1;
                target.Address.Address2 ??= source.Address.Address2;
                target.Address.City ??= source.Address.City;
                target.Address.PostalCode ??= source.Address.PostalCode;
                target.Address.Country ??= source.Address.Country;
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
