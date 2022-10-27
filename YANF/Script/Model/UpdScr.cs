namespace YANF.Script.Model
{
    public class UpdScr
    {
        #region Properties
        public string Capacity { get; set; }
        public string Percent { get; set; }
        public int Width { get; set; }
        #endregion

        #region Constructors
        public UpdScr(string capacity, string percent, int width)
        {
            Capacity = capacity;
            Percent = percent;
            Width = width;
        }
        #endregion
    }
}