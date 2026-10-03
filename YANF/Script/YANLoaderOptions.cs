namespace YANF.Script
{
    /// <summary>
    /// Settings of a <see cref="YANLoader"/> overlay. The values are read when the overlay is created.
    /// </summary>
    public sealed class YANLoaderOptions
    {
        #region Fields
        internal const int DEFAULT_SHOW_DELAY = 250;
        internal const int DEFAULT_FADE_DURATION = 200;
        #endregion

        #region Properties
        /// <summary>
        /// The screen to show. Default: <see cref="YANLoaderKind.Load"/>.
        /// </summary>
        public YANLoaderKind Kind { get; set; } = YANLoaderKind.Load;

        /// <summary>
        /// Milliseconds to wait before the overlay appears, so that short work shows nothing. Default: 250.
        /// 0 or less shows it at once. The owner form stops taking input immediately either way.
        /// </summary>
        public int ShowDelay { get; set; } = DEFAULT_SHOW_DELAY;

        /// <summary>
        /// Corner size of the overlay's rounded region in pixels (match a rounded owner form). Default: 0 (square corners).
        /// </summary>
        public int Corner { get; set; }

        /// <summary>
        /// Duration of the overlay's fade-in and fade-out in milliseconds. Default: 200. 0 or less shows and closes it without a fade.
        /// </summary>
        public int FadeDuration { get; set; } = DEFAULT_FADE_DURATION;
        #endregion
    }
}
