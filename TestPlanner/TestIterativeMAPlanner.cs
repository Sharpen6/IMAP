using IMAP;
using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace TestPlanner
{
    [TestClass]
    public class TestIterativeMAPlanner
    {
        [TestMethod]
        public void IterativeMAPlanner_TestPlan()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p, SDRPlanner.Planners.FF);
            var result = ma_planner.Plan();
            Assert.AreEqual(result.Count, 2);
        }

        [TestMethod]
        public void IterativeMAPlanner_TestPlan_TwoJoint()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B2\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B2\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p, SDRPlanner.Planners.FF);
            var result = ma_planner.Plan();
            Assert.AreEqual(result.Count, 2);
        }
        [TestMethod]
        public void IterativeMAPlanner_TestIndepeneceBetweenRuns()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B2\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B2\d.pddl";

            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);

            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(d, p, SDRPlanner.Planners.FF);
            // Get the first agent
            Constant agent1 = d.GetAgents()[0];
            PlanResult pr1 = saSDR.Plan(agent1, null, null, null);
            string domainAfter_1 = d.ToString();
            PlanResult pr2 = saSDR.Plan(agent1, null, null, null);
            string domainAfter_2 = d.ToString();

            // General domain shoud remain the same after planning once
            Assert.AreEqual(d.ToString(), domainAfter_1);
            // General domain shoud remain the same after planning twice
            Assert.AreEqual(d.ToString(), domainAfter_2);
            // Both used domains (after one & two running) should remain the same
            Assert.AreEqual(pr1.m_agentDomain.ToString(), pr2.m_agentDomain.ToString());

            Assert.AreNotEqual(pr1.m_agentDomain.ToString(), d.ToString());
            Assert.AreNotEqual(pr2.m_agentDomain.ToString(), d.ToString());
        }
    }
}
