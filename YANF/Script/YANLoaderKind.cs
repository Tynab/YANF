namespace YANF.Script
{
    /// <summary>
    /// The screen that <see cref="YANLoader"/> shows over its owner form.
    /// </summary>
    public enum YANLoaderKind
    {
        /// <summary>
        /// Loading animation with a percentage, covering the owner form.
        /// </summary>
        Load,

        /// <summary>
        /// Waiting animation without progress, covering the owner form.
        /// </summary>
        Wait,

        /// <summary>
        /// Small update box centred on the owner form, with a percentage, a detail text and a progress bar.
        /// </summary>
        Update
    }
}
