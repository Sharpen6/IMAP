using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IMAP.SDRPlanners
{
    public class Symba
    {
        private string m_sFFOutput;
        private static Dictionary<Thread, Process> FFProcesses = new Dictionary<Thread, Process>();
        public static TimeSpan PlannerTimeout = new TimeSpan(0, 0, 50);
        public bool Run(string sPath)
        { // sagi- I've changed how fd words.
            string plannerPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\Planners\\Symba\\";
            if (!RunTranslation(sPath, plannerPath)) return false;
            if (!RunPreprocess(sPath, plannerPath)) return false;
            if (!RunSearch(sPath, plannerPath)) return false;
            List<string> plan = ReadPlan(sPath + "sas_plan");
            return true;
        }
        private List<string> ReadPlan(string path)
        {
            List<string> lPlan = new List<string>();
            if (File.Exists(path))
            {
                StreamReader sr = new StreamReader(path);
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
            }
            return lPlan;
        }
        private bool RunSearch(string sPath, string plannerPath)
        {
            Process p = new Process();
            p.StartInfo.WorkingDirectory = sPath;
            p.StartInfo.FileName = plannerPath + "\\search\\downward-1.exe";
            //p.StartInfo.Arguments = "--heuristic \"hlm,hff = lm_ff_syn(lm_rhw(reasonable_orders = true, lm_cost_type = ONE, cost_type = ONE))\" --search \"lazy_greedy([hff, hlm],preferred =[hff, hlm],cost_type = ONE)\"";
            //p.StartInfo.Arguments = "--search \"lazy_greedy([ff()], preferred =[ff()])\"";
            //p.StartInfo.Arguments = "--search \"astar(lmcut())\"";
            //p.StartInfo.Arguments = "--search \"astar(blind())\"";
            p.StartInfo.Arguments = "--search \"astar(ff())\"";
            if (!RunProcess(p, "output"))
                return false;

            return true;
        }
        private bool RunPreprocess(string sPath, string plannerPath)
        {
            Process p = new Process();
            p.StartInfo.WorkingDirectory = sPath;
            p.StartInfo.FileName = plannerPath + "\\preprocess\\preprocess.exe";
            if (!RunProcess(p, "output.sas"))
                return false;

            return true;
        }
        private bool RunTranslation(string sPath, string plannerPath)
        {
            Process p = new Process();
            p.StartInfo.WorkingDirectory = sPath;
            string cmd = "";
            cmd = plannerPath + "translate\\translate.py Kd.pddl Kp.pddl";
            p.StartInfo.FileName = Program.PYTHON_PATH;
            p.StartInfo.Arguments = cmd;

            if (!RunProcess(p, null))
                return false;

            return true;

        }
        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            m_sFFOutput += outLine.Data + "\n";
        }
        public bool RunProcess(Process p, string sInputFile)
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
    }
}
