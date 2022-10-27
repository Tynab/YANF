using YANF.Script.Model;

namespace YANF.Script
{
    public interface IYANUpdScrService : IYANSrcService
    {
        /// <summary>
        /// Truyền hiển thị cho form update.
        /// </summary>
        /// <param name="updScr"></param>
        public void PublishValue(UpdScr updScr);
    }
}