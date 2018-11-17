using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IMAP.General;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.PlanTree;

namespace TestPlanner
{
    [TestClass]
    public class RunPP_ButtonPushing
    {
        [TestMethod]
        public void RunPP_ButtonPushing_TestB1()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B1\";
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
            Console.WriteLine("Done");
        }
        [TestMethod]
        public void RunPP_ButtonmPushing_TestB3()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B3\";
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
            Console.WriteLine("Done");
        }
        [TestMethod]
        public void RunPP_ButtonPushing_TestB2()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B2\";
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
            Console.WriteLine("Done");
        }
    }
}
