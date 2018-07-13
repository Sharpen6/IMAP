using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using IMAP.SDRPlanners;
using IMAP.General;

namespace IMAP.Predicates
{
    public class GroundedPredicate : Predicate
    {
        public List<Constant> Constants { get; protected set; }
        public static GroundedPredicate pFalsePredicate = Domain.FALSE_PREDICATE;
        public static GroundedPredicate pTruePredicate = Domain.TRUE_PREDICATE;
        private GroundedPredicate m_gpNegation;
        public GroundedPredicate(string sName)
            : base(sName)
        {
            //if (sName == Domain.FALSE_PREDICATE)
            //    Debug.WriteLine("Initialized  a false predicate");
            m_gpNegation = null;
            Constants = new List<Constant>();
        }

        internal override void ChangeConstants(string prevAgent, string newAgent)
        {
            foreach (var item in Constants)
            {
                if (item.Name == prevAgent)
                    item.Name = newAgent;
            }
        }
        public GroundedPredicate(string sName, bool bNegate)
             : base(sName)
        {
            //if (sName == Domain.FALSE_PREDICATE)
            //    Debug.WriteLine("Initialized  a false predicate");
            Negation = bNegate;
            m_gpNegation = null;
            Constants = new List<Constant>();
        }
        public GroundedPredicate(GroundedPredicate gpOther)
            : base(gpOther.Name, gpOther.Negation)
        {
            //if (gpOther == Domain.FALSE_PREDICATE || gpOther == Domain.TRUE_PREDICATE)
                //Console.Write("*");
            List<Constant> newConstants = new List<Constant>();
            foreach (var item in gpOther.Constants)
            {
                newConstants.Add(new Constant(item.Type, item.Name));
            }
            Constants = newConstants;
        }
        protected override string GetString()
        {
            string s = "(" + Name + " " + Parser.ListToString((List<Constant>)Constants) + ")";
            if (Negation)
                s = "(not " + s + ")";
            return s;
        }
        public override bool ContainsConstant(Constant iName)
        {
            foreach (var item in Constants)
            {
                if (item.Name == iName.Name)
                    return true;               
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is GroundedPredicate)
            {
                return GetHashCode() == obj.GetHashCode();
            }
            return false;
        }

        public Dictionary<string,Constant> Bind(ParameterizedPredicate p)
        {
            if (Name != p.Name)
                return null;
            
            if (((List<Constant>)Constants).Count != ((List<Argument>)p.Parameters).Count)
                return null;

            Dictionary<string, Constant> dBindings = new Dictionary<string, Constant>();

            for (int i = 0; i < Constants.Count; i++)
            {
                Argument arg = p.Parameters.ElementAt(i);
                if (arg is Constant)
                {
                    if (!Constants[i].Equals(arg))
                        return null;
                }
                if (arg is Parameter)
                    dBindings[arg.Name] = Constants[i];
            }
            return dBindings;
        }


        public override bool ConsistentWith(Predicate p)
        {
            if (Name != p.Name)
                return true; //irrelvant predicate - no contradiction
            if (p is ParameterizedPredicate)
            {
                //TODO
                throw new NotImplementedException();
            }
            GroundedPredicate gp = (GroundedPredicate)p;
            if (((List<Constant>)Constants).Count != ((List<Constant>)gp.Constants).Count)
                return true;
            for (int i = 0; i < Constants.Count; i++)
            {
                if(!gp.Constants[i].Equals(Constants[i]))
                    return true;//irrelvant predicate - no contradiction
            }
            return Negation == p.Negation;
        }

        public void AddConstant(Constant c)
        {
            ((List<Constant>)Constants).Add(c);
            m_sCachedToString = null;
        }
        internal override bool ContainsParameter(Parameter argument)
        {
            return false;
        }
        public override Predicate Negate()
        {
            if (m_gpNegation == null)
            {
                m_gpNegation = new GroundedPredicate(this);
                m_gpNegation.Negation = !Negation;
                m_gpNegation.m_gpNegation = this;
            }
            return m_gpNegation;
        }

        public override bool IsContainedIn(List<Predicate> lPredicates)
        {
            return lPredicates.Contains(this);
        }

        public override Predicate GenerateKnowGiven(string sTag, bool bKnowWhether)
        {
            GroundedPredicate pKGiven = null;
            if (bKnowWhether)
                pKGiven = new GroundedPredicate("KWGiven" + Name);
            else
                pKGiven = new GroundedPredicate("KGiven" + Name);
            foreach (Constant c in Constants)
                pKGiven.AddConstant(c);
            pKGiven.AddConstant(new Constant(Domain.TAG, sTag));
            if (!bKnowWhether)
            {
                if (Negation)
                    pKGiven.AddConstant(new Constant(Domain.VALUE, Domain.FALSE_VALUE));
                else
                    pKGiven.AddConstant(new Constant(Domain.VALUE, Domain.TRUE_VALUE));
            }
            return pKGiven;
        }
        public override Predicate GenerateGiven(string sTag)
        {
            GroundedPredicate pGiven = new GroundedPredicate("Given" + Name);
            foreach (Constant c in Constants)
                pGiven.AddConstant(c);
            pGiven.AddConstant(new Constant(Domain.TAG, sTag));
            if (Negation)
                return pGiven.Negate();
            return pGiven;
        }

        public override Predicate ToTag()
        {
            GroundedPredicate gpNew = new GroundedPredicate(this);
            if(Negation)
                gpNew.Name = gpNew.Name + "-Remove";
            else
                gpNew.Name = gpNew.Name + "-Add";
            gpNew.Negation = false;

            return gpNew;
        }

        public override int Similarity(Predicate p)
        {
            if (Negation != p.Negation)
                //if (Name != p.Name || Negation != p.Negation)
                    return 0;
            if (p is GroundedPredicate)
            {
                GroundedPredicate gpGrounded = (GroundedPredicate)p;
                int iSimilarity = 0;
                if (Name == p.Name)
                {
                    for (int i = 0; i < Constants.Count; i++)
                        if (Constants[i].Equals(gpGrounded.Constants[i]))
                            iSimilarity++;
                }
                else
                {
                    foreach (Constant c in Constants)
                        if (gpGrounded.Constants.Contains(c))
                            iSimilarity++;

                }
                return iSimilarity;
            }
            return 0;
        }

        public override bool SameInvariant(Predicate p, Argument aInvariant)
        {
            if (Name != p.Name)
                return false;
            if (p is GroundedPredicate)
            {
                GroundedPredicate gpGrounded = (GroundedPredicate)p;
                for (int i = 0; i < Constants.Count; i++)
                {
                    if (Constants[i].Equals(aInvariant)
                        && !gpGrounded.Constants[i].Equals(aInvariant))
                        return false;
                }
                return true;
            }
            return false;
        }

        protected override int ComputeHashCode()
        {
            int iSum = 0;
            foreach(Constant c in Constants)
            {
                iSum += c.GetHashCode();
                iSum *= 100;
            }
            iSum += m_iName;
            return iSum;
        }

        public override Predicate Clone()
        {
            return new GroundedPredicate(this);
        }

        public string WritePredicate()
        {
            string res = "(" + Name;
            foreach (var constant in Constants)
            {
                res += " " + constant.Name + " - " + constant.Type;
            }
            return res + ")";
        }
        public override string[] GetInvolvedAgents(string sAgentCallsign)
        {
            List<string> ans = new List<string>();
            foreach (var item in Constants)
            {
                if (item.Type == sAgentCallsign)
                    ans.Add(item.Name);
            }
            return ans.ToArray();
        }
    }
}
