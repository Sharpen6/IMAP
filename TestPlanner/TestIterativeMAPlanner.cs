using IMAP;
using IMAP.General;
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
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p);
            PlanResult result = ma_planner.Plan();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IterativeMAPlanner_TestPlan_TwoJoint()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B2\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B2\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p);
            PlanResult result = ma_planner.Plan();
            Assert.IsNull(result);
        }
        [TestMethod]
        public void IterativeMAPlanner_TestGetConstraintsForNextAgents()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B2\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B2\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            IterativeMAPlanner ma_planner = new IterativeMAPlanner(d, p);

            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(d, p, SDRPlanner.Planners.FF);
            // Get the first agent
            Constant agent = d.GetAgents()[0];
            PlanResult pr = saSDR.Plan(agent, null, null, null);
            Dictionary<Constant, List<Action>> constraints = ma_planner.GetConstraintsForNextAgents(pr);
            if (!constraints.ContainsKey(d.GetAgents()[1]))
                Assert.IsTrue(false);

        }
    }
}
