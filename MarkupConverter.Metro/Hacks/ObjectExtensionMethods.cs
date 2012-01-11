using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkupConverter.Metro.Hacks
{
    public static class ObjectExtensionMethods
    {
        public static string ToLower(this object o)
        {
            return ((string)o).ToLower();
        }
    }
}
