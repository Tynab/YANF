using YANF.Script.Model;

namespace YANF.Script;

public interface IYANUpdateScrService : IYANSrcService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="updateScr"></param>
    public void PublishValue(UpdateScr updateScr);
}
