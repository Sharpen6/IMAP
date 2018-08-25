using IMAP.General;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TestPlanner
{
    [TestClass]
    public class TestSDR
    {
        [TestMethod]
        public void TestParseDomain()
        {
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Assert.AreEqual(d.Actions.Count, 4);
            Assert.AreEqual(d.AgentCallsign, "agent");
            Assert.IsTrue(File.Exists(d.FilePath));
        }
        [TestMethod]
        public void TestParseProblem()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            Assert.IsTrue(File.Exists(p.FilePath));
            Assert.AreEqual(p.Hidden.Count, 3);
        }

        [TestMethod]
        public void TestSDRStartBoxes()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            Assert.AreEqual(4, d.Actions.Count);
            Assert.AreEqual(11, d.Constants.Count);
            // solve with all agents problem B3
            SDRPlanner sdr = new SDRPlanner(d, p, SDRPlanner.Planners.FF);
            sdr.Start();
        }

        [TestMethod]
        public void TestSDRStartButtons()
        {
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B1\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\ButtonPushing\B1\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            Assert.AreEqual(4, d.Actions.Count);
            Assert.AreEqual(11, d.Constants.Count);
            // solve with all agents problem B3
            SDRPlanner sdr = new SDRPlanner(d, p, SDRPlanner.Planners.FF);
            sdr.Start();
        }
    }
}
