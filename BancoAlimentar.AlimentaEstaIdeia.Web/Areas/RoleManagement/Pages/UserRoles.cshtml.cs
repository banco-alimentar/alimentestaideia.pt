// -----------------------------------------------------------------------
// <copyright file="UserRoles.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Model;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// User roles model.
    /// </summary>
    public class UserRolesModel : PageModel
    {
        private const int PageSize = 25;
        private const int MaxEmailSearchLength = 256;

        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRolesModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="dbContext">Application database context.</param>
        public UserRolesModel(
            UserManager<WebUser> userManager,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="UserRolesViewModel"/>.
        /// </summary>
        public List<UserRolesViewModel> UserRolesViewModel { get; set; } = new List<UserRolesViewModel>();

        /// <summary>
        /// Gets or sets the email search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string EmailSearch { get; set; }

        /// <summary>
        /// Gets or sets the role identifier filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string RoleId { get; set; }

        /// <summary>
        /// Gets the name of the role used to filter the user list.
        /// </summary>
        public string FilteredRoleName { get; private set; }

        /// <summary>
        /// Gets or sets the current page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of users per page.
        /// </summary>
        public int UsersPerPage => PageSize;

        /// <summary>
        /// Gets the total number of users matching the filter.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            string emailFilter = NormalizeEmailSearch(EmailSearch);
            EmailSearch = emailFilter;

            if (PageIndex < 1)
            {
                PageIndex = 1;
            }

            IQueryable<WebUser> query = userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(RoleId))
            {
                string roleId = RoleId.Trim();
                FilteredRoleName = await dbContext.Roles
                    .AsNoTracking()
                    .Where(role => role.Id == roleId)
                    .Select(role => role.Name)
                    .FirstOrDefaultAsync();

                if (FilteredRoleName == null)
                {
                    RoleId = null;
                }
                else
                {
                    RoleId = roleId;
                    query = query.Where(user => dbContext.UserRoles.Any(
                        userRole => userRole.UserId == user.Id && userRole.RoleId == roleId));
                }
            }

            if (!string.IsNullOrEmpty(emailFilter))
            {
                query = query.Where(user => user.Email.Contains(emailFilter));
            }

            TotalCount = await query.CountAsync();
            TotalPages = TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (TotalPages > 0 && PageIndex > TotalPages)
            {
                PageIndex = TotalPages;
            }

            List<WebUser> users = await query
                .OrderBy(user => user.Email)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Dictionary<string, List<string>> rolesByUserId = await LoadRolesByUserIdAsync(users.Select(user => user.Id).ToList());

            UserRolesViewModel = users.Select(user => new UserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FullName,
                Roles = rolesByUserId.TryGetValue(user.Id, out List<string> roles)
                    ? roles
                    : new List<string>(),
            }).ToList();

            return Page();
        }

        private static string NormalizeEmailSearch(string emailSearch)
        {
            if (string.IsNullOrWhiteSpace(emailSearch))
            {
                return null;
            }

            string trimmed = emailSearch.Trim();
            if (trimmed.Length > MaxEmailSearchLength)
            {
                trimmed = trimmed.Substring(0, MaxEmailSearchLength);
            }

            return trimmed;
        }

        private async Task<Dictionary<string, List<string>>> LoadRolesByUserIdAsync(IList<string> userIds)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<string, List<string>>();
            }

            var roleAssignments = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                orderby role.Name
                select new { userRole.UserId, role.Name })
                .ToListAsync();

            return roleAssignments
                .GroupBy(assignment => assignment.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(assignment => assignment.Name).ToList());
        }
    }
}
