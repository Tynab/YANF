using System.Windows.Forms;

namespace YANF.Script
{
    public interface IYANSrcService
    {
        /// <summary>
        /// Bật form loader.
        /// </summary>
        /// <param name="pFrm">Parent form.</param>
        public void OnLoader(Form pFrm);

        /// <summary>
        /// Tắt form update.
        /// </summary>
        public void OffLoader();
    }
}