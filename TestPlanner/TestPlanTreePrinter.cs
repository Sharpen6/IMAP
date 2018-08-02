using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TestPlanner
{
    [TestClass]
    public class TestPlanTreePrinter
    {
        [TestMethod]
        public void TestPrint()
        {
            ConditionalPlanTreeNode cptn1 = new ConditionalPlanTreeNode();
            Assert.AreEqual(PlanTreePrinter.Print(cptn1), "");
        }
        [TestMethod]
        public void TestPrint2()
        {
            ConditionalPlanTreeNode cptn1 = new ConditionalPlanTreeNode();
            Assert.AreEqual(PlanTreePrinter.Print(cptn1), "");
        }
    }
}
