// -----------------------------------------------------------------------
// <copyright file="FoodBankDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Initialize the food bank table when migration.
/// </summary>
public static class FoodBankDbInitializer
{
    /// <summary>
    /// Initialize the database.
    /// </summary>
    /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
    /// <param name="configuration">Tenant configuration.</param>
    public static void Initialize(ApplicationDbContext context, IConfiguration configuration)
    {
        context.Database.EnsureCreated();

        if (context.FoodBanks.Any())
        {
            return;
        }

        string[] foodBanks = new string[0];

        string blobName = configuration["AzureStorage:FoodBankSourceBlobName"];
        string containerName = configuration["AzureStorage:FoodBankSourceContainerName"];
        string azureStorageConnectionString = configuration["AzureStorage:ConnectionString"];

        if (!string.IsNullOrEmpty(blobName) &&
            !string.IsNullOrEmpty(containerName) &&
            !string.IsNullOrEmpty(azureStorageConnectionString))
        {
            BlobContainerClient container = new BlobContainerClient(azureStorageConnectionString, containerName);
            BlobBaseClient blob = container.GetBlobBaseClient(blobName);
            using (Stream stream = blob.OpenRead(new BlobOpenReadOptions(false)))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    foodBanks = content.Split(Environment.NewLine);
                    foodBanks = foodBanks.Select(p => p.TrimEnd('\r')).ToArray();
                }
            }
        }

        if (foodBanks.Length == 0)
        {
            // Fallback to the alimenta esta ideia food banks (for testing).
            foodBanks = GetFoodBankFromResources();
        }

        foreach (var item in foodBanks)
        {
            FoodBank foodBank = new FoodBank()
            {
                Name = item,
            };

            context.FoodBanks.Add(foodBank);
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Gets the food banks for the embedded resourde FoodBankList.txt.
    /// </summary>
    /// <returns>An array of the food bank to be inserted in the database.</returns>
    private static string[] GetFoodBankFromResources()
    {
        string[] result = null;

        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BancoAlimentar.AlimentaEstaIdeia.Model.Initializer.FoodBankList.txt"))
        {
            using (StreamReader streamReader = new StreamReader(stream, true))
            {
                string value = streamReader.ReadToEnd();
                result = value.Split(Environment.NewLine);
                result = result.Select(p => p.TrimEnd('\r')).ToArray();
            }
        }

        return result;
    }
}
