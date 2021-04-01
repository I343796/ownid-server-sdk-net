using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnID.Web.Attributes;
using OwnID.Web.Extensibility;
using OwnID.Web.Features;

namespace OwnID.Web.Configuration
{
    public class OwnIdConfiguration
    {
        private readonly IReadOnlyDictionary<Type, IFeature> _features;

        public OwnIdConfiguration([NotNull] IReadOnlyDictionary<Type, IFeature> features)
        {
            _features = features;
        }

        /// <summary>
        ///     Added features list
        /// </summary>
        public IEnumerable<IFeature> Features => _features.Values;

        /// <summary>
        ///     Allows to find feature by type
        /// </summary>
        /// <returns>
        ///     Return the feature if it exists in <see cref="Features" />. If there is no feature with
        ///     <typeparamref name="TFeature" /> type returns <c>null</c>
        /// </returns>
        public TFeature FindFeature<TFeature>() where TFeature : class, IFeature
        {
            return _features.TryGetValue(typeof(TFeature), out var feature) ? (TFeature) feature : null;
        }

        /// <summary>
        ///     Check if feature has been added
        /// </summary>
        /// <typeparam name="TFeature">type of feature to check</typeparam>
        /// <returns>true if feature with <typeparamref name="TFeature" /> has been added, otherwise false</returns>
        public bool HasFeature<TFeature>()
            where TFeature : class, IFeature
        {
            return _features.ContainsKey(typeof(TFeature));
        }

        /// <summary>
        ///     Tries to set (adds or updates) feature with <typeparamref name="TFeature" />
        /// </summary>
        /// <param name="feature"><typeparamref name="TFeature" /> instance</param>
        /// <typeparam name="TFeature"><see cref="IFeature" /> implementation</typeparam>
        /// <returns>New instance of <see cref="OwnIdConfiguration" /> with <typeparamref name="TFeature" /> set</returns>
        public OwnIdConfiguration WithFeature<TFeature>([NotNull] TFeature feature)
            where TFeature : class, IFeature
        {
            var extensions = Features.ToDictionary(p => p.GetType(), p => p);
            extensions[typeof(TFeature)] = feature;

            return new OwnIdConfiguration(extensions);
        }

        /// <summary>
        ///     Validates if all required features were added and all features are correctly configured
        /// </summary>
        /// <remarks>
        ///     Calls all features <c>Validate()</c>
        /// </remarks>
        /// <exception cref="InvalidOperationException">On first invalid setting occurence</exception>
        public void Validate()
        {
            if (!_features.ContainsKey(typeof(CoreFeature)))
                throw new InvalidOperationException($"{nameof(CoreFeature)} should be added for any SDK usage");

            if (!_features.ContainsKey(typeof(UserHandlerFeature)))
                throw new InvalidOperationException(
                    $"{nameof(UserHandlerFeature)} should be added to authorize users and updating their profiles");

            if (!_features.ContainsKey(typeof(CacheStoreFeature)))
                throw new InvalidOperationException(
                    $"{nameof(CacheStoreFeature)} should be added to store challenge context");

            if (!_features.ContainsKey(typeof(LocalizationFeature)))
                throw new InvalidOperationException(
                    $"{nameof(LocalizationFeature)} should be added");

            foreach (var feature in _features.Values)
            {
                var attrs = feature.GetType().GetCustomAttribute(typeof(FeatureDependencyAttribute));

                if (attrs != null && attrs is FeatureDependencyAttribute dependency)
                    foreach (var dependencyType in dependency.Types)
                        if (!_features.ContainsKey(dependencyType))
                            throw new InvalidOperationException(
                                $"{dependencyType.Name} feature is required for {feature.GetType().Name} feature");

                feature.FillEmptyWithOptional();
                feature.Validate();
            }
        }

        /// <summary>
        ///     Lets all features to setup their services with <paramref name="serviceCollection" />
        /// </summary>
        /// <returns>Current instance of <see cref="OwnIdConfiguration" /></returns>
        public OwnIdConfiguration IntegrateFeatures(IServiceCollection serviceCollection)
        {
            foreach (var feature in _features.Values) feature.ApplyServices(serviceCollection);
            serviceCollection.TryAddSingleton(this);

            return this;
        }
    }
}