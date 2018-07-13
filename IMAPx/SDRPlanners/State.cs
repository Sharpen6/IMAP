﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using IMAP.Predicates;
using IMAP.Formulas;
using IMAP.General;

namespace IMAP.SDRPlanners
{
    public class State
    {
        public IEnumerable<Predicate> Predicates { get { return m_lPredicates; } }
        protected HashSet<Predicate> m_lPredicates;
        public List<Action> AvailableActions { get; protected set; }
        private State m_sPredecessor;
        public bool MaintainNegations { get; private set; }
        public Problem Problem { get; private set; }
        public int ID { get; private set; }
        public Dictionary<string, double> FunctionValues { get; private set; }
        public int Time { get; private set; }
        public int ChoiceCount { get; private set; }

        public static int STATE_COUNT = 0;

        public State(Problem p)
        {
            Problem = p;
            m_sPredecessor = null;
            m_lPredicates = new HashSet<Predicate>();
            AvailableActions = new List<Action>();
            MaintainNegations = true;
            ID = STATE_COUNT++;
            FunctionValues = new Dictionary<string, double>();
            Time = 0;
            ChoiceCount = 0;
            foreach (string sFunction in Problem.Domain.Functions)
            {
                FunctionValues[sFunction] = 0.0;
            }
        }
        public State(State sPredecessor)
            : this(sPredecessor.Problem)
        {
            m_sPredecessor = sPredecessor;
            m_lPredicates = new HashSet<Predicate>(sPredecessor.Predicates);
            FunctionValues = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> p in sPredecessor.FunctionValues)
                FunctionValues[p.Key] = p.Value;
            Time = sPredecessor.Time + 1;
            MaintainNegations = sPredecessor.MaintainNegations;
        }

        public bool ConsistentWith(Predicate p)
        {
            foreach (Predicate pState in Predicates)
            {
                if (!p.ConsistentWith(pState))
                    return false;
            }
            return true;
        }

        public bool ConsistentWith(Formula f)
        {
            if (f is CompoundFormula)
            {
                CompoundFormula cf = (CompoundFormula)f;
                bool bConsistent = false;
                foreach (Formula fOperand in cf.Operands)
                {
                    bConsistent = ConsistentWith(fOperand);
                    if (cf.Operator == "and" && !bConsistent)
                        return false;
                    if (cf.Operator == "or" && bConsistent)
                        return true;
                    if (cf.Operator == "not")
                        return !bConsistent;
                }
                if (cf.Operator == "and")
                    return true;
                if (cf.Operator == "or")
                    return false;
            }
            else
            {
                PredicateFormula vf = (PredicateFormula)f;
                return ConsistentWith(vf.Predicate);
            }
            return false;
        }
        public void AddPredicate(Predicate p)
        {
            if (m_lPredicates.Contains(p))
                return;
            /*
            foreach (Predicate pState in Predicates)
            {
                if (pState.Equals(p))
                    return;
            }
             * */
            m_lPredicates.Add(p);

        }

        public override bool Equals(object obj)
        {
            if (obj is State)
            {
                State s = (State)obj;
                if (s.m_lPredicates.Count != m_lPredicates.Count)
                    return false;
                
                foreach (Predicate p in s.Predicates)
                    if (!Predicates.Contains(p))
                        return false;
                return true;
                 
                //return m_lPredicates.Equals(s.m_lPredicates);
            }
            return false;
        }
        public virtual void GroundAllActions()
        {
            AvailableActions = Problem.Domain.GroundAllActions(m_lPredicates, MaintainNegations);
        }
        public bool Contains(Formula f)
        {
            return f.ContainedIn(m_lPredicates, false);
        }
        public virtual State Clone()
        {
            //BUGBUG; //very slow? remove negations?
            return new State(this);
        }
        /*
        public State Apply(string sActionName)
        {
            sActionName = sActionName.Replace(' ', '_');//moving from ff format to local format
            if (AvailableActions.Count == 0)
                GroundAllActions(Problem.Domain.Actions);
            foreach (Action a in AvailableActions)
                if (a.Name == sActionName)
                    return Apply(a);
            return null;
        }
         * */
        public State Apply(string sActionName)
        {
            Action a = Problem.Domain.GroundActionByName(sActionName.Split(' '), m_lPredicates, false);
            if (a == null)
                return null;
            return Apply(a);
        }



        public State Apply(Action a)
        {
            //Debug.WriteLine("Executing " + a.Name);
            if (a is ParametrizedAction)
                return null;
            if (a.Preconditions != null && !a.Preconditions.IsTrue(m_lPredicates, MaintainNegations))
                return null;

            State sNew = Clone();
            sNew.Time = Time + 1;

            if (a.Effects == null)
                return sNew;

            if (a.Effects != null)
            {
                /*
                if (a.HasConditionalEffects)
                {
                    sNew.AddEffects(a.GetApplicableEffects(m_lPredicates, MaintainNegations));
                }
                else
                {
                    sNew.AddEffects(a.Effects);
                }
                 * */
                HashSet<Predicate> lDeleteList = new HashSet<Predicate>(), lAddList = new HashSet<Predicate>();
                GetApplicableEffects(a.Effects, lAddList, lDeleteList);
                foreach (Predicate p in lDeleteList)
                    sNew.AddEffect(p);
                foreach (Predicate p in lAddList)
                    sNew.AddEffect(p);
                //sNew.AddEffects(a.Effects);
            }
            if (!MaintainNegations)
                sNew.RemoveNegativePredicates();
            if (sNew.Predicates.Contains(Domain.FALSE_PREDICATE))
                Debug.WriteLine("BUGBUG");
            return sNew;
        }
        private void AddEffect(Predicate pEffect)
        {
            if (pEffect == Domain.FALSE_PREDICATE)
                Debug.WriteLine("BUGBUG");
            if (Problem.Domain.IsFunctionExpression(pEffect.Name))
            {
                GroundedPredicate gpIncreaseDecrease = (GroundedPredicate)pEffect;
                double dPreviousValue = m_sPredecessor.FunctionValues[gpIncreaseDecrease.Constants[0].Name];
                double dDiff = double.Parse(gpIncreaseDecrease.Constants[1].Name);
                double dNewValue = double.NaN;
                if (gpIncreaseDecrease.Name.ToLower() == "increase")
                    dNewValue = dPreviousValue + dDiff;
                else if (gpIncreaseDecrease.Name.ToLower() == "decrease")
                    dNewValue = dPreviousValue + dDiff;
                else
                    throw new NotImplementedException();
                FunctionValues[gpIncreaseDecrease.Constants[0].Name] = dNewValue;
            } 
            else if (!m_lPredicates.Contains(pEffect))
            {
                Predicate pNegateEffect = pEffect.Negate();
                if (m_lPredicates.Contains(pNegateEffect))
                {
                    //Debug.WriteLine("Removing " + pNegateEffect);
                    m_lPredicates.Remove(pNegateEffect);
                }
                /*
                if (!pEffect.Negation)
                {
                    //Debug.WriteLine("Adding " + pEffect);
                    m_lPredicates.Add(pEffect);
                }
                 * */
                m_lPredicates.Add(pEffect);//we are maintaining complete state information
            }
        }
        private void AddEffects(Formula fEffects)
        {
            if (fEffects is PredicateFormula)
            {
                AddEffect(((PredicateFormula)fEffects).Predicate);
            }
            else
            {
                CompoundFormula cf = (CompoundFormula)fEffects;
                if (cf.Operator == "oneof" || cf.Operator == "or")//BUGBUG - should treat or differently
                {
                    int iRandomIdx = RandomGenerator.Next(cf.Operands.Count);
                    AddEffects(cf.Operands[iRandomIdx]);
                    GroundedPredicate pChoice = new GroundedPredicate("Choice");
                    pChoice.AddConstant(new Constant("ActionIndex", "a" + (Time - 1)));//time - 1 because this is the action that generated the state, hence its index is i-1
                    pChoice.AddConstant(new Constant("ChoiceIndex", "c" + iRandomIdx));
                    State s = this;
                    while (s != null)
                    {
                        s.m_lPredicates.Add(pChoice);
                        s = s.m_sPredecessor;
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula f in cf.Operands)
                    {
                        if (f is PredicateFormula)
                        {
                            AddEffect(((PredicateFormula)f).Predicate);
                        }
                        else
                            AddEffects(f);
                    }
                }
                else if (cf.Operator == "when")
                {
                    if (m_sPredecessor.Contains(cf.Operands[0]))
                        AddEffects(cf.Operands[1]);
                }
                else
                    throw new NotImplementedException();
            }
        }

        private void GetApplicableEffects(Formula fEffects, HashSet<Predicate> lAdd, HashSet<Predicate> lDelete)
        {
            if (fEffects is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)fEffects).Predicate;
                if (p.Negation)
                    lDelete.Add(p);
                else
                    lAdd.Add(p);
            }
            else if (fEffects is ProbabilisticFormula)
            {
                ProbabilisticFormula pf = (ProbabilisticFormula)fEffects;
                double dRand = RandomGenerator.NextDouble();
                double dInitialRand = dRand;
                int iOption = 0;
                while (iOption < pf.Options.Count && dRand > 0)
                {
                    dRand -= pf.Probabilities[iOption];
                    iOption++;
                }
                if (dRand < 0.01) 
                {
                    iOption--;

                    GetApplicableEffects(pf.Options[iOption], lAdd, lDelete);
                }
                else //the no-op option was chosen
                {
                    iOption = -1;
                }
                GroundedPredicate pChoice = new GroundedPredicate("Choice");
                pChoice.AddConstant(new Constant("ActionIndex", "a" + Time));
                pChoice.AddConstant(new Constant("ChoiceIndex", "c" + ChoiceCount + "." + iOption));
                ChoiceCount++;
                State s = this;
                while (s != null)
                {
                    s.m_lPredicates.Add(pChoice);
                    s = s.m_sPredecessor;
                }
                

            }
            else
            {
                CompoundFormula cf = (CompoundFormula)fEffects;
                if (cf.Operator == "oneof" || cf.Operator == "or")//BUGBUG - should treat or differently
                {
                    int iRandomIdx = RandomGenerator.Next(cf.Operands.Count);
                    GetApplicableEffects(cf.Operands[iRandomIdx], lAdd, lDelete);
                    GroundedPredicate pChoice = new GroundedPredicate("Choice");
                    pChoice.AddConstant(new Constant("ActionIndex", "a" + Time));
                    pChoice.AddConstant(new Constant("ChoiceIndex", "c" + ChoiceCount + "." + iRandomIdx));
                    ChoiceCount++;
                    State s = this;
                    while (s != null)
                    {
                        s.m_lPredicates.Add(pChoice);
                        s = s.m_sPredecessor;
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula f in cf.Operands)
                    {
                        GetApplicableEffects(f, lAdd, lDelete);
                    }
                }
                else if (cf.Operator == "when")
                {
                    if (Contains(cf.Operands[0]))
                        GetApplicableEffects(cf.Operands[1], lAdd, lDelete);
                }
                else if (cf is ParametrizedFormula)
                {
                    ParametrizedFormula pf = (ParametrizedFormula)cf;
                    foreach (Formula fNew in pf.Ground(Problem.Domain.Constants))
                        GetApplicableEffects(fNew, lAdd, lDelete);
                }
                else
                    throw new NotImplementedException();
            }
        }

        public Formula Observe(Formula fObserve)
        {
            if (fObserve is PredicateFormula)
            {
                Predicate pObserve = ((PredicateFormula)fObserve).Predicate;
                foreach (Predicate pCurrent in Predicates)
                {
                    if (pObserve.Equals( pCurrent ))
                    {
                        return new PredicateFormula(pCurrent);
                    }
                }
                return new PredicateFormula(pObserve.Negate());
            }
            throw new NotImplementedException("Not handling compound formulas for observations");
        }

        public void RemoveNegativePredicates()
        {
            HashSet<Predicate> lFiltered = new HashSet<Predicate>();
            foreach (Predicate pObserved in m_lPredicates)
            {
                if (pObserved.Negation == false)
                {
                    lFiltered.Add(pObserved);
                }
            }
            m_lPredicates = lFiltered;
            MaintainNegations = false;
        }
        public override string ToString()
        {
            foreach (Predicate p in Predicates)
                if (p.Name == "at" && !p.Negation)
                    return p.ToString();
            return "";
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public KnowledgeState CreateKnowledgeState()
        {
            KnowledgeState kState = new KnowledgeState(Problem);
            foreach (Predicate p in Predicates)
                kState.m_lPredicates.Add(p);
            return kState;
        }

        public bool Contains(Predicate p)
        {
            if (p.Negation)
                return !Predicates.Contains(p.Negate());
            return Predicates.Contains(p);                   
        }


        internal void CompleteNegations()
        {
            throw new NotImplementedException();
        }
    }
}
