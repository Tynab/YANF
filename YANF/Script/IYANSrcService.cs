namespace YANF.Script;

public interface IYANSrcService
{
    /// <summary>
    /// Bật form updater.
    /// </summary>
    public void OnLoader();

    /// <summary>
    /// Tắt form updater.
    /// </summary>
    public void OffLoader();
}
