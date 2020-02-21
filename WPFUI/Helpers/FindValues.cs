using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUI.Helpers
{
    public static class FindValues
    {
        /// <summary>
        /// Linear interpolation, given the value <paramref name="x"/> and the value pairs <paramref name="x0"/> <paramref name="y0"/> and <paramref name="x1"/> <paramref name="y1"/>
        /// returns the interpolated value y.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double LinearInterpolation(double x0, double y0, double x1, double y1, double x)
        {
            double y = y0 + (x - x0) * (y1 - y0) / (x1 - x0);
            return y;
        }



        /// <summary>
        /// Search for value <paramref name="findVal"/> in list, returns the index of the closest higher and lower value for interpolation. 
        /// </summary>
        /// <param name="findVal"></param>
        /// <param name="listToSearchIn"></param>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        public static void FindIndexOfTwoClosestValues(double findVal, List<double> listToSearchIn, out int i1, out int i2)
        {

            if (listToSearchIn[1] < listToSearchIn[0])                                  // IF list is descending
            {
                i1 = listToSearchIn.FindIndex(m => findVal >= m);                      // this ONLY works if the coordinates for depth dose is descending
                i2 = listToSearchIn.FindLastIndex(m => findVal <= m);                  // normally this is the case but not guaranteed
            }
            else
            {
                i1 = listToSearchIn.FindLastIndex(m => findVal >= m);
                i2 = listToSearchIn.FindIndex(m => findVal <= m);
            }
            // int i1BinaryS = depthDose.CoordZ.BinarySearch(OFList[indexOF].PointDepth);                   // with binary search, only works with sorted (ascending) lists
            // to be on the safe side, check the order
        }
    }



}
