using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TestBenchmarks
{
    [TestClass]
    public class BoxPushing
    {
        [TestMethod]
        public void RunPP_BoxPushing_TestB2()
        {
            string sProblemName = "B2";
            string mainPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\PlanningProblems\BoxPushing\"+ sProblemName + "\\";
            string s = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            bool isValid = Test.RunProblem(mainPath);
            Assert.IsTrue(isValid);
        }
    }
}
