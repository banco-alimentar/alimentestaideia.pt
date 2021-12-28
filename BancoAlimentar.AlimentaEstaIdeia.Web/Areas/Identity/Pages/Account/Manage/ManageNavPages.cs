// -----------------------------------------------------------------------
// <copyright file="ManageNavPages.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using Microsoft.AspNetCore.Mvc.Rendering;

    /// <summary>
    /// Managed navigation pages.
    /// </summary>
    public static class ManageNavPages
    {
        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string Index => "Index";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string Email => "Email";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string ChangePassword => "ChangePassword";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DownloadPersonalData => "DownloadPersonalData";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DeletePersonalData => "DeletePersonalData";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string ExternalLogins => "ExternalLogins";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string PersonalData => "PersonalData";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DonationHistory => "DonationHistory";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string CampaignsHistory => "CampaignsHistory";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string SubscriptionManagement => "SubscriptionIndex";

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DonationHistoryNavClass(ViewContext viewContext) => PageNavClass(viewContext, DonationHistory);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string CampaignsHistoryNavClass(ViewContext viewContext) => PageNavClass(viewContext, CampaignsHistory);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        public static string SubscriptionManagementNavClass(ViewContext viewContext) => PageNavClass(viewContext, SubscriptionManagement);

        /// <summary>
        /// Gets the navigation nav class..
        /// </summary>
        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
