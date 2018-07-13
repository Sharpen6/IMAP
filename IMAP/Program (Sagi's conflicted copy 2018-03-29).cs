using IMAP.SDRPlanners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP
{
    class Program
    {
        public static string BASE_PATH { get; set; }
        public static string PYTHON_PATH { get; set; }
        public static bool RedirectShellOutput { get; internal set; }

        static void Main(string[] args)
        {
            SetEnvironmentVars();
            string sProblemFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\";
            Symba symba = new Symba();
            symba.Run(sProblemFolder);
        }

        private static void SetEnvironmentVars()
        {
            string systemName = System.Environment.MachineName;
            if (systemName == "DESKTOP-FIFRUO5") //SAGI HOME PC
            {
                BASE_PATH =  @"R:\Dropbox\Dropbox\SDR\Offline";
                PYTHON_PATH = @"R:\Python27\python.exe";
            }
            else if (systemName == "SAGI") // SAGI LAPTOP
            {
                BASE_PATH = @"C:\Users\Sagi\Dropbox\SDR\Offline";
                PYTHON_PATH = @"C:\Python27\python.exe";
            }
            else
            {
                BASE_PATH = @"C:\Dropbox\SDR\Offline";
                PYTHON_PATH = "Guy's python path";
            }
        }
    }
}
