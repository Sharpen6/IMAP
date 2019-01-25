using System;
using System.IO;
using IMAP.General;
using IMAP.PlanTree;
using IMAP.SDRPlanners;

namespace TestBenchmarks
{
    class Test
    {
        internal static bool RunProblem(string mainPath)
        {
            string filePathProblem = mainPath + "p.pddl";
            string filePathDomain = mainPath + "d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p, SDRPlanner.Planners.FF);
            var result = ma_planner.Plan();
            foreach (var res in result)
            {
                string plan = PlanTreePrinter.Print(res.Value.Plan);
                File.WriteAllText(mainPath + "plan_" + res.Key.Name + ".txt", plan);
            }
            bool isValid = CheckMAPlan.IsValid2(d, p, result);
            return isValid;
        }
    }
}
