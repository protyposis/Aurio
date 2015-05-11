using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio {
    public static class CommonUtil {
        /// <summary>
        /// Swap two references of generic type.
        /// Source: http://msdn.microsoft.com/en-us/library/twcad0zb%28v=VS.100%29.aspx
        /// </summary>
        public static void Swap<T>(ref T lhs, ref T rhs) {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
