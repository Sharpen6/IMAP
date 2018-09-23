using IMAP.General;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace TestPlanner
{
    [TestClass]
    public class TestAgentSelector
    {
        [TestMethod]
        public void AgentSelector_TestGetNextAgent()
        {
            Constant a1 = new Constant("agent", "a1");
            Constant a2 = new Constant("agent", "a2");
            Constant a3 = new Constant("agent", "a3");
            Constant a4 = new Constant("agent", "a4");

            AgentSelector agentSelector = new AgentSelector(new List<Constant>() { a1, a2, a3, a4 }, null);
            Assert.AreEqual(agentSelector.GetNextAgent(), a1);
            Assert.AreEqual(agentSelector.GetNextAgent(), a2);
            Assert.AreEqual(agentSelector.GetNextAgent(), a3);
            Assert.AreEqual(agentSelector.GetNextAgent(), a4);
        }
    }
}
