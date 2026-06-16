// -----------------------------------------------------------------------
// <copyright file="UserLoginTrackingService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Records user registrations and successful sign-ins for reporting.
    /// </summary>
    public class UserLoginTrackingService
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger<UserLoginTrackingService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoginTrackingService"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="unitOfWork">Unit of work.</param>
        /// <param name="logger">Logger.</param>
        public UserLoginTrackingService(
            ApplicationDbContext dbContext,
            IUnitOfWork unitOfWork,
            ILogger<UserLoginTrackingService> logger)
        {
            this.dbContext = dbContext;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        /// <summary>
        /// Sets registration metadata on a newly created user when not already present.
        /// </summary>
        /// <param name="user">The registered user.</param>
        /// <param name="loginProvider">The provider used to register.</param>
        public void SetRegistrationMetadata(WebUser user, string loginProvider)
        {
            if (user == null)
            {
                return;
            }

            if (user.RegisteredAtUtc.HasValue)
            {
                return;
            }

            var campaign = this.unitOfWork.CampaignRepository.GetCurrentCampaign();
            user.RegisteredAtUtc = DateTime.UtcNow;
            user.RegistrationLoginProvider = loginProvider;
            user.RegistrationCampaignId = campaign?.Id;
        }

        /// <summary>
        /// Records a successful login event.
        /// </summary>
        /// <param name="user">The signed-in user.</param>
        /// <param name="loginProvider">The provider used to sign in.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RecordLoginAsync(
            WebUser user,
            string loginProvider,
            CancellationToken cancellationToken = default)
        {
            if (user == null || string.IsNullOrWhiteSpace(loginProvider))
            {
                return;
            }

            try
            {
                var campaign = this.unitOfWork.CampaignRepository.GetCurrentCampaign();
                this.dbContext.UserLoginEvents.Add(new UserLoginEvent
                {
                    UserId = user.Id,
                    LoginProvider = loginProvider,
                    CampaignId = campaign?.Id,
                    OccurredAtUtc = DateTime.UtcNow,
                });

                await this.dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to record login for user {UserId}.", user.Id);
            }
        }
    }
}
