using System;
using System.Collections.Generic;
using System.IO;
using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlanner
{
    [TestClass]
    public class TestConditionalPlanTreeNode
    {
        [TestMethod]
        public void TestGetGoalsTiming()
        {
            string main_path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\";
            string filePathProblem = main_path + "p.pddl";
            string filePathDomain = main_path + "d.pddl";

            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);


            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p);
            var ma_result = ma_planner.Plan();
            Assert.IsNotNull(ma_result);

            Constant a1 = d.GetAgents()[0];
            PlanResult pr = ma_result[a1];
            List<Predicate> goals = p.GetGoals();
            Dictionary<Predicate, int> timing = new Dictionary<Predicate, int>();
            pr.Plan.GetGoalsTiming(goals, null, ref timing);
            
            string plan = PlanTreePrinter.Print(pr.Plan);
            File.WriteAllText(main_path + "plan_" + a1.Name + ".txt", plan);
            Assert.AreEqual(timing.Count, p.GetGoals().Count - 1);
        }
    }
}
