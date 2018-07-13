using System;
using IMAP.Formulas;
using IMAP.Predicates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlanner
{
    [TestClass]
    public class TestFormula
    {
        [TestMethod]
        public void TestFormulaToString()
        {
            //CompoundAndFormula f = new CompoundAndFormula();
            /*GroundedPredicate p1 = new GroundedPredicate("agent-at");
            Constant c1 = new Constant("a1", "agent");
            Constant c2 = new Constant("p1", "pos");
            p1.Constants.Add(c1);
            p1.Constants.Add(c2);
            Console.WriteLine(p1.ToString());
            PredicateFormula pf1 = new PredicateFormula(p1);

            GroundedPredicate p2 = new GroundedPredicate("adj");
            p2.Constants.Add(new Constant("p1", "pos"));
            p2.Constants.Add(new Constant("p2", "pos"));
            PredicateFormula pf2 = new PredicateFormula(p2);

            f.AddOperand(pf1);
            Assert.AreEqual("(and (agent-at a1 p1))", f.ToString());
            f.AddOperand(pf2);
            Assert.AreEqual("(and (agent-at a1 p1) (adj p1 p2))", f.ToString());*/
        }
    }
}
