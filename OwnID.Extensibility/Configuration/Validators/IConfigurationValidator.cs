namespace OwnID.Extensibility.Configuration.Validators
{
    public interface IConfigurationValidator<in TConfiguration>
    {
        /// <summary>
        ///     Fill empty configuration options with default valued
        /// </summary>
        /// <param name="configuration">configuration to update</param>
        void FillEmptyWithOptional(TConfiguration configuration);
        /// <summary>
        ///     Validate <see cref="TConfiguration"/>
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        void Validate(TConfiguration configuration);
    }
}