// -----------------------------------------------------------------------
// <copyright file="DistributedCacheExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// Distributed cache extensions.
/// </summary>
public static class DistributedCacheExtensions
{
    /// <summary>
    /// Adds an entry to the distributed cache.
    /// </summary>
    /// <typeparam name="TValue">Type of the entity to add.</typeparam>
    /// <param name="distributedCache">The distributed cache.</param>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Cache value.</param>
    public static void AddEntry<TValue>(this IDistributedCache distributedCache, string key, TValue value)
    {
        string json = JsonSerializer.Serialize<TValue>(value);
        distributedCache.SetString(key, json);
    }

    /// <summary>
    /// Gets the entry from the distributed cache.
    /// </summary>
    /// <typeparam name="TValue">Type of the entity to retrieve.</typeparam>
    /// <param name="distributedCache">The distributed cache.</param>
    /// <param name="key">Cache key.</param>
    /// <returns>An instance of the element in the cache.</returns>
    public static TValue GetEntry<TValue>(this IDistributedCache distributedCache, string key)
    {
        string json = distributedCache.GetString(key);
        if (json == null)
        {
            return default(TValue);
        }

        return JsonSerializer.Deserialize<TValue>(json);
    }
}
