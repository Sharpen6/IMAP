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
    public class SymBA : Planner
    {
        public override bool Run(string sProblemPath)
        {
            try
            {
                CleanFolder(sProblemPath);
                string plannerPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\Planners\\Symba\\";
                if (!RunTranslation(sProblemPath, plannerPath)) return false;
                if (!RunPreprocess(sProblemPath, plannerPath)) return false;
                if (!RunSearch(sProblemPath, plannerPath)) return false;
                List<string> plan = Read_SAS_Plan(sProblemPath + "sas_plan");
                Plan = plan;
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void CleanFolder(string sPath)
        {
            File.Delete(sPath + "output.sas");
            File.Delete(sPath + "output");
            File.Delete(sPath + "sas_plan");
            File.Delete(sPath + "plan_numbers_and_cost");           
        }
        private bool RunSearch(string sPath, string plannerPath)
        {
            Process p = new Process();
            p.StartInfo.WorkingDirectory = sPath;
            p.StartInfo.FileName = plannerPath + "search\\downward-1.exe";
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
    }
}
