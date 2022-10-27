namespace YANF.Script
{
    public interface IYANSrcService
    {
        /// <summary>
        /// Bật form update.
        /// </summary>
        public void OnLoader();

        /// <summary>
        /// Tắt form update.
        /// </summary>
        public void OffLoader();
    }
}