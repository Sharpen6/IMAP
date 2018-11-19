using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IMAP.General;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.PlanTree;

namespace TestPlanner
{
    [TestClass]
    public class RunPP_Rovers
    {
        private void RunTest(string ProblemPath)
        {
            string main_path = ProblemPath;
            string filePathProblem = main_path + "p.pddl";
            string filePathDomain = main_path + "d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "rover");
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
        public void RunPP_Rovers_TestR1()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R1\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR2()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R2\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR3()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R3\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR4()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R4\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR5()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R5\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR6()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R6\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR7()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R7\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR8()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R8\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR9()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R9\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR10()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R10\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR11()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R11\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR12()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R12\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR13()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R13\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR14()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R14\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR15()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R15\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR16()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R16\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR17()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R17\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR18()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R18\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR19()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R19\";
            RunTest(main_path);
        }
        [TestMethod]
        public void RunPP_Rovers_TestR20()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\Rovers\R20\";
            RunTest(main_path);
        }
    }
}
