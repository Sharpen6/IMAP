using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMAP.General;
using IMAP.Predicates;
using IMAP.SDRPlanners;

namespace IMAP.Formulas
{
    public class PredicateFormula : Formula
    {
        public Predicate Predicate { get; private set; }
        public override List<Predicate> CollectPredicates
        {
            get
            {
                return new List<Predicate>() { Predicate };
            }
        }

        public PredicateFormula(Predicate p)
        {
            Predicate = p;
            Size = 1;
        }
        public override bool ContainsAgent(string sActiveAgent)
        {
            if (Predicate.Name == "active-agent" || Predicate.Name == "agent-at")
            {
                if (Predicate.ContainsConstant(new Constant(SDRPlanner.AgentName, sActiveAgent)))
                {
                    return true;
                }
            }
            return false;
        }

        public override void ChangeAgent(string sOldAgent, string sNewAgent)
        {
            Predicate newP = this.Predicate.Clone();
            if (Predicate.Name == "agent-at" || Predicate.Name == "active-agent")
            {
                newP.ChangeConstants(sOldAgent, sNewAgent);
                this.Predicate = newP;
            }
            
        }
         
        public override void RemoveAgent(Constant agent)
        {
            //throw new NotImplementedException();
        }
        public override Predicate GetTimeConstraint()
        {
            throw new NotImplementedException();
        }
        public override void RemoveTime()
        {
            string predicateName = Predicate.Name;
            if (predicateName == "next-time" || predicateName == "current-time")
            {
                Predicate = null;
            }        
        }

        public override Predicate GetAdjConstraint()
        {
            throw new NotImplementedException();
        }
        internal override Formula GetUnknownPredicates(List<string> m_lObservable)
        {
            if (m_lObservable.Contains(Predicate.Name))
            {
                return Clone();
            }
            return null;
        }
        public override bool IsTrue(IEnumerable<Predicate> lKnown, bool bContainsNegations)
        {
            if (Predicate == Domain.TRUE_PREDICATE)
                return true;
            if (Predicate == Domain.FALSE_PREDICATE)
                return false;
            if(Predicate.Name == "=" && Predicate is GroundedPredicate)
            {
                GroundedPredicate gp = (GroundedPredicate)Predicate;
                bool bIsSame = gp.Constants[0].Equals(gp.Constants[1]);
                if (gp.Negation)
                    return !bIsSame;
                return bIsSame;
            }
            if (lKnown != null)
            {
                if (bContainsNegations)
                {
                    return lKnown.Contains(Predicate);
                }
                else
                {
                    Predicate pCheck = Predicate;
                    if (Predicate.Negation)
                        pCheck = Predicate.Negate();

                    bool bContained = lKnown.Contains(pCheck);
                    if (!bContained && Predicate.Negation)
                        return true;

                    if (bContained && !Predicate.Negation)
                        return true;

                    return false;
                }



            }
            return false;
        }
        public override bool IsFalse(IEnumerable<Predicate> lKnown, bool bContainsNegations)
        {
            if (Predicate == Domain.FALSE_PREDICATE)
                return true;
            if (Predicate == Domain.TRUE_PREDICATE)
                return false;
            if (Predicate.Name == "=" && Predicate is GroundedPredicate)
            {
                GroundedPredicate gp = (GroundedPredicate)Predicate;
                bool bIsSame = gp.Constants[0].Equals(gp.Constants[1]);
                if(gp.Negation)
                    return bIsSame;
                return !bIsSame;
            }
            if (lKnown == null)
                return false;
            if (lKnown.Contains(Predicate))
                return false;
            Predicate pNegate = Predicate.Negate();

            if (lKnown != null)
            {
                bool bContained = lKnown.Contains(pNegate);
                if (bContained)
                    return true;
                if (pNegate.Negation && !bContainsNegations)
                    return true;
                return false;
            }
            return false;
        }
        public override string ToString()
        {
            return Predicate.ToString();
        }
        public override string ToJointString()
        {
            return Predicate.ToString().Replace(' ', '_').Replace('(','_').Replace(')','_').Replace('?','_').Trim('_');
        }
        public override Formula Negate(bool keepAND = false)
        {
            return new PredicateFormula(Predicate.Negate());
        }

        public override Formula Ground(Dictionary<string, Constant> dBindings)
        {
            if (Predicate is ParameterizedPredicate)
            {
                ParameterizedPredicate ppred = (ParameterizedPredicate)Predicate;
                GroundedPredicate gpred = ppred.Ground(dBindings);
                return new PredicateFormula(gpred);
            }
            if (Predicate is KnowPredicate)
            {
                KnowPredicate kp = (KnowPredicate)Predicate;
                GroundedPredicate gpred = kp.Ground(dBindings);
                return new PredicateFormula(gpred);
            }
            if (Predicate is KnowGivenPredicate)
            {
                throw new NotImplementedException();
            }
            return this;
        }
        public override Formula PartiallyGround(Dictionary<string, Constant> dBindings)
        {
            if (Predicate is ParameterizedPredicate)
            {
                ParameterizedPredicate ppred = (ParameterizedPredicate)Predicate;
                Predicate pGrounded = ppred.PartiallyGround(dBindings);
                return new PredicateFormula(pGrounded);
            }
            if (Predicate is KnowPredicate)
            {
                throw new NotImplementedException();
            }
            if (Predicate is KnowGivenPredicate)
            {
                throw new NotImplementedException();
            }
            return this;
        }

        public override void CollectAllPredicates(HashSet<Predicate> lPredicates)
        {
            Predicate nThis = Predicate.Negate();
            if (lPredicates.Contains(nThis))
            {
                lPredicates.Remove(nThis);
            }
            if (!lPredicates.Contains(Predicate))
                lPredicates.Add(Predicate);
        }

        public override void GetAllPredicates(HashSet<Predicate> lPredicates)
        {
            if (!lPredicates.Contains(Predicate))
                lPredicates.Add(Predicate);
        }

        public override void GetAllEffectPredicates(HashSet<Predicate> lConditionalPredicates, HashSet<Predicate> lNonConditionalPredicates)
        {
            GetAllPredicates(lNonConditionalPredicates);
        }


        public override bool ContainsCondition()
        {
            return false;
        }

        public override Formula Clone()
        {
            PredicateFormula f = new PredicateFormula(Predicate.Clone());
            return f;
        }

        public override bool ContainedIn(IEnumerable<Predicate> lPredicates, bool bContainsNegations)
        {
            Predicate pNegate = Predicate.Negate();
            foreach (Predicate pOther in lPredicates)
            {
                if (pOther.Equals(Predicate))
                    return true;
            }
            foreach (Predicate pOther in lPredicates)
            {
                if (pOther.Equals(pNegate))
                    //return false; sagichanged for  NOT heavy box grouned
                    return true;
            }
            if(!bContainsNegations)
                return Predicate.Negation;//assumes that predicate list contains only positives - not sure where this applies
            return false;
        }

        public override Formula Replace(Formula fOrg, Formula fNew)
        {
            if (Equals(fOrg))
                return fNew;
            return this;
        }
        public override Formula Replace(Dictionary<Formula, Formula> dTranslations)
        {
            if (dTranslations.ContainsKey(this))
                return dTranslations[this];
            return this;
        }

        public override Formula Simplify()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            PredicateFormula fOther = null;
            if (obj is CompoundFormula)
            {
                Formula fSimplify = ((CompoundFormula)obj).Simplify();
                if (fSimplify is PredicateFormula)
                    fOther = (PredicateFormula)fSimplify;
                else
                    return false;//might not be accurate - could be not
            }
            else if (obj is PredicateFormula)
            {
                fOther = (PredicateFormula)obj;
            }
            else
                return false;
            return (Predicate.Equals(fOther.Predicate));
        }

        public override Formula Regress(Action a, IEnumerable<Predicate> lObserved)
        {
            if (lObserved.Contains(Predicate))
                return new PredicateFormula(Domain.TRUE_PREDICATE);
            if(lObserved.Contains(Predicate.Negate()))
                return new PredicateFormula(Domain.FALSE_PREDICATE);
            return Regress(a);
        }

        public override Formula Regress(Action a)
        {
            if (a.ContainsNonDeterministicEffect)
                return RegressNonDet(a);
            else
                return RegressDet(a);
        }



        public Formula RegressNonDet(Action a)
        {
            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            int iCondition = 0;
            Predicate pNegate = Predicate.Negate();
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                HashSet<Predicate> lEffects = cfCondition.Operands[1].GetAllPredicates();
                HashSet<Predicate> lOptionalEffects = cfCondition.Operands[1].GetAllOptionalPredicates();
                if (lEffects.Contains(Predicate))
                {
                    int iChoice = cfCondition.GetChoiceIndex(Predicate);
                    cfOr.AddOperand(cfCondition.Operands[0].CreateRegression(Predicate, iChoice));
                    a.SetChoice(iCondition, iChoice);
                }
                else if (lEffects.Contains(pNegate))
                {

                    if (!lOptionalEffects.Contains(pNegate))
                        cfAndNot.AddOperand(cfCondition.Operands[0].Negate());
                    else
                    {
                        int iChoice = cfCondition.GetChoiceIndex(pNegate);
                        int iOtherChoice = cfCondition.GetOtherChoiceIndex(pNegate);
                        cfAndNot.AddOperand(cfCondition.Operands[0].CreateRegression(pNegate, iChoice).Negate());
                        a.SetChoice(iCondition, iOtherChoice);
                    }
                }
                iCondition++;
            }
            cfOr.AddOperand(this);
            cfAndNot.AddOperand(cfOr);
            return cfAndNot.Simplify();
        }

        public Formula RegressDet(Action a)
        {
            Formula f = a.RegressDet(Predicate);
            if (f != null)
                return f;

            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            int iCondition = 0;
            Predicate pNegate = Predicate.Negate();
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                HashSet<Predicate> lEffects = cfCondition.Operands[1].GetAllPredicates();
                if (lEffects.Contains(Predicate))
                {
                    cfOr.AddOperand(cfCondition.Operands[0].CreateRegression(Predicate, -1));
                }
                else if (lEffects.Contains(pNegate))
                {
                    cfAndNot.AddOperand(cfCondition.Operands[0].CreateRegression(pNegate, -1).Negate());
                }
                iCondition++;
            }
            cfOr.AddOperand(this);
            cfAndNot.AddOperand(cfOr);
            return cfAndNot.Simplify();
        }

        public Formula RegressII(Action a)
        {
            CompoundFormula cfAndNot = new CompoundFormula("and");
            CompoundFormula cfOr = new CompoundFormula("or");
            /*
            if (a.Effects is PredicateFormula)
            {
                if (a.Effects.Equals(this))
                    return AddPreconditions(a);//assuming that an effect can't be both deterministic and conditional
            }
            else
            {
                CompoundFormula cfEffects = (CompoundFormula)a.Effects;
                if (cfEffects.Operator != "and")
                    throw new NotImplementedException();
                foreach (Formula f in cfEffects.Operands)
                    if (f.Equals(this))
                        return AddPreconditions(a);//assuming that an effect can't be both deterministic and conditional
            }
             * */
            foreach (CompoundFormula cfCondition in a.GetConditions())
            {
                HashSet<Predicate> lEffects = new HashSet<Predicate>();
                cfCondition.Operands[1].GetAllPredicates(lEffects);
                if (lEffects.Contains(Predicate))
                {
                    cfOr.AddOperand(cfCondition.Operands[0]);
                }
                if (lEffects.Contains(Predicate.Negate()))
                    cfAndNot.AddOperand(cfCondition.Operands[0].Negate());
            }
            cfAndNot.AddOperand(this);
            cfOr.AddOperand(cfAndNot);
            return cfOr.Simplify();
        }

        private Formula AddPreconditions(Action a)
        {
            CompoundFormula cfOr = new CompoundFormula("or");
            CompoundFormula cfAnd = new CompoundFormula("and");
            cfAnd.AddOperand(a.Preconditions);
            cfAnd.AddOperand(Negate());
            cfOr.AddOperand(cfAnd);
            cfOr.AddOperand(this);
            return cfOr.Simplify();
        }

        public override Formula Reduce(IEnumerable<Predicate> lKnown)
        {
            Predicate pReduced = Predicate;
            if (lKnown.Contains(Predicate))
                pReduced = Domain.TRUE_PREDICATE;
            if (lKnown.Contains(Predicate.Negate()))
                pReduced = Domain.FALSE_PREDICATE;
            return new PredicateFormula(pReduced);
        }

        public override bool ContainsNonDeterministicEffect()
        {
            return false;
        }
        public override int GetMaxNonDeterministicOptions()
        {
            return 0;
        }

        public override void GetAllOptionalPredicates(HashSet<Predicate> lPredicates)
        {
            //predicate is not optional
        }

        public override Formula CreateRegression(Predicate p, int iChoice)
        {
            RegressedPredicate rpNew = new RegressedPredicate((GroundedPredicate)Predicate, p, iChoice);
            return new PredicateFormula(rpNew);
        }

        public override Formula GenerateGiven(string sTag, List<string> lAlwaysKnown)
        {
            if (lAlwaysKnown.Contains(Predicate.Name))
                return this;
            PredicateFormula pfGiven = new PredicateFormula(Predicate.GenerateKnowGiven(sTag));
            return pfGiven;
        }

        public override Formula AddTime(int iTime)
        {
            return new PredicateFormula(new TimePredicate(Predicate, iTime));
        }
        
        public override Formula ReplaceNegativeEffectsInCondition()
        {
            if (Predicate.Negation)
            {
                Predicate p = Predicate.Negate();
                p.Name = "Not-" + p.Name;
                return new PredicateFormula(p);
            }
            return this;
        }
        public override Formula RemoveImpossibleOptions(IEnumerable<Predicate> lObserved)
        {
            if (lObserved.Contains(Predicate.Negate()))
                return null;
            return this;
        }

        public override Formula ApplyKnown(IEnumerable<Predicate> lKnown)
        {
            return this;
            /* Seems like this is what we want, but perhaps not here
            if (lKnown.Contains(Predicate))
                return new PredicateFormula(Domain.TRUE_PREDICATE);
            else if(lKnown.Contains(Predicate.Negate()))
                return new PredicateFormula(Domain.FALSE_PREDICATE);
            return this;
             * */
        }

        public override List<Predicate> GetNonDeterministicEffects()
        {
            return new List<Predicate>();
        }

        public override Formula RemoveUniversalQuantifiers(List<Constant> lConstants, List<Predicate> lConstantPredicates, Domain d)
        {
            if (d != null && lConstantPredicates != null && d.AlwaysConstant(Predicate) && d.AlwaysKnown(Predicate) && !(Predicate is ParameterizedPredicate))
            {
                Predicate p = Predicate;
                if (p.Negation)
                    p = p.Negate();
                bool bContains = lConstantPredicates.Contains(p);
                //assuming that list does not contain negations
                if ((bContains && !Predicate.Negation) || (!bContains && Predicate.Negation))
                    return new PredicateFormula(Domain.TRUE_PREDICATE);
                else
                    return new PredicateFormula(Domain.FALSE_PREDICATE);
            }
            return this;
        }
        public override Formula GetKnowledgeFormula(List<string> lAlwaysKnown, bool bKnowWhether, HashSet<Predicate> lNegativePreconditions)
        {
            CompoundFormula cf = new CompoundFormula("and");
            if (Predicate.Name == Domain.OPTION_PREDICATE)
                return null;//we never know an option value
            if (lAlwaysKnown.Contains(Predicate.Name))
                cf.AddOperand(this);
            else
            {
                if (bKnowWhether)
                    cf.AddOperand(new PredicateFormula(new KnowWhetherPredicate(Predicate)));
                else
                {
                    //cf.AddOperand(this);
                   
                    Predicate pNot = new KnowPredicate(Predicate.Negate());
                    cf.AddOperand(pNot.Negate());
                    cf.AddOperand(new KnowPredicate(Predicate));// added by sagi 15.6 removed again, guy is right
                    //return new PredicateFormula(new KnowPredicate(Predicate)); sagi: removed for adding box moving, not only knowing..
                }
            }
            if(lNegativePreconditions != null && lNegativePreconditions.Contains(Predicate.Canonical()))
            {
                Predicate pNot = Predicate.Clone();
                pNot.Name = "Not" + pNot.Name;
                if (Predicate.Negation)
                    cf.AddOperand(pNot);
                else
                    cf.AddOperand(pNot.Negate());
            }
            return cf;
        }

        public override Formula ReduceConditions(IEnumerable<Predicate> lKnown)
        {
            return new PredicateFormula(Predicate);
        }

        public override Formula RemoveNegations()
        {
            if (Predicate.Negation)
                return null;
            return this;
        }

        public override Formula ToCNF()
        {
            return this;
        }

        public override void GetNonDeterministicOptions(List<CompoundFormula> lOptions)
        {
            
        }
        public override bool RemoveConstant(Constant agent)
        {
            return Predicate.ContainsConstant(agent);
        }
        internal override bool ContainsParameter(Parameter argument)
        {
            if (Predicate is ParameterizedPredicate)
                return Predicate.ContainsParameter(argument);
            else
                return false;
        }
        internal override int CountAgents(string sAgentCallsign)
        {
            return Predicate.GetInvolvedAgents(sAgentCallsign).Length;
        }
        internal override string[] GetAgents(string sAgentCallsign)
        {
            return Predicate.GetInvolvedAgents(sAgentCallsign);
        }
    }
}
