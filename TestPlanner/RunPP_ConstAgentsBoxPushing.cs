using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IMAP.General;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.PlanTree;

namespace TestPlanner
{
    [TestClass]
    public class RunPP_ConstAgentsBoxPushing
    {
        [TestMethod]
        public void RunPP_ConstAgentsBoxPushing_TestB3_3()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ConstAgentsBoxPushing\B3.3\";
            string filePathProblem = main_path + "p.pddl";
            string filePathDomain = main_path + "d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p, SDRPlanner.Planners.FF);
            var result = ma_planner.Plan();
            foreach (var res in result)
            {
                string plan = PlanTreePrinter.Print(res.Value.Plan);
                File.WriteAllText(main_path + "plan_" + res.Key.Name + ".txt", plan);
            }
            bool isValid = CheckMAPlan.IsValid(result);
            Console.WriteLine("Is valid? " + isValid);
            Console.WriteLine("Done");
        }
    }
}
