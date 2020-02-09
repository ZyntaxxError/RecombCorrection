using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
namespace WPFUI
{
    /// <summary>
    /// Takes depth doses and profiles in MyQA Accept .asc format (RFA300 ASCII Measurement Dump ( BDS format ))
    /// and normalises signal to reference situation by relative output factors.
    /// Recombination correction is made by applying a linear fit of normalised signal versus recombination correction 
    /// Recombination corrected data can be saved in the same format to read back into MyQa accept, also signal normalized data (NormSignal) can be saved in.asc
    /// to quickly be able to check the normalisation in MyQA Accept. 
    /// PDD:s and profiles can also be saved in easy to inspect csv format (for plotting in excel etc.) including  
    /// RelDose         raw data from supplied asc files
    /// NormSignal      renormalized signal data, where 100 equals signal @ reference situation
    /// RecombCorr      the recombination correction for each point
    /// corrSignal      corrected signal
    /// corrRelDose     renormalised corrected relative dose
    /// 
    /// Note: no correction is made for the recombination differences i output factors, only amounts to appr. 0,16% between field size 7x7 and 40x40
    /// Prerequisite :  _all_ data use the same SSD, output factors normalised to ref situation, 
    /// recombination fit with x-axis normalised to 100 @ reference condition, y-axis the recombination factor (kS)
    /// 
    /// Discussion about the introduced error if no correction is applied. depends on chosen reference in eclipse?
    /// Uncertainties about raw data including the difference in temp and pressure, daily calibration of mashine etc
    /// </summary>
    /// TODO: in csv file print the used linear equation for correction
    /// TODO: can not open profiles and diagonales simultaneously....
    /// TODO: If the file already exists, the new text appends... wtf
    /// TODO: assumes the pdd and profiles are normalised to dmax/CAX? Calculation works regardless as normalisation is done at the depth specified in the output factor file. Renormalisation should perhaps be done when writing to csv
    /// TODO Systemtest , what happens if reopen a pdd file... have to clear all data for both pdd:s and profiles...
    /// TODO Open pdd should be inactive utntil OF file opened, same goes for profiles
    /// TODO extract method PDDoperations from read PDD
    public partial class MainWindow : Window
    {
        List<OF> OFList = new List<OF>();           // correct place to instantiate list of OF?    NEJ inte för profiler***********!!!!!!!!!! eller...
        List<PDD> PDDList = new List<PDD>();
        List<Profile> profileList = new List<Profile>();
        List<Profile> diagList = new List<Profile>();
        string[] fileHeaderPDD = new string[2];
        string[] fileHeaderProf = new string[2];

        static double RecombinationK = 0.0001771;
        static double RecombinationM = 1.0006;              //Should really be 1, i.e. no recombination correction when the signal is 0.
        

                     
        public MainWindow()
        {
            InitializeComponent();
           //DebugDefault10FFFData();                        //                    ***************FOR DEBUGGING   Loads data for 10FFF*******************
        }
        
        #region ButtonClick Methods Open files

        private void ButtonOpenOFFileClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Text files(*.txt) |*.txt|Ascii files (*.asc) |*.asc"
            };
            Nullable<bool> result = dlg.ShowDialog();   // Display OpenFileDialog by calling ShowDialog method
            if (result == true)                         // Get the selected file name and display in a TextBox and read data to objectlist OF
            {
                string filenameOF = dlg.FileName;
                TextBoxOFFile.Text = filenameOF;
                ReadOFFile(filenameOF);                
                OFList.Sort((x, y) => x.FieldSizeX.CompareTo(y.FieldSizeX));
            }
        }

        private void ButtonOpenPDDFileClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".asc",
                Filter = "Ascii files (*.asc) |*.asc| Text files(*.txt) |*.txt"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filenamePDD = dlg.FileName;
                TextBoxPDDFile.Text = filenamePDD;
                ReadPDDFile(filenamePDD);
            }
        }


        private void ButtonOpenProfFileClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".asc",
                Filter = "Ascii files (*.asc) |*.asc| Text files(*.txt) |*.txt"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filenameProf = dlg.FileName;
                TextBoxProfFile.Text = filenameProf;
   //             List<Profile> profileList = new List<Profile>();
                ReadProfFile(filenameProf);
            }
        }

        private void ButtonOpenDiagFileClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".asc",
                Filter = "Ascii files (*.asc) |*.asc| Text files(*.txt) |*.txt"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filenameDiag = dlg.FileName;
                TextBoxDiagFile.Text = filenameDiag;
  //              List<Profile> diagList = new List<Profile>();
                ReadProfFile(filenameDiag);
            }
        }



        # endregion


        #region Button Click Save files



        private void ButtonSavePDDadFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("ad", "PDD");
        }

        private void ButtonSavePDDcrdFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("crd", "PDD");
        }

        private void ButtonSavePDDcsvFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("csv", "PDD");
        }

        private void ButtonSaveSignalProfFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("ad", "Profile");
        }
      
        private void ButtonSaveCorrProfFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("crd", "Profile");
        }

        private void ButtonSaveCSVProfFileClick(object sender, RoutedEventArgs e)
        {
            SaveFile("csv", "Profile");
        }

        # endregion
        private void SaveFile(string dataFormat, string measurementType)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".asc",
                Filter = "Ascii files (*.asc) |*.asc| Text files(*.txt) |*.txt"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string fileName = dlg.FileName;
                //.Text = fileName;    how to send reference to correct text box? 
                if (measurementType == "Profile")
                    {
                    WriteProfFile(fileName, dataFormat);
                        switch (measurementType)
                        {
                        case "csv":
                            ProfcsvFile.Text = fileName;
                            break;
                        case "crd":
                            ProfcorrFile.Text = fileName;
                            break;
		                default:
                            ProfSignalFile.Text = fileName;
                            break;
	                    }
                    //  switch case csv etc to display file name in correct text box
                } else if(measurementType == "PDD")
                {
                    WritePDDFile(fileName, dataFormat);
                    switch (measurementType)
                    {
                        case "csv":
                            PDDcsvFile.Text = fileName;
                            break;
                        case "crd":
                            PDDcorrFile.Text = fileName;
                            break;
                        default:
                            PDDSignalFile.Text = fileName;
                            break;
                    }
                }
            }
            
        }



        /// <summary>
        ///  // SSD FSZX FSZY Depth OF      Has no check for energy...
        /// </summary>


        private void ReadOFFile(string filenameOF)
        {
            string[] lines = File.ReadAllLines(@filenameOF);           
            int k = 0;

            for (int i = 1; i < lines.Count(); i++)
            {
                if (!String.IsNullOrWhiteSpace(lines[i])){
                    OFList.Add(new OF());
                    string[] ofPropsAsText = lines[i].Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    OFList[k].SSD = int.Parse(ofPropsAsText[0]);
                    OFList[k].FieldSizeX = int.Parse(ofPropsAsText[1]);
                    OFList[k].FieldSizeY = int.Parse(ofPropsAsText[2]);
                    OFList[k].PointDepth = int.Parse(ofPropsAsText[3]);
                    OFList[k].PointRelDose = double.Parse(ofPropsAsText[4]);                    
                }
                k++;
            }
        }


        private void ReadPDDFile(string filenamePDD)
        {

            // read all lines into array of strings; pddFileTextArray
            string[] pddFileTextArray = System.IO.File.ReadAllLines(filenamePDD);
            // assumes always 2 lines of text as file header (not belonging to specific measurement)           
            for (int i = 0; i < 2; i++)
            {
                fileHeaderPDD[i] = pddFileTextArray[i];           // number of measurements in file must be updated when saving
            }
            #region PDD file read and convert to list of PDDList objects

            int pddFileIndex = 2;
            List<string> pddMeasurement = new List<string>();
            List<string> pddMeasurementHeader = new List<string>();
            List<double> pddMcoordX = new List<double>();
            List<double> pddMcoordY = new List<double>();
            List<double> pddMcoordZ = new List<double>();
            List<double> pddMrelDose = new List<double>();
            //var summaryPDD = new StringBuilder();

            int PDDIndex = 0;
            bool endOfTextFile = false;

            while (!endOfTextFile)
            {
                pddMeasurementHeader.Clear();
                pddMeasurement.Clear();
                pddMcoordX.Clear();
                pddMcoordY.Clear();
                pddMcoordZ.Clear();
                pddMrelDose.Clear();

                // the rest of pddFileTextArray starts at index 2, make list of PDD objects
                // ************************************ fill in properties of the pdd in list, read header to pdd.headertext

                bool endOfMeasurement = false;
                while (!endOfMeasurement)

                {
                    if (pddFileTextArray[pddFileIndex].Contains(":EOM  # End of Measurement"))
                    {
                        endOfMeasurement = true;
                        if (pddFileTextArray[pddFileIndex + 1].Contains(":EOF")) { endOfTextFile = true; }   // stops the while-loop if EOF found
                    }
                    else if (pddFileTextArray[pddFileIndex].Contains("="))
                    {
                        double[] coordandDoseAsNumbers = GetCoordinatesAndDose(pddFileTextArray[pddFileIndex]);
                        pddMcoordX.Add(coordandDoseAsNumbers[0]);
                        pddMcoordY.Add(coordandDoseAsNumbers[1]);
                        pddMcoordZ.Add(coordandDoseAsNumbers[2]);
                        pddMrelDose.Add(coordandDoseAsNumbers[3]);
                    }
                    else
                    {
                        pddMeasurementHeader.Add(pddFileTextArray[pddFileIndex]);
                    }
                    pddFileIndex++;
                }

                // after this there's temporary pdd-header and pdd-data that can be used and cleared until EOF end of file

                int fSZindex = pddMeasurementHeader.FindIndex(x => x.StartsWith("%FSZ"));
                string[] rowFSZ = pddMeasurementHeader[fSZindex].Split('\t');
                int FieldSX = int.Parse(rowFSZ[1]);
                int FieldSY = int.Parse(rowFSZ[2]);

                if (OFList.FindIndex(a => a.FieldSizeX == FieldSX && a.FieldSizeY == FieldSY) >= 0) // Before making new PDD-object check if the fieldsize is included in OF
                {
                    PDDList.Add(new PDD());

                    PDDList[PDDIndex].FieldSizeX = FieldSX;
                    PDDList[PDDIndex].FieldSizeY = FieldSY;

                    PDDList[PDDIndex].HeaderText = new List<string>(pddMeasurementHeader);  // had to set new otherwise only references                
                    PDDList[PDDIndex].CoordX = new List<double>(pddMcoordX);
                    PDDList[PDDIndex].CoordY = new List<double>(pddMcoordY);
                    PDDList[PDDIndex].CoordZ = new List<double>(pddMcoordZ);
                    PDDList[PDDIndex].RelDose = new List<double>(pddMrelDose);

                    PDDIndex++;
                };
            }

            PDDList.Sort((x, y) => x.FieldSizeX.CompareTo(y.FieldSizeX));  // can't sort unless change measurement number in header

            for (int i = 0; i < PDDList.Count(); i++)
            {
                // in the text "# Measurement number 	1",   replace the number with correct value, hardcoded index due to known format RFA
                string[] measurementNumberAsText = PDDList[i].HeaderText[3].Split('\t');
                PDDList[i].HeaderText[3] = PDDList[i].HeaderText[3].Replace(measurementNumberAsText[1], Convert.ToString(i + 1));
            }

            PDDOperations();
        }
        #endregion





        private void PDDOperations()
        {

            // Create new list of doubles for absolute dose (cGy/100MU) or more correct normalised signal, recombination correction and corrected renormalised PDD for every PDD object in PDDList
            int indexOF = 0;
            int indexDepthToNormalise = 0;
            foreach (PDD depthDose in PDDList)
            {
                depthDose.AbsDose = new List<double>();
                depthDose.RecombCorr = new List<double>();
                depthDose.CorrAbsDose = new List<double>();
                depthDose.CorrRelDose = new List<double>();
                indexOF = OFList.FindIndex(a => a.FieldSizeX == depthDose.FieldSizeX && a.FieldSizeY == depthDose.FieldSizeY);  // get the index of OF to use
                indexDepthToNormalise = depthDose.CoordZ.FindIndex(b => b.Equals(OFList[indexOF].PointDepth));  // works IF exact depth exists in depth dose...
                Console.WriteLine(indexOF + "  " + OFList[indexOF].FieldSizeX + " " + depthDose.FieldSizeX);
                foreach (double relD in depthDose.RelDose)
                {
                    depthDose.AbsDose.Add(100 * relD * OFList[indexOF].PointRelDose / depthDose.RelDose[indexDepthToNormalise]); // renormalises the depthdose to depth of OF and applies the output factor
                    depthDose.RecombCorr.Add(depthDose.AbsDose[depthDose.AbsDose.Count()-1] * RecombinationK + RecombinationM);
                    depthDose.AbsDose.Max();
                }
                for (int i = 0; i < depthDose.AbsDose.Count(); i++)
                {
                    depthDose.CorrAbsDose.Add(depthDose.AbsDose[i] * depthDose.RecombCorr[i]);
                }
                // Normalise separately to avoid finding the index of max
                double normValue = depthDose.CorrAbsDose.Max();

                for (int i = 0; i < depthDose.RelDose.Count(); i++)
                {
                    depthDose.CorrRelDose.Add(100 * depthDose.CorrAbsDose[i] / normValue);
                }
            }
        }





        




        private void ReadProfFile(string FileName)
        {
                       
            // read all lines into array of strings; pddFileTextArray
            string[] profileFileTextArray = System.IO.File.ReadAllLines(FileName);

            for (int i = 0; i < 2; i++)
            {
                fileHeaderProf[i] = profileFileTextArray[i];
            }

            #region Profile file read and convert to list of ProfileList objects

            int profileFileIndex = 2;
            List<string> profileMeasurement = new List<string>();
            List<string> profileMeasurementHeader = new List<string>();
            List<double> profileMcoordX = new List<double>();
            List<double> profileMcoordY = new List<double>();
            List<double> profileMcoordZ = new List<double>();
            List<double> profileMrelDose = new List<double>();
            var summaryProf = new StringBuilder();
            //List<Profile> profileList = new List<Profile>();
            int profileIndex = 0;
            bool endOfTextFile = false;

            while (!endOfTextFile)

            {
                profileMeasurementHeader.Clear();
                profileMeasurement.Clear();
                profileMcoordX.Clear();
                profileMcoordY.Clear();
                profileMcoordZ.Clear();
                profileMrelDose.Clear();

                // the rest of profileFileTextArray starts at index 2, make list of Profile objects
                // ************************************ fill in properties of the pdd in list, read header to pdd.headertext

                bool endOfMeasurement = false;
                while (!endOfMeasurement)
                {

                    if (profileFileTextArray[profileFileIndex].Contains(":EOM  # End of Measurement"))
                    {
                        endOfMeasurement = true;
                        if (profileFileTextArray[profileFileIndex + 1].Contains(":EOF")) { endOfTextFile = true; }   // stops the while-loop if EOF found
                       

                    }
                    else if (profileFileTextArray[profileFileIndex].Contains("="))
                    {
                        //pddMeasurement.Add(pddFileTextArray[pddFileIndex]);
                        double[] coordandDoseAsNumbers = GetCoordinatesAndDose(profileFileTextArray[profileFileIndex]);
                        profileMcoordX.Add(coordandDoseAsNumbers[0]);
                        profileMcoordY.Add(coordandDoseAsNumbers[1]);
                        profileMcoordZ.Add(coordandDoseAsNumbers[2]);
                        profileMrelDose.Add(coordandDoseAsNumbers[3]);
                    }
                    else
                    {
                        profileMeasurementHeader.Add(profileFileTextArray[profileFileIndex]);
                    }
                    profileFileIndex++;
                }
                // efter detta finns temporär enskiljd pdd-header och pdd-data som kan användas och sen clearas (redan gjort ovan ) tills EOF end of file läses

                int fSZindex = profileMeasurementHeader.FindIndex(x => x.StartsWith("%FSZ"));
                string[] rowFSZ = profileMeasurementHeader[fSZindex].Split('\t');
                int FieldSX = int.Parse(rowFSZ[1]);
                int FieldSY = int.Parse(rowFSZ[2]);


                if (PDDList.FindIndex(a => a.FieldSizeX == FieldSX && a.FieldSizeY == FieldSY) >= 0) // Before making new Profile-object check if the fieldsize is included in DepthDose
                {
                    profileList.Add(new Profile());                                                     // makes new empty object in ProfileList for every EOM found 

                    profileList[profileIndex].HeaderText = new List<string>(profileMeasurementHeader);  // had to set new otherwise only references                
                    profileList[profileIndex].CoordX = new List<double>(profileMcoordX);
                    profileList[profileIndex].CoordY = new List<double>(profileMcoordY);
                    profileList[profileIndex].CoordZ = new List<double>(profileMcoordZ);
                    profileList[profileIndex].RelDose = new List<double>(profileMrelDose);

                    profileList[profileIndex].FieldSizeX = FieldSX;
                    profileList[profileIndex].FieldSizeY = FieldSY;
                    profileList[profileIndex].ProfileDepth = profileList[profileIndex].CoordZ[0];

                    profileIndex++;
                }
            }

            // Update the fileheader stating the number of measurements with the correct value
            int actualNoOfProfiles = profileList.Count();
            string[] nrOfProfilesAsText = fileHeaderProf[0].Split('\t');
            fileHeaderProf[0] = fileHeaderProf[0].Replace(nrOfProfilesAsText[1], Convert.ToString(actualNoOfProfiles));

            //Sort by fieldsize and depth, ascending, and update the measurement number in the measurement header of each profile

            profileList = profileList.OrderBy(x => x.FieldSizeX).ThenBy(x => x.ProfileDepth).ToList();
                       
            for (int i = 0; i < profileList.Count(); i++)
            {
                // in the text "# Measurement number 	1",   replace the number with correct value, hardcoded index due to known format RFA
                string[] measurementNumberAsText = profileList[i].HeaderText[3].Split('\t');
                profileList[i].HeaderText[3] = profileList[i].HeaderText[3].Replace(measurementNumberAsText[1], Convert.ToString(i + 1));
            }

            ProfileOperations();  // call method to create recombination correction etc. for the profiles
            #endregion
        }



        private void ProfileOperations()
        {
            //      Create new list of doubles for absolute dose (cGy/100MU) (really renormalised signal...) for every profile object in profileList
            //      if exact depth found in depth dose, get PDD data for profile normalisation
            //      else
            //      get index closest below and above and make a linear interpolation

            double profNormValue;
            int indexDD = 0;

            foreach (Profile profile in profileList)
            {
                profile.AbsDose = new List<double>();
                profile.RecombCorr = new List<double>();
                profile.CorrAbsDose = new List<double>();
                profile.CorrRelDose = new List<double>();
                indexDD = PDDList.FindIndex(a => a.FieldSizeX == profile.FieldSizeX && a.FieldSizeY == profile.FieldSizeY);  // get the index of depth dose to use
                
                if (PDDList[indexDD].CoordZ.Contains(profile.ProfileDepth))
                {
                    int doseValueIndex = PDDList[indexDD].CoordZ.FindIndex(a => a == profile.ProfileDepth);
                    profNormValue = PDDList[indexDD].AbsDose[doseValueIndex];
                }
                else
                {
                    int ipr = PDDList[indexDD].CoordZ.FindIndex(m => profile.ProfileDepth >= m);
                    profNormValue = PDDList[indexDD].AbsDose[ipr - 1] + (profile.ProfileDepth - PDDList[indexDD].CoordZ[ipr - 1]) * (PDDList[indexDD].AbsDose[ipr] - PDDList[indexDD].AbsDose[ipr - 1]) / (PDDList[indexDD].CoordZ[ipr] - PDDList[indexDD].CoordZ[ipr - 1]);
                }

                foreach (double relD in profile.RelDose)
                {
                    profile.AbsDose.Add(relD * profNormValue / 100);
                }
                
                for (int i = 0; i < profile.RelDose.Count(); i++) 
                {
                    profile.RecombCorr.Add(profile.AbsDose[i] * RecombinationK + RecombinationM);
                    profile.CorrAbsDose.Add(profile.AbsDose[i] * profile.RecombCorr[i]);
                }

                double normValue = profile.CorrAbsDose.Max();

                for (int i = 0; i < profile.RelDose.Count(); i++)
                {
                    profile.CorrRelDose.Add(100 * profile.CorrAbsDose[i] / normValue);
                }
            }
        }







        private void WritePDDFile(string fileName, string outputType)
        {
            using (StreamWriter writetext = new StreamWriter(@fileName, true))
            {
                writetext.WriteLine(fileHeaderPDD[0]);
                writetext.WriteLine(fileHeaderPDD[1]);
                foreach (PDD DepthDose in PDDList)
                {
                    // Write header info for each scan
                    for (int i = 0; i < DepthDose.HeaderText.Count; i++)
                    {
                        writetext.WriteLine(DepthDose.HeaderText[i]);
                    }
                    // Convert and write coordinate and dose data
                    switch (outputType)
                        
                    {
                        case "ad":
                            {
                                for (int j = 0; j < DepthDose.CoordX.Count; j++)
                                {
                                    string rowStringPDD = DataToAcceptFormat(DepthDose.CoordX[j], DepthDose.CoordY[j], DepthDose.CoordZ[j], DepthDose.AbsDose[j]);
                                    writetext.WriteLine(rowStringPDD);
                                }
                                break;
                            }
                        case "crd":
                            {
                                for (int j = 0; j < DepthDose.CoordX.Count; j++)
                                {
                                    string rowStringPDD = DataToAcceptFormat(DepthDose.CoordX[j], DepthDose.CoordY[j], DepthDose.CoordZ[j], DepthDose.CorrRelDose[j]);
                                    writetext.WriteLine(rowStringPDD);
                                }
                                break;
                            }
                        case "csv":
                            {
                                string headerCsv = "Inline Crossline Depth RelDose NormSignal RecombCorr corrSignal corrRelDose";
                                writetext.WriteLine(headerCsv);
                                for (int j = 0; j < DepthDose.CoordX.Count; j++)
                                {
                                    string rowStringPDD = DataToCSV(DepthDose.CoordX[j], DepthDose.CoordY[j], DepthDose.CoordZ[j], DepthDose.RelDose[j], DepthDose.AbsDose[j], DepthDose.RecombCorr[j], DepthDose.CorrAbsDose[j], DepthDose.CorrRelDose[j]);
                                    writetext.WriteLine(rowStringPDD);
                                }
                                break;
                            }
                    }


                    writetext.WriteLine(":EOM  # End of Measurement");
                }
                writetext.WriteLine(":EOF # End of File");
            }
        }



        private void WriteProfFile(string fileName, string outputType)
        {
            using (StreamWriter writetext = new StreamWriter(@fileName, true))
            {
                writetext.WriteLine(fileHeaderProf[0]);
                writetext.WriteLine(fileHeaderProf[1]);
                MessageBox.Show(fileHeaderProf[0]);
                foreach (Profile profile in profileList)
                {
                    // Write header info for each scan
                    for (int i = 0; i < profile.HeaderText.Count; i++)
                    {
                        writetext.WriteLine(profile.HeaderText[i]);
                    }
                    // Convert and write coordinate and dose data
                    switch (outputType)

                    {
                        case "ad":
                            {
                                for (int j = 0; j < profile.CoordX.Count; j++)
                                {
                                    string rowStringProf = DataToAcceptFormat(profile.CoordX[j], profile.CoordY[j], profile.CoordZ[j], profile.AbsDose[j]);
                                    writetext.WriteLine(rowStringProf);                                    
                                }
                                break;
                            }
                        case "crd":
                            {
                                for (int j = 0; j < profile.CoordX.Count; j++)
                                {
                                    string rowStringProf = DataToAcceptFormat(profile.CoordX[j], profile.CoordY[j], profile.CoordZ[j], profile.CorrRelDose[j]);
                                    writetext.WriteLine(rowStringProf);
                                }
                                break;
                            }
                        case "csv":
                            {
                                string headerCsv = "Inline Crossline Depth RelDose NormSignal RecombCorr corrSignal corrRelDose";
                                writetext.WriteLine(headerCsv);
                                for (int j = 0; j < profile.CoordX.Count; j++)
                                {
                                    string rowStringProf = DataToCSV(profile.CoordX[j], profile.CoordY[j], profile.CoordZ[j], profile.RelDose[j], profile.AbsDose[j], profile.RecombCorr[j], profile.CorrAbsDose[j], profile.CorrRelDose[j]);
                                    writetext.WriteLine(rowStringProf);
                                }
                                break;
                            }
                    }


                    writetext.WriteLine(":EOM  # End of Measurement");
                }
                writetext.WriteLine(":EOF # End of File");
            }
        }












        private static string DataToAcceptFormat(double x, double y, double z, double d)
        {
            string xText = x.ToString("0.0");
            string yText = y.ToString("0.0");
            string zText = z.ToString("0.0");
            string dText = d.ToString("0.0");
            // Accept format uses 6 chars before decimal point including negative sign

            while (xText.Length < 7)
            {
                xText = " " + xText;
            }
            while (yText.Length < 7)
            {
                yText = " " + yText;
            }
            while (zText.Length < 7)
            {
                zText = " " + zText;
            }
            while (dText.Length < 7)
            {
                dText = " " + dText;
            }
            string returnString = "= " + "\t" + xText + "\t" + yText + "\t" + zText + "\t" + dText;
            return returnString.Replace(",", ".");
        }


        private static string DataToCSV(double x, double y, double z, double rd, double ad, double c, double cad, double crd)
        {
            string xText = x.ToString("0.0");
            string yText = y.ToString("0.0");
            string zText = z.ToString("0.0");
            string rdText = rd.ToString("0.0");
            string adText = ad.ToString("0.0");
            string cText = c.ToString("0.000");
            string cadText = cad.ToString("0.0");
            string crdText = crd.ToString("0.0");
      
            string returnString = $"{xText} {yText} {zText} {rdText} {adText} {cText} {cadText} {crdText}";
            return returnString;
        }



        private static double[] GetCoordinatesAndDose(string textLine)
        {
            string[] coordAndDose = textLine.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            coordAndDose[2] = coordAndDose[2].Replace(".", ",");
            coordAndDose[3] = coordAndDose[3].Replace(".", ",");
            coordAndDose[4] = coordAndDose[4].Replace(".", ",");
            coordAndDose[5] = coordAndDose[5].Replace(".", ",");
            double x = 0;
            double y = 0;
            double z = 0;
            double d = 0;

            try
            {
                x = double.Parse(coordAndDose[2]);
                y = double.Parse(coordAndDose[3]);
                z = double.Parse(coordAndDose[4]);
                d = double.Parse(coordAndDose[5]);
            }

            catch (Exception)
            {
                Console.WriteLine("Could not get numbers from this line: " + textLine);
                Console.WriteLine((coordAndDose[2]));
                Console.WriteLine((coordAndDose[3]));
                Console.WriteLine((coordAndDose[4]));
                Console.WriteLine((coordAndDose[5]));
            }
            double[] returnNumbers = { x, y, z, d };
            return returnNumbers;

        }

        public void DebugDefault10FFFData()
        {
            string filenameOF = "C:\\Users\\Mathias\\Documents\\Work\\Rekombination\\10FFF\\10FFFinput\\OF10FFF.txt";
            TextBoxOFFile.Text = filenameOF;
            ReadOFFile(filenameOF);
            OFList.Sort((x, y) => x.FieldSizeX.CompareTo(y.FieldSizeX));
            string filenamePDD = "C:\\Users\\Mathias\\Documents\\Work\\Rekombination\\10FFF\\10FFFinput\\PDD10FFFCC13.asc";
            TextBoxPDDFile.Text = filenamePDD;
            ReadPDDFile(filenamePDD);
        }
    }
}
