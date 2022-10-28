namespace YANF.Script
{
    public interface IYANUpdScrService : IYANSrcService
    {
        /// <summary>
        /// Truyền hiển thị cho form update.
        /// </summary>
        /// <param name="capacity">Capacity.</param>
        /// <param name="percent">Percent.</param>
        /// <param name="width">Progress bar width.</param>
        public void PublishValue(string capacity, string percent, int width);
    }
}