using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace OwnID.Web.Extensibility
{
    /// <summary>
    ///     Describes extensibility unit for OwnId Sdk
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        ///     Will be called after successful <see cref="Validate" />
        /// </summary>
        /// <param name="services"></param>
        void ApplyServices([NotNull] IServiceCollection services);

        /// <summary>
        ///     Well be called before <see cref="Validate" /> to set default values
        /// </summary>
        /// <returns></returns>
        IFeature FillEmptyWithOptional();

        /// <summary>
        ///     Validates all needed feature parameters and settings
        /// </summary>
        /// <remarks>Should throw <see cref="System.InvalidOperationException" /> if there is any validation errors</remarks>
        void Validate();
    }
}