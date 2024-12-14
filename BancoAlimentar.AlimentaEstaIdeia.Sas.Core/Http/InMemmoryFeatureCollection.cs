// -----------------------------------------------------------------------
// <copyright file="InMemmoryFeatureCollection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Http
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.AspNetCore.Http.Features;

    /// <summary>
    /// Default implementation for <see cref="IFeatureCollection"/>.
    /// </summary>
    [DebuggerDisplay("Count = {GetCount()}")]
    public class InMemmoryFeatureCollection : IFeatureCollection
    {
        private static readonly KeyComparer FeatureKeyComparer = new KeyComparer();
        private IDictionary<Type, object>? features;
        private volatile int containerRevision;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemmoryFeatureCollection"/> class.
        /// </summary>
        public InMemmoryFeatureCollection()
        {
        }

        /// <inheritdoc />
        public virtual int Revision
        {
            get { return this.containerRevision; }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <inheritdoc />
        public object? this[Type key]
        {
            get
            {
                return this.features != null && this.features.TryGetValue(key, out var result) ? result : null;
            }

            set
            {
                if (value == null)
                {
                    if (this.features != null && this.features.Remove(key))
                    {
                        this.containerRevision++;
                    }

                    return;
                }

                if (this.features == null)
                {
                    this.features = new Dictionary<Type, object>();
                }

                this.features[key] = value;
                this.containerRevision++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            if (this.features != null)
            {
                foreach (var pair in this.features)
                {
                    yield return pair;
                }
            }
        }

        /// <inheritdoc />
        public TFeature? Get<TFeature>()
        {
            if (typeof(TFeature).IsValueType)
            {
                var feature = this[typeof(TFeature)];
                if (feature is null && Nullable.GetUnderlyingType(typeof(TFeature)) is null)
                {
                    throw new InvalidOperationException(
                        $"{typeof(TFeature).FullName} does not exist in the feature collection " +
                        $"and because it is a struct the method can't return null. Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' to check if the feature exists.");
                }

                return (TFeature?)feature;
            }

            return (TFeature?)this[typeof(TFeature)];
        }

        /// <inheritdoc />
        public void Set<TFeature>(TFeature? instance)
        {
            this[typeof(TFeature)] = instance;
        }

        // Used by the debugger. Count over enumerable is required to get the correct value.
        private int GetCount() => this.Count();

        private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
        {
            public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<Type, object> obj)
            {
                return obj.Key.GetHashCode();
            }
        }
    }
}
