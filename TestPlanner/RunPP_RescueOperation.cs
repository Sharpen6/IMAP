using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IMAP.General;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.PlanTree;

namespace TestPlanner
{
    [TestClass]
    public class RunPP_RescueOperation
    {
        [TestMethod]
        public void RunPP_RescueOperation_TestRO1()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\RescueOperation\RO1\";
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
        [TestMethod]
        public void RunPP_BoxPushing_TestB3()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\";
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
        public void RunPP_BoxPushing_TestB4()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B4\";
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
        public void RunPP_BoxPushing_TestB5()
        {
            string problem_id = "B5";
            
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\"+ problem_id + @"\";
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
        public void RunPP_BoxPushing_TestB6()
        {
            string problem_id = "B6";

            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\" + problem_id + @"\";
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
