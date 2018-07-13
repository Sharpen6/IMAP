using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IMAP.General;
using System.IO;
using IMAP.SDRPlanners;

namespace TestPlanner
{
    [TestClass]
    public class TestParse
    {
        [TestMethod]
        public void TestParseDomain()
        {
            string filePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\B3\d.pddl";
            Domain d = Parser.ParseDomain(filePath, "agent");
            Assert.AreEqual(4, d.Actions.Count);
            Assert.AreEqual(11, d.Constants.Count);
        }
    }
}
