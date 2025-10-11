using System.Drawing;

namespace Captura
{
    public class RegionSelectorSettings : PropertyStore
    {
        public Color BrushColor
        {
            get => Get(Color.FromArgb(27, 27, 27));
            set => Set(value);
        }
    }
}
