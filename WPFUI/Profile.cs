using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUI
{
    class Profile : FieldProps
    {
        private double _profileDepth;

        public double ProfileDepth { get => _profileDepth; set => _profileDepth = value; }
    }
}
