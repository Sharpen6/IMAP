﻿using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TestPlanner
{
    [TestClass]
    public class TestSingleAgentSDRPlanner
    {
        [TestMethod]
        public void SingleAgentSDRPlanner_TestConvertToSingleAgentProblemBoxes()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);

            // parameters        
            Constant currentAgent = new Constant("agent", "a1");

            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(d, p, 200, SDRPlanner.Planners.FF);
            PlanResult result = saSDR.Plan(currentAgent, null, null, null);
            Assert.IsNotNull(result.Plan);
        }

        [TestMethod]
        public void SingleAgentSDRPlanner_TestConvertToSingleAgentProblemBoxes_DUAL()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);

            // parameters        
            Constant currentAgent1 = new Constant("agent", "a1");
            Constant currentAgent2 = new Constant("agent", "a2");
            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(d, p, 200, SDRPlanner.Planners.FF);
            PlanResult result = saSDR.Plan(currentAgent1, null, null, null);
            result = saSDR.Plan(currentAgent2, null, null, null);
            Assert.IsNotNull(result.Plan);
        }

        [TestMethod]
        public void SingleAgentSDRPlanner_TestConvertToSingleAgentProblemButtons()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B1\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B1\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);

            // parameters        
            Constant currentAgent = new Constant("agent", "a1");

            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(d, p, 200, SDRPlanner.Planners.FF);
            PlanResult result = saSDR.Plan(currentAgent, null, null, null);
            Assert.IsNotNull(result.Plan);
        }
    }
}
