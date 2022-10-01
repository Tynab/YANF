namespace YANF.Model;

public class UpdateScr
{
    #region Properties
    public string Capacity { get; set; }
    public string Percent { get; set; }
    public int Width { get; set; }
    #endregion

    #region Constructors
    public UpdateScr() { }

    public UpdateScr(string capacity, string percent, int width)
    {
        Capacity = capacity;
        Percent = percent;
        Width = width;
    }
    #endregion
}
