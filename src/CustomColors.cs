using EmeralEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EmeralEngine
{
    public struct CustomColors
    {
        public static SolidColorBrush CharacterBackground = (SolidColorBrush)new BrushConverter().ConvertFromString("#e2fcf8");
        public static SolidColorBrush WarnBorder = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF0000");
        public static SolidColorBrush Transparent = new SolidColorBrush(System.Windows.Media.Colors.Transparent);
        public static SolidColorBrush Edge = (SolidColorBrush)new BrushConverter().ConvertFromString("#A0A9A5");
        public static SolidColorBrush FocusingScene = (SolidColorBrush)new BrushConverter().ConvertFromString("#A0A9A5");
        public static SolidColorBrush FocusingScript = (SolidColorBrush)new BrushConverter().ConvertFromString("#A0A9A5");
        public static SolidColorBrush DraggingScript = (SolidColorBrush)new BrushConverter().ConvertFromString("#1010bc");
        public static SolidColorBrush FocusingSetting = (SolidColorBrush)new BrushConverter().ConvertFromString("#f2f2f2");
        public static SolidColorBrush SelectingElement = Utils.GetBrush("#FF2FD3D7");
    }
}
