using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUI
{
    class OF : FieldProps
    {
        private double pointRelDose;
        private double pointDepth;

        public double PointRelDose
        {
            get { return pointRelDose; }
            set { pointRelDose = value; }
        }
        public double PointDepth
        {
            get { return pointDepth; }
            set { pointDepth = value; }
        }
    }
}
