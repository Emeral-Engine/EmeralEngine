using EmeralEngine.Resource.Transition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace EmeralEngine.Resource.CustomTransition
{
    public struct TransitionTypes
    {
        public const int NONE = 0;
        public const int SIMPLE = 1;
    }
}
