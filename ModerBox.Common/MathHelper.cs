using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common {
    public class MathHelper {
        public static double GetMax(List<double> analog) {
            return analog.Max() > -analog.Min() ? analog.Max() : analog.Min();
        }

        public static double GetMax(double[] analog) {
            return analog.Max() > -analog.Min() ? analog.Max() : analog.Min();
        }
    }
}
