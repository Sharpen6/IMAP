using IMAP.SDRPlanners;
using IMAP.SDRPlanners.ClassicPlanners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMAP
{
    class Program
    {
        private static string m_pythonPath = "";
        private static string m_basePath = "";
        public static string BASE_PATH
        {
            get
            {
                if (m_basePath == "")
                    SetEnvironmentVars();
                return m_basePath;
            }
            set
            {
                m_basePath = value;
            }
        }
        public static string PYTHON_PATH
        {
            get
            {
                if (m_pythonPath == "")
                    SetEnvironmentVars();
                return m_pythonPath;
            }
            set
            {
                m_pythonPath = value;
            }
        }
        public static bool RedirectShellOutput { get; internal set; }
        private static void SetEnvironmentVars()
        {
            string systemName = System.Environment.MachineName;
            if (systemName == "DESKTOP-FIFRUO5") //SAGI HOME PC
            {
                BASE_PATH = @"R:\Dropbox\Dropbox\SDR\Offline";
                PYTHON_PATH = @"R:\Python27\python.exe";
            }
            else if (systemName == "SAGI") // SAGI LAPTOP
            {
                BASE_PATH = @"C:\Users\Sagi\Dropbox\SDR\Offline";
                PYTHON_PATH = @"C:\Python27\python.exe";
            }
            else //if (systemName == "???")
            {
                BASE_PATH = @"put the planners path here";
                PYTHON_PATH = "python.exe";
            }
            //else
            //{
            //    BASE_PATH = @"put the planners path here";
            //    PYTHON_PATH = "put your python exe path here";
            //}
        }

        [STAThread]
        static void Main(string[] args)
        {
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.Main());
            }
        }
        
    }
}
