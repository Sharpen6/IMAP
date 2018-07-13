using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IMAP.SDRPlanners.ClassicPlanners
{
    public abstract class Planner
    {
        private static Dictionary<Thread, Process> FFProcesses = new Dictionary<Thread, Process>();
        public static TimeSpan PlannerTimeout = new TimeSpan(0, 0, 50);

        private string m_sFFOutput;

        public List<string> Plan = new List<string>();
        public abstract bool Run(string sProblemPath);
        protected List<string> Read_SAS_Plan(string sPath)
        {
            List<string> lPlan = new List<string>();
            StreamReader sr = new StreamReader(sPath);
            while (!sr.EndOfStream)
            {
                string sLine = sr.ReadLine().Trim().ToLower();
                sLine = sLine.Replace("(", "");
                sLine = sLine.Replace(")", "");
                if (sLine.Count() > 0 && !sLine.StartsWith(";"))
                {
                    int iStart = sLine.IndexOf("(");
                    sLine = sLine.Substring(iStart + 1).Trim();
                    lPlan.Add(sLine);
                }
            }
            sr.Close();
            return lPlan;
        }
        protected bool RunProcess(Process p, string sInputFile)
        {
            p.StartInfo.UseShellExecute = false;
            FFProcesses[Thread.CurrentThread] = p;
            if (Program.RedirectShellOutput)
            {
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            }
            if (sInputFile != null)
            {
                p.StartInfo.RedirectStandardInput = true;
            }

            p.Start();
            if (Program.RedirectShellOutput)
            {
                //string sOutput = p.StandardOutput.ReadToEnd();
                p.BeginOutputReadLine();
            }
            if (sInputFile != null)
            {
                StreamReader sr = new StreamReader(p.StartInfo.WorkingDirectory + "/" + sInputFile);
                StreamWriter sw = p.StandardInput;
                while (!sr.EndOfStream)
                    sw.WriteLine(sr.ReadLine());
                sr.Close();
                sw.Close();
            }

            //p.WaitForExit();
            if (!p.WaitForExit((int)PlannerTimeout.TotalMilliseconds))//2 minutes max
            {
                p.Kill();
                return false;
            }
            FFProcesses[Thread.CurrentThread] = null;
            return true;
        }
        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            m_sFFOutput += outLine.Data + "\n";
        }
    }
}
