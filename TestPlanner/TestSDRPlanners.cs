using System;
using System.IO;
using IMAP.General;
using IMAP.SDRPlanners;
using IMAP.SDRPlanners.ClassicPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlanner
{
    [TestClass]
    public class TestSDR
    {
        [TestMethod]
        public void TestSDRStart()
        {
            string filesPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\";
            string filePathProblem = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\p.pddl";
            string filePathDomain = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePathDomain, "agent");
            Problem p = Parser.ParseProblem(filePathProblem, d);
            Assert.AreEqual(4, d.Actions.Count);
            Assert.AreEqual(11, d.Constants.Count);

            SDRPlanner sdr = new SDRPlanner(d, p);
            sdr.Start();
        }
        [TestMethod]
        public void TestSymba()
        {
            string sProblemFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\";
            SymBA symba = new SymBA();
            if (symba.Run(sProblemFolder))
            {
                System.Collections.Generic.List<string> plan = symba.Plan;
                Assert.AreNotEqual(plan, null);
                Assert.AreEqual(plan.Count, 7);
            }
            else
            {
                Assert.IsFalse(true);
            }
            
        }
    }
}
