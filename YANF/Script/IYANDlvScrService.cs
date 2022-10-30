namespace YANF.Script
{
    public interface IYANDlvScrService : IYANSrcService
    {
        /// <summary>
        /// Truyền hiển thị cho màn hình chờ.
        /// </summary>
        /// <param name="percent">Percent.</param>
        /// <param name="capacity">Capacity.</param>
        /// <param name="width">Progress bar width.</param>
        public void PublishValue(int percent, string capacity, int width);
    }
}