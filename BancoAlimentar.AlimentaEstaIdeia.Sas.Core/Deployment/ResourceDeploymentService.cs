// -----------------------------------------------------------------------
// <copyright file="ResourceDeploymentService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Resources;
    using Azure.ResourceManager.Resources.Models;

    /// <summary>
    /// Service for deploying Azure resources with an ARM template.
    /// </summary>
    public class ResourceDeploymentService
    {
        private readonly ArmClient armClient;
        private readonly Dictionary<string, ArmOperation<ArmDeploymentResource>> operations = new ();
        private readonly DeploymentTemplateProvider templateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDeploymentService"/> class.
        /// </summary>
        /// <param name="options">The id of the Azure AD tenant where the service principal is defined.</param>
        /// <param name="templateProvider">The provider used to retrieve the ARM template from storage.</param>
        public ResourceDeploymentService(ResourceDeploymentOptions options, DeploymentTemplateProvider templateProvider)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            this.armClient = new (new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret));
            this.templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
        }

        /// <summary>
        /// Initiates a deployment of an ARM template.
        /// </summary>
        /// <param name="subscriptionId">The id of the Azure subscription to deploy to.</param>
        /// <param name="location">The Azure location to deploy to.</param>
        /// <param name="tenantId">The id of the customer tenant.</param>
        /// <returns>The deployment identifier.</returns>
        public async Task<string?> CreateTenantDeploymentAsync(string subscriptionId, string location, string tenantId)
        {
            SubscriptionResource sub = (await this.armClient.GetSubscriptions().GetAsync(subscriptionId)).Value;

            AzureLocation deployLocation = await ValidateLocationAsync(sub, location);

            var deployments = sub.GetArmDeployments();

            var deploymentName = $"{tenantId}-{DateTime.Now:yyyy-MM-dd-HHmm}";

            ArmDeploymentContent deploymentContent = new (new (ArmDeploymentMode.Incremental)
            {
                Template = await this.templateProvider.GetArmTemplateAsync(),
                Parameters = GetDeploymentParameters(deployLocation.Name, tenantId),
            })
            { Location = deployLocation };

            this.operations.Add(deploymentName, await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Started, deploymentName, deploymentContent));

            var depData = deployments.FirstOrDefault(d => d.HasData && d.Data.Name.Equals(deploymentName, StringComparison.Ordinal))?.Data;

            return depData?.Id?.ToString();
        }

        /// <summary>
        /// Gets the deployment parameter values as JSON formatted data.
        /// </summary>
        /// <param name="location">The value of the location parameter.</param>
        /// <param name="tenantId">The value of the tenantId parameter.</param>
        /// <returns>The JSON data as a <see cref="BinaryData"/> object.</returns>
        private static BinaryData GetDeploymentParameters(string location, string tenantId)
        {
            Dictionary<string, ParameterValue> parameters = new ()
            {
                { "tenantId", new ParameterValue(tenantId) },
                { "location", new ParameterValue(location) },
            };

            return BinaryData.FromObjectAsJson<Dictionary<string, ParameterValue>>(parameters);
        }

        /// <summary>
        /// Validate the location name matches an available location for the subscription.
        /// </summary>
        /// <param name="subscription">The subscription resource.</param>
        /// <param name="location">The location name.</param>
        /// <returns>The <see cref="AzureLocation"/> with the details of the location.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the location does not match.</exception>
        private static async Task<AzureLocation> ValidateLocationAsync(SubscriptionResource subscription, string location)
        {
            await foreach (var loc in subscription.GetLocationsAsync())
            {
                if (location.Equals(loc.Name, StringComparison.OrdinalIgnoreCase) ||
                    location.Equals(loc.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    return loc;
                }
            }

            throw new ArgumentOutOfRangeException(location, $"The location \"{location}\" is not an available Azure location.");
        }
    }
}
