namespace YANF.Screen
{
    public class MiddleScreen : AnonScreen
    {
        /// <summary>
        /// Closes the form (1.0.x threw NotImplementedException here). The overlay screens override it to fade out, close and dispose.
        /// </summary>
        public override void Frm_Close() => Close();
    }
}