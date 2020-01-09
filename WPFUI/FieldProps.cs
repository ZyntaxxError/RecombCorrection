using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUI
{
    abstract class FieldProps
    {

        private string treatmentRoom;
        private string energy;      // limit to one energy at a time
        private int fieldSizeX;  // check that x=y, only square fields allowed? nah..
        private int fieldSizeY;
        private int ssd;
        private List<string> headerText;
        private List<double> coordX;
        private List<double> coordY;
        private List<double> coordZ;
        private List<double> relDose;
        private List<double> absDose;
        private List<double> _recombCorr;
        private List<double> _corrRelDose;
        private List<double> _corrAbsDose;

        public List<double> CorrAbsDose { get => _corrAbsDose; set => _corrAbsDose = value; }

        public List<double> CorrRelDose { get => _corrRelDose; set => _corrRelDose = value; }

        public List<double> RecombCorr { get => _recombCorr; set => _recombCorr = value; }


        public string TreatmentRoom
        {
            get { return treatmentRoom; }
            set { treatmentRoom = value; }
        }
        public string Energy
        {
            get { return energy; }
            set { energy = value; }
        }

        public int FieldSizeX { get => fieldSizeX; set => fieldSizeX = value; }
        public int FieldSizeY { get => fieldSizeY; set => fieldSizeY = value; }
        public int SSD { get => ssd; set => ssd = value; }
        public List<string> HeaderText { get => headerText; set => headerText = value; }
        public List<double> CoordX { get => coordX; set => coordX = value; }
        public List<double> CoordY { get => coordY; set => coordY = value; }
        public List<double> CoordZ { get => coordZ; set => coordZ = value; }
        public List<double> RelDose { get => relDose; set => relDose = value; }
        public List<double> AbsDose { get => absDose; set => absDose = value; }







        ////constructor   ... can not be overridden as it is not declared virtual and in a abstract class
        //public FieldProps()
        //{
        //    //Console.WriteLine("check that you only have one energy");
        //    CheckForSingleEnergy();
        //}



        ////method; can be overridden in subclasses or in inplementation due to virtual
        //public virtual void CheckForSingleEnergy()
        //{
        //    //Console.WriteLine("this method never get used... yes it does if I call it in the constructor!");
        //}






    }
}

