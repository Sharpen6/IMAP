﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using IMAP.SDRPlanners;
using IMAP.Predicates;
using IMAP.Formulas;
using IMAP.General;

namespace IMAP
{
    public class Action
    {
        private static int IDs = 0;

        public int ID { get; private set; }

        public string Name { get; set; }
        public Formula Preconditions { get; set; }
        public Formula Effects { get; set; }
        public Formula Observe { get; set; }
        public bool ContainsNonDeterministicEffect { get; protected set; }
        public HashSet<Predicate> NonDeterministicEffects { get; private set; }
        //public int Time { get; set; }
        public int Cost { get; set; }

        private Dictionary<int, List<int>> m_mMapConditionsChoices;

        private Dictionary<Predicate, Formula> m_mRegressions;
        internal ParametrizedAction BaseAction;

        public bool HasConditionalEffects { get; protected set; }

        public Action Original { get; private set; }
        public bool JointAction { get; set; }
        public Action OriginalActionBeforeSplit { get; set; }
        public Action OriginalActionBeforeRemovingAgent { get; internal set; }

        public Action(string sName)
        {
            Name = sName;
            m_mMapConditionsChoices = new Dictionary<int, List<int>>();
            ID = IDs++;
            NonDeterministicEffects = new HashSet<Predicate>();
        }

        public override string ToString()
        {
            string s = "(:action " + Name + "\n";
            if (Preconditions != null)
                s += " :precondition " + Preconditions + "\n";
            if (Effects != null)
            {
                if (SDRPlanner.AddActionCosts)
                {
                    CompoundFormula tmpFormula = new CompoundFormula("and");
                    tmpFormula.AddOperand(Effects);
                    GroundedPredicate gp = new GroundedPredicate("increase");
                    gp.AddConstant(new Constant("", "(total-cost)"));
                    gp.AddConstant(new Constant("", Cost.ToString()));
                    PredicateFormula pf = new PredicateFormula(gp);
                    tmpFormula.AddOperand(pf);
                    s += " :effect " + tmpFormula.ToString() + "\n";
                } else
                {
                    s += " :effect " + Effects.ToString() + "\n";
                }
            }
            if (Observe != null)
                s += " :observe " + Observe + "\n";
            s += ")";
            return s;
        }


        public void SetEffects(Formula f)
        {
            Effects = f;
            ContainsNonDeterministicEffect = f.ContainsNonDeterministicEffect();
            HasConditionalEffects = f.ContainsCondition();
        }

        private void SplitEffects(List<CompoundFormula> lConditions, List<Formula> lObligatory)
        {
            if (Effects == null)
                return;
            if (Effects is PredicateFormula)
            {
                lObligatory.Add(Effects);
                return;
            }
            if (Effects is CompoundFormula)
            {
                CompoundFormula cfEffects = (CompoundFormula)Effects;
                if (cfEffects.Operator == "when")
                {
                    lConditions.Add(cfEffects);
                    return;
                }
                if (cfEffects.Operator != "and")
                    throw new NotImplementedException();
                foreach (Formula fSub in cfEffects.Operands)
                {
                    if (fSub is PredicateFormula)
                        lObligatory.Add(fSub);
                    else if(fSub is CompoundFormula)
                    {
                        if (((CompoundFormula)fSub).Operator == "when")
                            lConditions.Add((CompoundFormula)fSub);
                        else
                            lObligatory.Add(fSub);
                    }
                    else if (fSub is ProbabilisticFormula)
                    {
                        //not doing anything here - assuming no nested conditions inside probabilistic
                    }
                }
            }
            if (Effects is ProbabilisticFormula)
            {
                ProbabilisticFormula pf = (ProbabilisticFormula)Effects;
                foreach (Formula fSub in pf.Options)
                {
                    if (fSub is PredicateFormula)
                        lObligatory.Add(fSub);
                    else
                    {
                        if (((CompoundFormula)fSub).Operator == "when")
                            lConditions.Add((CompoundFormula)fSub);
                        else
                            lObligatory.Add(fSub);
                    }
                }
            }
        }


        public Action RemoveNonDeterministicEffects(BeliefState bsInitialBelief)
        {
            if (Effects == null || !Effects.ContainsNonDeterministicEffect())
                return this;
            Action aNew = Clone();
            if (Original == null)
                aNew.Original = this;
            List<CompoundFormula> lOptions = new List<CompoundFormula>();
            Effects.GetNonDeterministicOptions(lOptions);
            HashSet<Predicate> hsNonDetPredicates = new HashSet<Predicate>();
            foreach (CompoundFormula cf in lOptions)
                cf.GetAllPredicates(hsNonDetPredicates);
            CompoundFormula cfEffects = (CompoundFormula)Effects;
            foreach (CompoundFormula cfOption in lOptions)
            {
                if (cfOption.Operator == "oneof")
                {
                    if (cfOption.Operands.Count != 2)
                        throw new NotImplementedException();
                    GroundedPredicate gpChoice = new GroundedPredicate(Name + "_" + Domain.OPTION_PREDICATE + "_" + bsInitialBelief.NextNonDetChoice());
                    //bsInitialBelief.Problem.Domain.Predicates.Add(gpChoice);
                    CompoundFormula cfChoice = new CompoundFormula("oneof");
                    cfChoice.SimpleAddOperand(gpChoice);
                    cfChoice.SimpleAddOperand(gpChoice.Negate());
                    bsInitialBelief.AddInitialStateFormula(cfChoice);

                    CompoundFormula cfWhenTrue = new CompoundFormula("when");
                    cfWhenTrue.AddOperand(gpChoice);
                    cfWhenTrue.AddOperand(cfOption.Operands[0]);

                    CompoundFormula cfWhenFalse = new CompoundFormula("when");
                    cfWhenFalse.AddOperand(gpChoice.Negate());
                    cfWhenFalse.AddOperand(cfOption.Operands[1]);

                    CompoundFormula cfAnd = new CompoundFormula("and");
                    foreach (Formula f in cfEffects.Operands)
                    {
                        if (!f.ContainsNonDeterministicEffect())
                            cfAnd.AddOperand(f);
                    }
                    cfAnd.AddOperand(cfWhenFalse);
                    cfAnd.AddOperand(cfWhenTrue);
                    aNew.Effects = cfAnd;
                    foreach (Predicate p in hsNonDetPredicates)
                        aNew.NonDeterministicEffects.Add(p);
                }
                else
                    throw new NotImplementedException();
            }
            return aNew;
        }

        public Action MergeWithAction(Action other)
        {
            if (other == null)
                return this;

            Action newAction = Clone();

            if (newAction.Effects is CompoundFormula)
            {
                ((CompoundFormula)newAction.Effects).AddOperand(other.Effects);
            }
            else
            {
                if (newAction.Effects == null)
                {
                    if (other.Effects == null)
                    {
                        // keep null, do nothing
                    }
                    else
                    {
                        newAction.Effects = other.Effects;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (newAction.Preconditions is CompoundFormula)
            {
                ((CompoundFormula)newAction.Preconditions).AddOperand(other.Preconditions);
            }
            else if (newAction.Preconditions is PredicateFormula)
            {
                CompoundFormula cf  = new CompoundFormula("and");
                cf.AddOperand(newAction.Preconditions);
                cf.AddOperand(other.Preconditions);
                newAction.Preconditions = cf;
            } else if (newAction.Preconditions==null)
            {
                CompoundFormula cf = new CompoundFormula("and");
                cf.AddOperand(other.Preconditions);
                newAction.Preconditions = cf;
            }
            else
            {
                throw new Exception();
            }

            return newAction;
        }
        /*
        public bool ContainsAgent(Constant otherAgent)
        {
            if (Preconditions!=null && Preconditions.GetAgents().ToList().Contains(otherAgent.Name))
                return true;
            if (Effects!=null && Effects.GetAgents().ToList().Contains(otherAgent.Name))
                return true;
            if (Observe!= null && Observe.GetAgents().ToList().Contains(otherAgent.Name))
                return true;
            return false;
        }*/

        internal void AddTime(int time, bool addPreconditions)
        {

            /*GroundedPredicate nextTimePredicate = new GroundedPredicate("next-time");
            nextTimePredicate.AddConstant(new Constant("time", "t" + time));
            nextTimePredicate.AddConstant(new Constant("time", "t" + (time + 1)));*/

            GroundedPredicate currTimePredicateStart = new GroundedPredicate("current-time");
            currTimePredicateStart.AddConstant(new Constant("time", "t" + time));

            /*GroundedPredicate currTimePredicateEnd = new GroundedPredicate("current-time");
            currTimePredicateEnd.AddConstant(new Constant("time", "t" + (time + 1)));*/

            /*if (addEffects)
            {
                if (Effects is CompoundFormula)
                {
                    CompoundFormula cfEffects = (CompoundFormula)Effects;

                    cfEffects.AddOperand(new PredicateFormula(currTimePredicateStart.Negate()));
                    cfEffects.AddOperand(new PredicateFormula(currTimePredicateEnd));
                }

            }*/
            if (addPreconditions)
            {
                if (Preconditions == null)
                {
                    Preconditions = new CompoundFormula("and");
                }

                if (Preconditions is CompoundFormula)
                {
                    CompoundFormula cfPreconditions = (CompoundFormula)Preconditions;

                    //cfPreconditions.AddOperand(new PredicateFormula(nextTimePredicate));
                    cfPreconditions.AddOperand(new PredicateFormula(currTimePredicateStart));
                }
            }

            Name += "_t" + time + "_t" + time;
        }

        internal int GetTime()
        {
            string[] nameParts = Name.Split('_');
            foreach (var item in nameParts)
            {
                if (item.StartsWith("t"))
                {
                    int time;
                    if (int.TryParse(item.Substring(1).ToString(), out time))
                    {
                        return time;
                    }
                }
            }
            return 0;
        }

        public void Postpone(int time)
        {
            int current = GetTime();
            int targetTime = current + time;

            if (Preconditions != null)
            {
                Preconditions.RemoveTime();
                Preconditions.AddTimeV2(targetTime,false);
            }
            if (Effects != null)
            {
                Effects.RemoveTime();
                Effects.AddTimeV2(targetTime,true);
            }
            
            Name = Name.Replace("t" + (current + 1), "t" + (current + 1 + time));
            Name = Name.Replace("t" + (current), "t" + (current + time));

        }

        public List<CompoundFormula> GetConditions()
        {
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            return lConditions;
        }

        public List<Action> SplitConditionalEffects(out CompoundFormula cfObligatory)
        {
            List<Action> lSplit = new List<Action>();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            cfObligatory = new CompoundFormula("and");
            foreach (Formula fSub in lObligatory)
            {
                cfObligatory.AddOperand(fSub);
            }
            int cActions = 1;
            foreach (CompoundFormula cfCondition in lConditions)
            {
                Action a = new Action(Name + cActions);
                cActions++;
                CompoundFormula cfPreconditions = new CompoundFormula("and");
                if( Preconditions != null )
                    cfPreconditions.AddOperand(Preconditions.Clone());
                cfPreconditions.AddOperand(cfCondition.Operands[0]);
                CompoundFormula cfEffects = new CompoundFormula("and");
                cfEffects.AddOperand(cfObligatory.Clone());
                cfEffects.AddOperand(cfCondition.Operands[1]);
                a.Preconditions = cfPreconditions;
                a.Effects = cfEffects;
                if(a.Observe != null)
                    a.Observe = Observe.Clone();
                lSplit.Add(a);
            }
            return lSplit;
        }

        public Formula GetApplicableEffects(IEnumerable<Predicate> lPredicates, bool bContainsNegations)
        {
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach(Formula f in lObligatory)
                cfEffects.SimpleAddOperand(f);
            int iCondition = 0;
            List<Predicate> lEffects = new List<Predicate>();
            foreach(CompoundFormula cfWhen in lConditions)
            {
                if (cfWhen.Operands[0].ContainedIn(lPredicates, bContainsNegations))
                {
                    if (m_mMapConditionsChoices.ContainsKey(iCondition))
                    {
                        if (cfWhen.Operands[1] is CompoundFormula)
                        {
                            //cfEffects.AddOperand(((CompoundFormula)cfWhen.Operands[1]).Operands[m_mMapConditionsChoices[iCondition].First()]);
                            AddPredicatesToEffectList(lEffects,((CompoundFormula)cfWhen.Operands[1]).Operands[m_mMapConditionsChoices[iCondition].First()]);
                        }
                    }
                    else
                    {
                        //cfEffects.AddOperand(cfWhen.Operands[1]);
                        AddPredicatesToEffectList(lEffects, cfWhen.Operands[1]);
                       
                    }
                }

                iCondition++;
            }
            foreach (Predicate p in lEffects)
            {
                cfEffects.AddOperand(p);
            }
            return cfEffects;
        }

        public void ChangeAgent(Constant pastAgent, Constant activeAgent)
        {
            if (pastAgent != activeAgent)
            {
                // Update Action Name
                Name = Name.Replace(pastAgent.ToString(), "aTempAgentName");
                Name = Name.Replace(activeAgent.ToString(), pastAgent.ToString());
                Name = Name.Replace("aTempAgentName", activeAgent.ToString());

                Preconditions.ChangeAgent(pastAgent.ToString(), activeAgent.ToString());
                Effects.ChangeAgent(pastAgent.ToString(), activeAgent.ToString());
            }
            
        }

        public Action RemoveTime()
        {
            Action newAction = Clone();
            //Rename
            string newName = "";
            foreach (var item in Name.Split('_'))
            {
                if (!item.StartsWith("t"))
                {
                    newName += "_" + item;
                }
            }
            newName = newName.Substring(1);
            newAction.Name = newName;

            //Remove from effects
            Formula newEffects = Effects.Clone();
            newEffects.RemoveTime();
            newAction.Effects = newEffects;

            //Remove from preconditions
            Formula newPreconditions = Preconditions.Clone();
            newPreconditions.RemoveTime();
            newAction.Preconditions = newPreconditions;         
            return newAction;
        }

        public void GetApplicableEffects(IEnumerable<Predicate> lPredicates, HashSet<Predicate> lAddEffects, HashSet<Predicate> lRemoveEffects, bool bContainsNegations)
        {
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            foreach (Formula f in lObligatory)
            {
                if (f is CompoundFormula)
                    throw new NotImplementedException();
                else
                {
                    Predicate p = ((PredicateFormula)f).Predicate;
                    if (p.Negation)
                        lRemoveEffects.Add(p);
                    else
                        lAddEffects.Add(p);

                }
            }
            //pretty sure that there is no point in going over the conditionals - 
            //reducing the effects, so every conditional effect that is true, is now mandatory
            return;



            int iCondition = 0;
            foreach (CompoundFormula cfWhen in lConditions)
            {
                if (cfWhen.Operands[0].ContainedIn(lPredicates, bContainsNegations))
                {
                    if (m_mMapConditionsChoices.ContainsKey(iCondition))
                    {
                        if (cfWhen.Operands[1] is CompoundFormula)
                        {
                            //cfEffects.AddOperand(((CompoundFormula)cfWhen.Operands[1]).Operands[m_mMapConditionsChoices[iCondition].First()]);
                            AddPredicatesToEffectList(lAddEffects, lRemoveEffects, ((CompoundFormula)cfWhen.Operands[1]).Operands[m_mMapConditionsChoices[iCondition].First()]);
                        }
                    }
                    else
                    {
                        //cfEffects.AddOperand(cfWhen.Operands[1]);
                        AddPredicatesToEffectList(lAddEffects, lRemoveEffects, cfWhen.Operands[1]);

                    }
                }

                iCondition++;
            }
        }

        private void AddPredicatesToEffectList(List<Predicate> lEffects, Formula f)
        {
            if (f is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)f).Predicate;
                if (!lEffects.Contains(p))
                {
                    if (lEffects.Contains(p.Negate()))
                    {
                        if (!p.Negation)
                        {
                            lEffects.Remove(p.Negate());
                            lEffects.Add(p);
                        }
                    }
                    else
                    {
                        lEffects.Add(p);
                    }
                }
            }
            else
            {
                CompoundFormula cf = (CompoundFormula)f;

                //non deterministic effects
                if (cf.Operator == "oneof" || cf.Operator == "or")
                {
                    int iTrueOption = RandomGenerator.Next(cf.Operands.Count);
                    if (cf.Operator == "oneof")
                    {
                        AddPredicatesToEffectList(lEffects, cf.Operands[iTrueOption]);
                    }
                    else
                    {
                        int iOption = 0;
                        for (iOption = 0; iOption < cf.Operands.Count; iOption++)
                        {
                            if (iOption == iTrueOption || RandomGenerator.NextDouble() < 0.5)
                                AddPredicatesToEffectList(lEffects, cf.Operands[iOption]);
                        }
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula fSub in cf.Operands)
                        AddPredicatesToEffectList(lEffects, fSub);
                }
                else
                    throw new NotImplementedException();
            }
        }

        /*public void ComputeJointAction()
        {
            if (Preconditions != null)
            {
                int preconditionAgents = Preconditions.GetAgents().Length;
                JointAction = preconditionAgents > 1 ? true : false;
            }
        }*/

        public Dictionary<Constant, Action> SplitByAgents(List<Constant> agents)
        {
            Dictionary<Constant, Action> ans = new Dictionary<Constant, Action>();
            foreach (var agent in agents)
            { 
                ans.Add(agent, Clone());
                foreach (var otherAgent in agents)
                {
                    if (otherAgent == agent)
                        continue;
                    ans[agent].RemoveConstant(otherAgent);
                    ans[agent].OriginalActionBeforeSplit = this;                    
                }
            }
            return ans;
        }

        private void AddPredicatesToEffectList(HashSet<Predicate> lAddEffects, HashSet<Predicate> lRemoveEffects, Formula f)
        {
            if (f is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)f).Predicate;
                HashSet<Predicate> lEffects = null;
                if (p.Negation)
                    lEffects = lRemoveEffects;
                else
                    lEffects = lAddEffects;
                if (!lEffects.Contains(p))
                {
                    if (lEffects.Contains(p.Negate()))
                    {
                        if (!p.Negation)
                        {
                            lEffects.Remove(p.Negate());
                            lEffects.Add(p);
                        }
                    }
                    else
                    {
                        lEffects.Add(p);
                    }
                }
            }
            else
            {
                CompoundFormula cf = (CompoundFormula)f;

                //non deterministic effects
                if (cf.Operator == "oneof" || cf.Operator == "or")
                {
                    int iTrueOption = RandomGenerator.Next(cf.Operands.Count);
                    if (cf.Operator == "oneof")
                    {
                        AddPredicatesToEffectList(lAddEffects, lRemoveEffects, cf.Operands[iTrueOption]);
                    }
                    else
                    {
                        int iOption = 0;
                        for (iOption = 0; iOption < cf.Operands.Count; iOption++)
                        {
                            if (iOption == iTrueOption || RandomGenerator.NextDouble() < 0.5)
                                AddPredicatesToEffectList(lAddEffects, lRemoveEffects, cf.Operands[iOption]);
                        }
                    }
                }
                else if (cf.Operator == "and")
                {
                    foreach (Formula fSub in cf.Operands)
                        AddPredicatesToEffectList(lAddEffects, lRemoveEffects, fSub);
                }
                else
                    throw new NotImplementedException();
            }
        }

        public HashSet<Predicate> GetMandatoryEffects()
        {
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            foreach (Formula f in lObligatory)
            {
                foreach (Predicate p in f.GetAllPredicates())
                {
                     lEffects.Add(p);
                }
            }
            return lEffects;
        }

        internal string GetOperationNameByInitials(string[] initials)
        {
            string output = Name.Split('_')[0];
            foreach (var item in Name.Split('_'))
            {
                foreach (var initi in initials)
                {
                    if (item.StartsWith(initi))
                        output += "_" + item;
                }
            }
            return output;
        }

        public SortedSet<Predicate> GetApplicableEffects(IEnumerable<Predicate> lPredicates, bool bContainsNegations, Dictionary<Predicate,Formula> dEffectPreconditions)
        {
            SortedSet<Predicate> lEffects = new SortedSet<Predicate>();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            foreach (Formula f in lObligatory)
            {
                foreach (Predicate p in f.GetAllPredicates())
                {
                    dEffectPreconditions[p] = Preconditions;
                    lEffects.Add(p);
                }
            }
            int iCondition = 0;
            foreach (CompoundFormula cfWhen in lConditions)
            {
                if (cfWhen.Operands[0].ContainedIn(lPredicates, bContainsNegations))
                {
                    
                    foreach (Predicate p in cfWhen.Operands[1].GetAllPredicates())
                    {
                        CompoundFormula cf = new CompoundFormula("and");
                        if(Preconditions != null )
                            cf.AddOperand(Preconditions);
                        cf.AddOperand(cfWhen.Operands[0]);
                        dEffectPreconditions[p] = cf;
                        lEffects.Add(p);
                    }
                }

                iCondition++;
            }
            return lEffects;
        }

        public Action AddKnowledgeConditions(List<string> lAlwaysKnown)
        {
            Action aNew = Clone();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    if(!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                }
                aNew.Preconditions = cfPreconditions;
            }
            HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
                cfEffects.AddOperand(f);
            }
            foreach (Predicate p in lKnowEffects)
            {
                if (!lAlwaysKnown.Contains(p.Name))
                {
                    cfEffects.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                }
            }
            foreach (CompoundFormula cfCondition in lConditions)
            {
                cfEffects.AddOperand(cfCondition);
                cfEffects.AddOperand(CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown));
                cfEffects.AddOperand(CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown));
            }
            aNew.Effects = cfEffects;
            if (Observe != null)
                throw new NotImplementedException();
            return aNew;
        }

        public Action NonConditionalObservationTranslation(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown, bool bTrue)
        {
            Action aNew = Clone();
            if (bTrue)
                aNew.Name += "-T";
            else
                aNew.Name += "-F";
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
            }
            if (bTrue)
                cfPreconditions.AddOperand(pObserve);
            else
                cfPreconditions.AddOperand(pObserve.Negate());

            if (SDRPlanner.SplitConditionalEffects)
                cfPreconditions.AddOperand(new GroundedPredicate("NotInAction"));

            aNew.Preconditions = cfPreconditions;

            if (bTrue)
                aNew.Effects = new PredicateFormula(new KnowPredicate(pObserve));
            else
                aNew.Effects = new PredicateFormula(new KnowPredicate(pObserve.Negate()));

            return aNew;
        }

        public Action KnowWhetherObservationTranslation(Dictionary<string, List<Predicate>> dTags, Domain d)
        {
            Action aNew = Clone();
            aNew.Name = Name + "-KW";
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;

            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    if (!d.AlwaysKnown(p))
                        cfPreconditions.AddOperand(new KnowWhetherPredicate(p));
                    if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                        cfPreconditions.AddOperand(new KnowPredicate(p));
                }
            }
            if (cfPreconditions.Operands.Count > 0)
                aNew.Preconditions = cfPreconditions;
            else
                aNew.Preconditions = null;

            CompoundFormula cfEffects = new CompoundFormula("and");

            foreach (string sTag in dTags.Keys)
            {
                CompoundFormula cfCondition = new CompoundFormula("when");
                CompoundFormula cfAnd = new CompoundFormula("and");
                foreach (Predicate p in lKnowPreconditions)
                {
                    if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                        continue;
                    if (d.AlwaysConstant(p))
                        cfAnd.AddOperand(new KnowPredicate(p));
                    else
                        cfAnd.AddOperand(p.GenerateGiven(sTag));
                }
                cfCondition.AddOperand(cfAnd);
                cfCondition.AddOperand(pObserve.GenerateKnowGiven(sTag, true));//know-whether given
                if (cfAnd.Operands.Count > 0)
                    cfEffects.AddOperand(cfCondition);
                else
                    cfEffects.AddOperand(cfCondition.Operands[1]);

            }

            aNew.Effects = cfEffects;

            return aNew;
        }
        
        public List<Action> TagObservationTranslationNoState(Dictionary<string, List<Predicate>> dTags, Domain d)
        {
            List<Action> lCompiled = new List<Action>();
            if (SDRPlanner.Translation == SDRPlanner.Translations.MPSRTags)
            {
                foreach(string sTag in dTags.Keys)
                    lCompiled.Add(KnowWhetherTagObservationTranslation(dTags, d, sTag));
            }
            if (SDRPlanner.Translation == SDRPlanner.Translations.MPSRTagPartitions)
            {
                List<List<string>[]> lAllPartitions = new List<List<string>[]>();
                GetAllPartitions(new List<string>(dTags.Keys), lAllPartitions);
                foreach (List<string>[] aPartition in lAllPartitions)
                {
                    if (aPartition[0].Count > 1)//there is no point in observing if you already know to distinguish between the current state and everything else.
                        lCompiled.Add(TagObservationTranslationNoState(dTags, d, aPartition[0], aPartition[1]));
                }
            }
            return lCompiled;
        }

        public Action KnowWhetherTagObservationTranslation(Dictionary<string, List<Predicate>> dTags, Domain d, string sActionTag)
        {
            string sName = Name + "-KW-" + sActionTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);
            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                    aNew.AddParameter(p);
            }
            

             if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;
            
            aNew.Preconditions = GetKnowWhetherPreconditions(dTags, d, sActionTag);

            CompoundFormula cfEffects = new CompoundFormula("and");

            foreach (string sTag in dTags.Keys)
            {
                
                if (sTag != sActionTag)
                {
                
                    CompoundFormula cfCondition = new CompoundFormula("when");
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    cfAnd.AddOperand(pObserve.GenerateGiven(sTag));
                    cfAnd.AddOperand(pObserve.GenerateGiven(sActionTag).Negate());
                    cfCondition.AddOperand(cfAnd);

                    Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag), new Constant(Domain.TAG, sActionTag));

                    cfCondition.AddOperand(pNotTag);

                    cfEffects.AddOperand(cfCondition);

                    cfCondition = new CompoundFormula("when");
                    cfAnd = new CompoundFormula("and");
                    cfAnd.AddOperand(pObserve.GenerateGiven(sTag).Negate());
                    cfAnd.AddOperand(pObserve.GenerateGiven(sActionTag));
                    cfCondition.AddOperand(cfAnd);

                    cfCondition.AddOperand(pNotTag);

                    cfEffects.AddOperand(cfCondition);
                }
            }

            aNew.Effects = cfEffects;

            return aNew;
        }


        public Action KnowWhetherTagObservationTranslation(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
        {
            string sName = Name + "-KW";
            foreach (string sTag in lIncludedTags)
                sName += "-" + sTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);
            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                    aNew.AddParameter(p);
            }


            if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;

            aNew.Preconditions = GetKnowWhetherPreconditions(dTags, d, lIncludedTags, lExcludedTags);

            CompoundFormula cfEffects = new CompoundFormula("and");

            foreach (string sTag in lIncludedTags)
            {
                cfEffects.AddOperand(pObserve.GenerateKnowGiven(sTag, true));                
            }

            aNew.Effects = cfEffects;

            return aNew;
        }


        public Action TagObservationTranslationNoState(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
        {
            string sName = Name;
            foreach (string sTag in lIncludedTags)
                sName += "-" + sTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);
            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                    aNew.AddParameter(p);
            }


            if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;

            aNew.Preconditions = GetPreconditionsNoState(dTags, d, lIncludedTags, lExcludedTags);
            ((CompoundFormula)aNew.Preconditions).AddOperand(new GroundedPredicate("NotInAction"));

            CompoundFormula cfEffects = new CompoundFormula("and");
            /*
            foreach (string sTag in lIncludedTags)
            {
                cfEffects.AddOperand(pObserve.GenerateKnowGiven(sTag, true));
            }
            */
            for (int i = 0; i < lIncludedTags.Count - 1; i++)
            {
                for (int j = i + 1; j < lIncludedTags.Count; j++)
                {
                    string sTag1 = lIncludedTags[i], sTag2 = lIncludedTags[j];
                    CompoundFormula cfWhen = new CompoundFormula("when");
                    CompoundFormula cfGiven = new CompoundFormula("and");
                    CompoundFormula cfEffect = new CompoundFormula("and");
                    cfGiven.AddOperand(pObserve.GenerateGiven(sTag1));
                    cfGiven.AddOperand(pObserve.GenerateGiven(sTag2).Negate());

                    Constant pTag1 = new Constant(Domain.TAG, sTag1);
                    Constant pTag2 = new Constant(Domain.TAG, sTag2);
                    Predicate ppKnowNot1Given2 = Predicate.GenerateKNot(pTag1, pTag2);
                    cfEffect.AddOperand(ppKnowNot1Given2);//no need to add the other side because all KNot will enforce t1 < t2

                    cfWhen.SimpleAddOperand(cfGiven);
                    cfWhen.SimpleAddOperand(cfEffect);

                    cfEffects.SimpleAddOperand(cfWhen);

                    cfWhen = new CompoundFormula("when");
                    cfGiven = new CompoundFormula("and");
                    cfGiven.AddOperand(pObserve.GenerateGiven(sTag1).Negate());
                    cfGiven.AddOperand(pObserve.GenerateGiven(sTag2));
                    cfWhen.SimpleAddOperand(cfGiven);
                    cfWhen.SimpleAddOperand(cfEffect);
                    cfEffects.SimpleAddOperand(cfWhen);

                }
            }

            aNew.Effects = cfEffects;

            return aNew;
        }

        public Action KnowObservationTranslation()
        {
            Action aNew = Clone();
            aNew.Name = Name + "-K";
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Observe == null)
                throw new NotImplementedException();
            if (Effects != null)
                throw new NotImplementedException();
            Predicate pObserve = ((PredicateFormula)Observe).Predicate;
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    cfPreconditions.AddOperand(new KnowPredicate(p));
                }
                aNew.Preconditions = cfPreconditions;
            }
            else
                aNew.Preconditions = null;

            aNew.Effects = new PredicateFormula(new KnowWhetherPredicate(pObserve));

            return aNew;
        }

        public Action AddTaggedConditions(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown)
        {
            Action aNew = Clone();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                if (SDRPlanner.SplitConditionalEffects)
                    cfPreconditions.AddOperand(new GroundedPredicate("NotInAction"));

                aNew.Preconditions = cfPreconditions;
            }
            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    cfEffects.AddOperand(f);
                }
                foreach (Predicate p in lKnowEffects)
                {
                    if (!lAlwaysKnown.Contains(p.Name))
                    {
                        Predicate pKEffect = new KnowPredicate(p);
                        cfEffects.AddOperand(pKEffect);
                        pKEffect = new KnowPredicate(p.Negate());
                        cfEffects.AddOperand(pKEffect.Negate());
                        foreach (string sTag in dTags.Keys)
                        {
                            pKEffect = p.GenerateKnowGiven(sTag);
                            cfEffects.AddOperand(pKEffect);
                            pKEffect = p.Negate().GenerateKnowGiven(sTag);
                            cfEffects.AddOperand(pKEffect.Negate());
                        }
                    }
                }
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    cfEffects.AddOperand(cfCondition);
                    CompoundFormula cfK = CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown, false);
                    if (cfK != null)
                        cfEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown);
                    if (cfK != null)
                        cfEffects.AddOperand(cfK);
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedKnowledgeGainCondition(cfCondition, sTag, lAlwaysKnown, false);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                        cfK = CreateTaggedKnowledgeLossCondition(cfCondition, sTag, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                    }
                    
                }
                aNew.Effects = cfEffects;
            }
            if (Observe != null)
            {
                if (aNew.Effects == null)
                    aNew.Effects = new CompoundFormula("and");
                
                Predicate pObserve = ((PredicateFormula)Observe).Predicate;
                CompoundFormula cfWhen = new CompoundFormula("when");
                cfWhen.AddOperand(pObserve);
                cfWhen.AddOperand(new KnowPredicate(pObserve));
                ((CompoundFormula)aNew.Effects).AddOperand(cfWhen);
                cfWhen = new CompoundFormula("when");
                cfWhen.AddOperand(pObserve.Negate());
                cfWhen.AddOperand(new KnowPredicate(pObserve.Negate()));
                ((CompoundFormula)aNew.Effects).AddOperand(cfWhen);
                 
            }
            return aNew;
        }


        public List<Action> KnowCompilationSplitConditions(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown, List<Predicate> lAdditionalPredicates)
        {
            List<Action> lActions = new List<Action>();

            ParametrizedAction aNewAdd = new ParametrizedAction(Name + "-Add");
            ParametrizedAction aNewRemove = new ParametrizedAction(Name + "-Remove");

            ParametrizedAction aNewTranslateAdd = new ParametrizedAction(Name + "-TranslateAdd");
            ParametrizedAction aNewTranslateRemove = new ParametrizedAction(Name + "-TranslateRemove");

            ParameterizedPredicate ppInFirst = new ParameterizedPredicate("P1-" + Name);
            ParameterizedPredicate ppInSecond = new ParameterizedPredicate("P2-" + Name);
            ParameterizedPredicate ppInThird = new ParameterizedPredicate("P3-" + Name);
            GroundedPredicate gpNotInAction = new GroundedPredicate("NotInAction");



            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                {
                    aNewAdd.AddParameter(p);
                    aNewRemove.AddParameter(p);
                    aNewTranslateAdd.AddParameter(p);
                    aNewTranslateRemove.AddParameter(p);

                    ppInFirst.AddParameter(p);
                    ppInSecond.AddParameter(p);
                    ppInThird.AddParameter(p);
                }
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
            }
            cfPreconditions.AddOperand(gpNotInAction);

            if (Effects == null)
                throw new NotImplementedException();

            HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
            CompoundFormula cfAddEffects = new CompoundFormula("and");
            CompoundFormula cfRemoveEffects = new CompoundFormula("and");
            CompoundFormula cfTranslateAddEffects = new CompoundFormula("and");
            CompoundFormula cfTranslateRemoveEffects = new CompoundFormula("and");
            List<Predicate> lRequireTranslation = new List<Predicate>();

            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
                cfAddEffects.AddOperand(f); //unconditional effects cannot conflict anyhow
            }

            foreach (Predicate p in lKnowEffects)
            {
                if (!lAlwaysKnown.Contains(p.Name))
                {
                    Predicate pKEffect = new KnowPredicate(p);
                    cfAddEffects.AddOperand(pKEffect);
                    pKEffect = new KnowPredicate(p.Negate());
                    cfRemoveEffects.AddOperand(pKEffect.Negate());
                    foreach (string sTag in dTags.Keys)
                    {
                        pKEffect = p.GenerateKnowGiven(sTag);
                        cfAddEffects.AddOperand(pKEffect);
                        pKEffect = p.Negate().GenerateKnowGiven(sTag);
                        cfRemoveEffects.AddOperand(pKEffect.Negate());
                    }
                }
            }
            if (lConditions.Count > 0)
            {
                lAdditionalPredicates.Add(ppInFirst);
                lAdditionalPredicates.Add(ppInSecond);
                lAdditionalPredicates.Add(ppInThird);

                aNewRemove.Preconditions = cfPreconditions;
                cfRemoveEffects.AddOperand(ppInFirst);
                cfRemoveEffects.AddOperand(gpNotInAction.Negate());

                aNewAdd.Preconditions = new PredicateFormula(ppInFirst);
                cfAddEffects.AddOperand(ppInSecond);
                cfAddEffects.AddOperand(ppInFirst.Negate());

                aNewTranslateRemove.Preconditions = new PredicateFormula(ppInSecond);
                cfTranslateRemoveEffects.AddOperand(ppInSecond.Negate());
                cfTranslateRemoveEffects.AddOperand(ppInThird);

                aNewTranslateAdd.Preconditions = new PredicateFormula(ppInThird);
                cfTranslateAddEffects.AddOperand(ppInThird.Negate());
                cfTranslateAddEffects.AddOperand(gpNotInAction);

                Dictionary<Predicate, Predicate> dTaggedPredicates = new Dictionary<Predicate,Predicate>();

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfAddCondition, cfRemoveCondition;
                    cfCondition.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                    if (cfAddCondition != null)
                        cfAddEffects.AddOperand(cfAddCondition);
                    if (cfRemoveCondition != null)
                        cfRemoveEffects.AddOperand(cfRemoveCondition);


                    CompoundFormula cfK = CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown, false);
                    if (cfK != null)
                    {
                        cfK.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                        if (cfAddCondition != null)
                            cfAddEffects.AddOperand(cfAddCondition);
                        if (cfRemoveCondition != null)
                            cfRemoveEffects.AddOperand(cfRemoveCondition);
                    }

                    cfK = CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown);
                    if (cfK != null)
                    {
                        cfK.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                        if (cfAddCondition != null)
                            cfAddEffects.AddOperand(cfAddCondition);
                        if (cfRemoveCondition != null)
                            cfRemoveEffects.AddOperand(cfRemoveCondition);
                    }
                    
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedKnowledgeGainCondition(cfCondition, sTag, lAlwaysKnown, false);
                        if (cfK != null)
                        {
                            cfK.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                            if (cfAddCondition != null)
                                cfAddEffects.AddOperand(cfAddCondition);
                            if (cfRemoveCondition != null)
                                cfRemoveEffects.AddOperand(cfRemoveCondition);
                        }
                        cfK = CreateTaggedKnowledgeLossCondition(cfCondition, sTag, lAlwaysKnown);
                        if (cfK != null)
                        {
                            cfK.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                            if (cfAddCondition != null)
                                cfAddEffects.AddOperand(cfAddCondition);
                            if (cfRemoveCondition != null)
                                cfRemoveEffects.AddOperand(cfRemoveCondition);
                        }
                    }

                }
                aNewAdd.Effects = cfAddEffects.Simplify();
                aNewRemove.Effects = cfRemoveEffects.Simplify();
                lActions.Add(aNewRemove);
                lActions.Add(aNewAdd);

                foreach (KeyValuePair<Predicate, Predicate> pair in dTaggedPredicates)
                {
                    CompoundFormula cfWhen = new CompoundFormula("when");
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    cfWhen.AddOperand(pair.Key);

                    cfAnd.SimpleAddOperand(pair.Value);
                    cfAnd.SimpleAddOperand(pair.Key.Negate());
                    cfWhen.SimpleAddOperand(cfAnd);

                    if (pair.Value.Negation)
                        cfTranslateRemoveEffects.AddOperand(cfWhen);
                    else
                        cfTranslateAddEffects.AddOperand(cfWhen);
                }

                aNewTranslateAdd.Effects = cfTranslateAddEffects;
                aNewTranslateRemove.Effects = cfTranslateRemoveEffects;
                lActions.Add(aNewTranslateRemove);
                lActions.Add(aNewTranslateAdd);
            }
            else
            {
                Action aK = AddTaggedConditions(dTags, lAlwaysKnown);
                lActions.Add(aK);
            }

            if (Observe != null)
            {
               throw new NotImplementedException();

            }
            return lActions;
        }

        /*
        public List<Action> KnowCompilationSplitConditions(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown, List<Predicate> lAdditionalPredicates)
        {
            List<Action> lActions = new List<Action>();

            ParametrizedAction aNewState = new ParametrizedAction(Name + "-State");
            ParametrizedAction aNewKnowledgeGain = new ParametrizedAction(Name + "-KnowledgeGain");
            ParametrizedAction aNewKnowledgeLoss = new ParametrizedAction(Name + "-KnowledgeLoss");

            ParameterizedPredicate ppInFirst = new ParameterizedPredicate("P1-" + Name);
            ParameterizedPredicate ppInSecond = new ParameterizedPredicate("P2-" + Name);
            GroundedPredicate gpNotInAction = new GroundedPredicate("NotInAction");

            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                {
                    aNewKnowledgeLoss.AddParameter(p);
                    aNewKnowledgeGain.AddParameter(p);
                    aNewState.AddParameter(p);
                    ppInFirst.AddParameter(p);
                    ppInSecond.AddParameter(p);
                }
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            List<Predicate> lKnowPreconditions = new List<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
            }
            cfPreconditions.AddOperand(gpNotInAction);
            aNewKnowledgeGain.Preconditions = cfPreconditions;//knowledge gain is the first action, so it will have all the preconditions

            if (Effects == null)
                throw new NotImplementedException();

            List<Predicate> lKnowEffects = new List<Predicate>();
            CompoundFormula cfStateEffects = new CompoundFormula("and");
            CompoundFormula cfKnowledgeLossEffects = new CompoundFormula("and");
            CompoundFormula cfKnowledgeGainEffects = new CompoundFormula("and");

            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
                cfStateEffects.AddOperand(f);
            }

            foreach (Predicate p in lKnowEffects)
            {
                if (!lAlwaysKnown.Contains(p.Name))
                {
                    Predicate pKEffect = new KnowPredicate(p);
                    cfKnowledgeGainEffects.AddOperand(pKEffect);
                    pKEffect = new KnowPredicate(p.Negate());
                    cfKnowledgeGainEffects.AddOperand(pKEffect.Negate());
                    foreach (string sTag in dTags.Keys)
                    {
                        pKEffect = p.GenerateKnowGiven(sTag);
                        cfKnowledgeGainEffects.AddOperand(pKEffect);
                        pKEffect = p.Negate().GenerateKnowGiven(sTag);
                        cfKnowledgeGainEffects.AddOperand(pKEffect.Negate());
                    }
                }
            }
            if (lConditions.Count > 0)
            {
                lAdditionalPredicates.Add(ppInFirst);
                lAdditionalPredicates.Add(ppInSecond);

                aNewKnowledgeGain.Preconditions = cfPreconditions;
                aNewKnowledgeLoss.Preconditions = new PredicateFormula(ppInFirst);
                aNewState.Preconditions = new PredicateFormula(ppInSecond);

                cfKnowledgeGainEffects.AddOperand(ppInFirst);
                cfKnowledgeGainEffects.AddOperand(gpNotInAction.Negate());

                cfKnowledgeLossEffects.AddOperand(ppInSecond);
                cfKnowledgeLossEffects.AddOperand(ppInFirst.Negate());

                cfStateEffects.AddOperand(ppInSecond.Negate());
                cfStateEffects.AddOperand(gpNotInAction);

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    cfStateEffects.AddOperand(cfCondition);
                    CompoundFormula cfK = CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown, false);
                    if (cfK != null)
                        cfKnowledgeGainEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown);
                    if (cfK != null)
                        cfKnowledgeLossEffects.AddOperand(cfK);
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedKnowledgeGainCondition(cfCondition, sTag, lAlwaysKnown, false);
                        if (cfK != null)
                            cfKnowledgeGainEffects.AddOperand(cfK);
                        cfK = CreateTaggedKnowledgeLossCondition(cfCondition, sTag, lAlwaysKnown);
                        if (cfK != null)
                            cfKnowledgeLossEffects.AddOperand(cfK);
                    }

                }
                aNewKnowledgeGain.Effects = cfKnowledgeGainEffects.Simplify();
                aNewKnowledgeLoss.Effects = cfKnowledgeLossEffects.Simplify();
                lActions.Add(aNewKnowledgeLoss);
                lActions.Add(aNewKnowledgeGain);
            }
            else
            {
                aNewState.Preconditions = cfPreconditions;
            }
            aNewState.Effects = cfStateEffects.Simplify();
            lActions.Add(aNewState);

            if (Observe != null)
            {
                throw new NotImplementedException();

            }
            return lActions;
        }
        */
        
        public Action KnowCompilation(Dictionary<string, List<Predicate>> dTags, Domain d)
        {
            Action aNew = Clone();
            aNew.Name = Name + "-K";
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    cfPreconditions.AddOperand(new KnowPredicate(p));
                }
                aNew.Preconditions = cfPreconditions;
            }
            else
                aNew.Preconditions = null;
            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    //cfEffects.AddOperand(f);//BGUBGU - probably a bug here. Need to separate always known and the rest.
                }
                foreach (Predicate p in lKnowEffects)
                {
                    
                    Predicate pKEffect = new KnowPredicate(p);
                    cfEffects.AddOperand(pKEffect);
                    Predicate pKNegateEffect = new KnowPredicate(p.Negate()).Negate();
                    cfEffects.AddOperand(pKNegateEffect);
                    /* why do we need all this?
                    pKEffect = new KnowPredicate(p.Negate());
                    cfEffects.AddOperand(pKEffect.Negate());
                    foreach (string sTag in dTags.Keys)
                    {
                        pKEffect = p.GenerateGiven(sTag);
                        cfEffects.AddOperand(pKEffect);
                        pKEffect = p.Negate().GenerateGiven(sTag);
                        cfEffects.AddOperand(pKEffect.Negate());
                    }
                        */                   
                }
                foreach (string sTag in dTags.Keys)
                {
                    //e|s
                    CompoundFormula cfKEffects = new CompoundFormula("and");
                    foreach (Predicate p in lKnowEffects)
                    {
                        Predicate pAdd = p.GenerateGiven(sTag);
                        cfKEffects.AddOperand(pAdd);
                    }
                    cfEffects.SimpleAddOperand(cfKEffects);
                }
                

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    //cfEffects.AddOperand(cfCondition);//no longer valid? Perhaps needed if there are some "always known" conditions?
                    CompoundFormula cfK = CreateKnowledgeGainCondition(cfCondition, d.m_lAlwaysKnown, false);
                    if (cfK != null)
                        cfEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, d.m_lAlwaysKnown, false);
                    if (cfK != null)
                    {
                        if(cfK.Operator == "and" || cfK.Operands[0] is PredicateFormula ||
                            cfK.Operands[0] is CompoundFormula && (((CompoundFormula)cfK.Operands[0]).Operands.Count > 0))
                            cfEffects.SimpleAddOperand(cfK);
                    }
                    //cfK = CreateKnowledgeGainCondition(cfCondition, d.m_lAlwaysKnown, true);
                    //if (cfK != null)
                    //    cfEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, d.m_lAlwaysKnown, true);
                    if (cfK != null)
                    {
                        if (cfK.Operator == "and" || cfK.Operands[0] is PredicateFormula ||
                            cfK.Operands[0] is CompoundFormula && (((CompoundFormula)cfK.Operands[0]).Operands.Count > 0))
                            cfEffects.SimpleAddOperand(cfK);
                    }
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        cfEffects.AddOperand(cfK);
                        cfK = CreateTaggedKnowledgeWhetherGainCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfEffects.SimpleAddOperand(cfK);
                        }
                        cfK = CreateTaggedKnowledgeWhetherLossCondition(cfCondition, d, sTag);
                        if (cfK != null)
                            cfEffects.SimpleAddOperand(cfK);
                    }

                }
                aNew.Effects = cfEffects.Simplify();
            }
            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return aNew;
        }


        public Action KnowWhetherCompilation(Dictionary<string, List<Predicate>> dTags, Domain d)
        {
            Action aNew = Clone();
            aNew.Name = Name + "-KW";
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfKWPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    if(!d.AlwaysKnown(p))
                        cfKWPreconditions.AddOperand(new KnowWhetherPredicate(p));
                    if(d.AlwaysKnown(p) && d.AlwaysConstant(p))
                        cfKWPreconditions.AddOperand(new KnowPredicate(p));
                }
                if (cfKWPreconditions.Operands.Count > 0)
                    aNew.Preconditions = cfKWPreconditions;
                else
                    aNew.Preconditions = null;
            }
            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                CompoundFormula cfMandatoryEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    //cfEffects.AddOperand(f);//BGUBGU - probably a bug here. Need to separate always known and the rest.
                }
                if (lKnowEffects.Count > 0)
                {
                    foreach (string sTag in dTags.Keys)
                    {
                        //K(preconditions|s)->K(effects|s) 
                        CompoundFormula cfKEffects = new CompoundFormula("and");
                        CompoundFormula cfKPreconditions = new CompoundFormula("and");
                        foreach (Predicate p in lKnowPreconditions)
                        {
                            if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                                continue;
                            else
                                cfKPreconditions.AddOperand(p.GenerateGiven(sTag));
                        }
                        foreach (Predicate p in lKnowEffects)
                        {
                            Predicate pAdd = p.GenerateGiven(sTag);
                            cfKEffects.AddOperand(pAdd);
                            //Predicate pDelete = p.Negate().GenerateKnowGiven(sTag).Negate();
                            //cfKEffects.AddOperand(pDelete);
                        }
                        if (cfKPreconditions.Operands.Count > 0)
                        {
                            CompoundFormula cfCondition = new CompoundFormula("when");
                            cfCondition.AddOperand(cfKPreconditions);
                            cfCondition.AddOperand(cfKEffects);
                            cfEffects.AddOperand(cfCondition);
                        }
                        else
                            cfEffects.AddOperand(cfKEffects);
                    }
                }
                //forgetting: ~K~p
                foreach (Predicate p in lKnowEffects)
                {
                    Predicate pKNotp = new KnowPredicate(p.Negate());
                    cfEffects.AddOperand(pKNotp.Negate());
                }                
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfK = null, cfOr = null, cfAnd = null;
                    //cfK = CreateKnowledgeGainCondition(cfCondition, d.m_lAlwaysKnown, false);
                    //if (cfK != null)
                    //    cfEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, d.m_lAlwaysKnown, false);
                    if (cfK != null)
                    {
                        cfOr = new CompoundFormula("or");
                        foreach (Predicate p in lKnowPreconditions)
                        {
                            Predicate pKNot = new KnowPredicate(p.Negate());
                            cfOr.AddOperand(pKNot.Negate());
                        }
                        if (cfK.Operator == "when")
                        {
                            if (cfK.Operands[0] is CompoundFormula && ((CompoundFormula)cfK.Operands[0]).Operands.Count > 0)
                                cfOr.AddOperand(cfK.Operands[0]);
                            cfK.Operands[0] = cfOr.Simplify();
                        }
                        else
                        {
                            CompoundFormula cfWhen = new CompoundFormula("when");
                            cfWhen.AddOperand(cfOr.Simplify());
                            cfWhen.AddOperand(cfK);
                            cfK = cfWhen;
                        }
                        cfEffects.AddOperand(cfK);
                    }
                    //cfK = CreateKnowledgeGainCondition(cfCondition, d.m_lAlwaysKnown, true);
                    //if (cfK != null)
                    //    cfEffects.AddOperand(cfK);
                    cfK = CreateKnowledgeLossCondition(cfCondition, d.m_lAlwaysKnown, true);
                    if (cfK != null)
                    {
                        cfOr = new CompoundFormula("or");
                        foreach (Predicate p in lKnowPreconditions)
                        {
                            Predicate pKNot = new KnowPredicate(p.Negate());
                            cfOr.AddOperand(pKNot.Negate());
                        }
                        if (cfK.Operator == "when")
                        {
                            if (cfK.Operands[0] is PredicateFormula || ((CompoundFormula)cfK.Operands[0]).Operands.Count > 0)
                                cfOr.AddOperand(cfK.Operands[0]);
                            cfK.Operands[0] = cfOr.Simplify();
                        }
                        else
                        {
                            CompoundFormula cfWhen = new CompoundFormula("when");
                            cfWhen.AddOperand(cfOr.Simplify());
                            cfWhen.AddOperand(cfK);
                            cfK = cfWhen;
                        }
                        cfEffects.AddOperand(cfK);
                    }
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfAnd = new CompoundFormula("and");
                            foreach (Predicate p in lKnowPreconditions)
                            {
                                if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                                    cfAnd.AddOperand(new KnowPredicate(p));
                                else
                                    cfAnd.AddOperand(p.GenerateGiven(sTag));
                            }
                            if (cfK.Operator == "when")
                            {
                                cfAnd.AddOperand(cfK.Operands[0]);
                                cfK.Operands[0] = cfAnd;
                                cfEffects.AddOperand(cfK);
                            }
                            else
                                throw new NotImplementedException();
                        }
                        
                        cfK = CreateTaggedKnowledgeWhetherGainCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfAnd = new CompoundFormula("and");
                            foreach (Predicate p in lKnowPreconditions)
                            {
                                if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                                    cfAnd.AddOperand(new KnowPredicate(p));
                                else
                                    cfAnd.AddOperand(p.GenerateGiven(sTag));
                            }
                            if (cfK.Operator == "when")
                            {
                                cfAnd.AddOperand(cfK.Operands[0]);
                                cfK.Operands[0] = cfAnd;
                                cfEffects.AddOperand(cfK);
                            }
                            else
                                throw new NotImplementedException();
                        } 
                         
                        cfK = CreateTaggedKnowledgeWhetherLossCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfOr = new CompoundFormula("or");
                            foreach (Predicate p in lKnowPreconditions)
                            {
                                Predicate pKNot = new KnowPredicate(p.Negate());
                                cfOr.AddOperand(pKNot.Negate());
                            }
                            if (cfK.Operator == "when")
                            {
                                if (cfK.Operands[0] is PredicateFormula || ((CompoundFormula)cfK.Operands[0]).Operands.Count > 0)
                                    cfOr.AddOperand(cfK.Operands[0]);
                                cfK.Operands[0] = cfOr.Simplify();
                            }
                            else
                            {
                                CompoundFormula cfWhen = new CompoundFormula("when");
                                cfWhen.AddOperand(cfOr.Simplify());
                                cfWhen.AddOperand(cfK);
                                cfK = cfWhen;
                            }
                        }
                    }

                }
                aNew.Effects = cfEffects.Simplify();
            }
            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return aNew;
        }

        public CompoundFormula GetKnowWhetherPreconditions(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
        {
            CompoundFormula cfKWPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                //foreach tag t, either KNot t | ?t, or forall precondition p, p|t
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                        cfKWPreconditions.AddOperand(new KnowPredicate(p));
                }

                foreach (string sTag in lIncludedTags)
                {

                    CompoundFormula cfAnd = new CompoundFormula("and");
                    foreach (Predicate p in lKnowPreconditions)
                    {
                        if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                            continue;
                        else
                        {
                            cfAnd.AddOperand(p.GenerateGiven(sTag));
                            if (!d.AlwaysKnown(p))
                                cfAnd.AddOperand(p.GenerateKnowGiven(sTag, true));
                        }
                    }
                    if (cfAnd.Operands.Count > 0)
                        cfKWPreconditions.SimpleAddOperand(cfAnd.Simplify());
                    
                    //this allows only actions on non-distinguishable tag sets - it is possible to allow actions that apply to distinguishable tag sets
                    if (sTag != lIncludedTags[0])
                    {
                        Predicate pKNotT = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, lIncludedTags[0]));
                        cfKWPreconditions.AddOperand(pKNotT.Negate());
                    }
                    
                }
            }
            foreach (string sTag in lExcludedTags)
            {

                Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, lIncludedTags[0]));

                cfKWPreconditions.AddOperand(pNotTag);
            }
            if(cfKWPreconditions.Operands.Count > 0)   
                return cfKWPreconditions;
            return null;
        }

        //this implementation requires ~Knot between all include tags, and Knot between every include and every exclude tags
        public CompoundFormula GetPreconditionsNoState(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
        {
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            if (Preconditions != null)
            {

                HashSet<Predicate> lKnowPreconditions = Preconditions.GetAllPredicates();
                foreach (Predicate p in lKnowPreconditions)
                {
                    if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                        cfPreconditions.AddOperand(p);
                }

                foreach (string sTag in lIncludedTags)
                {

                    CompoundFormula cfAnd = new CompoundFormula("and");
                    foreach (Predicate p in lKnowPreconditions)
                    {
                        if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                            continue;
                        else
                        {
                            cfAnd.AddOperand(p.GenerateGiven(sTag));
                        }
                    }
                    if (cfAnd.Operands.Count > 0)
                        cfPreconditions.SimpleAddOperand(cfAnd.Simplify());


                }
            }
            //this allows only actions on non-distinguishable tag sets - it is possible to allow actions that apply to distinguishable tag sets
            for (int iIncludeTag = 0; iIncludeTag < lIncludedTags.Count; iIncludeTag++)
            {
                for (int iOtherIncludeTag = iIncludeTag + 1; iOtherIncludeTag < lIncludedTags.Count; iOtherIncludeTag++)
                {
                    Predicate pKNotT = Predicate.GenerateKNot(new Constant(Domain.TAG, lIncludedTags[iIncludeTag]), new Constant(Domain.TAG, lIncludedTags[iOtherIncludeTag]));
                    cfPreconditions.AddOperand(pKNotT.Negate());

                }
            }
            foreach (string sIncludeTag in lIncludedTags)
            {
                foreach (string sExcludeTag in lExcludedTags)
                {

                    Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sIncludeTag), new Constant(Domain.TAG, sExcludeTag));

                    cfPreconditions.AddOperand(pNotTag);
                }
            }
            return cfPreconditions;
        }
        /*
         *
                public CompoundFormula GetPreconditionsNoState(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
                {
                    CompoundFormula cfPreconditions = new CompoundFormula("and");
                    HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
                    if (Preconditions != null)
                    {
                        //foreach tag t, either KNot t | ?t, or forall precondition p, p|t
                        Preconditions.GetAllPredicates(lKnowPreconditions);
                        foreach (Predicate p in lKnowPreconditions)
                        {
                            if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                                cfPreconditions.AddOperand(p);
                        }

                        foreach (string sTag in lIncludedTags)
                        {

                            CompoundFormula cfAnd = new CompoundFormula("and");
                            foreach (Predicate p in lKnowPreconditions)
                            {
                                if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                                    continue;
                                else
                                {
                                    cfAnd.AddOperand(p.GenerateGiven(sTag));
                                }
                            }
                            if (cfAnd.Operands.Count > 0)
                                cfPreconditions.SimpleAddOperand(cfAnd.Simplify());


                        }
                    }
                    foreach (string sTag in lIncludedTags)
                    {
                        //this allows only actions on non-distinguishable tag sets - it is possible to allow actions that apply to distinguishable tag sets
                        if (sTag != lIncludedTags[0])
                        {
                            Predicate pKNotT = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, lIncludedTags[0]));
                            cfPreconditions.AddOperand(pKNotT.Negate());
                        }
                    }
                    foreach (string sTag in lExcludedTags)
                    {

                        Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, lIncludedTags[0]));

                        cfPreconditions.AddOperand(pNotTag);
                    }
                    //if (cfPreconditions.Operands.Count > 0)
                        return cfPreconditions;
                    //return null;
                }


        */
        public CompoundFormula GetKnowWhetherPreconditions(Dictionary<string, List<Predicate>> dTags, Domain d, string sActionTag)
        {
            Argument pTag = new Constant(Domain.TAG, sActionTag);
            CompoundFormula cfKWPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            CompoundFormula cfOr = null;
            if (Preconditions != null)
            {
                //foreach tag t, either KNot t | ?t, or forall precondition p, p|t
                Preconditions.GetAllPredicates(lKnowPreconditions);
                foreach (Predicate p in lKnowPreconditions)
                {
                    if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                        cfKWPreconditions.AddOperand(p);
                }

                foreach (string sTag in dTags.Keys)
                {
                    Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag), (Constant)pTag);
                    cfOr = new CompoundFormula("or");
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    cfAnd.AddOperand(pNotTag.Negate());
                    foreach (Predicate p in lKnowPreconditions)
                    {
                        //if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                        if (d.AlwaysKnown(p) && (d.AlwaysConstant(p)))
                            continue;
                        else
                        {
                            cfAnd.AddOperand(p.GenerateGiven(sTag));
                            //if (!d.AlwaysKnown(p))
                            //    cfAnd.AddOperand(p.GenerateKnowGiven(sTag, true));
                        }
                    }
                    
                    if (cfAnd.Operands.Count > 0)
                        cfOr.AddOperand(cfAnd);
                    if (sTag == sActionTag)
                    {
                        cfKWPreconditions.SimpleAddOperand(cfAnd);
                    }
                    else
                    {
                        cfOr.AddOperand(pNotTag);

                        cfKWPreconditions.SimpleAddOperand(cfOr.Simplify());
                    }
                }
            }

            if (cfKWPreconditions.Operands.Count > 0)
                return cfKWPreconditions;
            return null;
        }

        public List<Action> KnowWhetherTagCompilation(Dictionary<string, List<Predicate>> dTags, Domain d)
        {
            List<Action> lCompiled = new List<Action>();
            /*
            foreach(string sTag in dTags.Keys)
                lCompiled.Add(KnowWhetherTagCompilation(dTags, d, sTag));
             */
            List<List<string>[]> lAllPartitions = new List<List<string>[]>();
            GetAllPartitions(new List<string>(dTags.Keys), lAllPartitions);
            foreach (List<string>[] aPartition in lAllPartitions)
            {
                lCompiled.Add(KnowWhetherTagCompilation(dTags, d, aPartition[0], aPartition[1]));
            }
            return lCompiled;
        }
        public List<Action> KnowWhetherTagCompilation(Dictionary<string, List<Predicate>> dTags, Domain d, List<Predicate> lAdditionalPredicates)
        {
            List<Action> lCompiled = new List<Action>();
            /*
            foreach(string sTag in dTags.Keys)
                lCompiled.Add(KnowWhetherTagCompilation(dTags, d, sTag));
             */
            List<List<string>[]> lAllPartitions = new List<List<string>[]>();
            GetAllPartitions(new List<string>(dTags.Keys), lAllPartitions);
            foreach (List<string>[] aPartition in lAllPartitions)
            {
                lCompiled.AddRange(KnowWhetherTagCompilationSplitConditions(dTags, d, aPartition[0], aPartition[1], lAdditionalPredicates));
            }
            return lCompiled;
        }
        public List<Action> TagCompilationNoState(Dictionary<string, List<Predicate>> dTags, Domain d, List<Predicate> lAdditionalPredicates)
        {
            List<Action> lCompiled = new List<Action>();
            if (SDRPlanner.Translation == SDRPlanner.Translations.MPSRTags)
            {

                foreach (string sTag in dTags.Keys)
                    lCompiled.Add(KnowWhetherTagCompilation(dTags, d, sTag));

            }
            if (SDRPlanner.Translation == SDRPlanner.Translations.MPSRTagPartitions)
            {
                List<List<string>[]> lAllPartitions = new List<List<string>[]>();
                GetAllPartitions(new List<string>(dTags.Keys), lAllPartitions);
                foreach (List<string>[] aPartition in lAllPartitions)
                {
                    lCompiled.AddRange(TagCompilationSplitConditionsNoState(dTags, d, aPartition[0], aPartition[1], lAdditionalPredicates));
                }
            }
            return lCompiled;
        }

        public static void GetAllPartitions(List<string> lItems, List<List<string>[]> lAllPartitions)
        {
            GetAllPartitions(lItems, lAllPartitions, new List<string>(), new List<string>(), 0);
        }

        private static void GetAllPartitions(List<string> lItems, List<List<string>[]> lAllPartitions, List<string> lFirst, List<string> lSecond, int iCurrent)
        {
            if (iCurrent == lItems.Count)
            {
                if (lFirst.Count > 0) // not interested in empty inclusion lists
                    lAllPartitions.Add(new List<string>[] { lFirst, lSecond });
            }
            else
            {
                List<string> lNewFirst = new List<string>(lFirst);
                List<string> lNewSecond = new List<string>(lSecond);
                lNewFirst.Add(lItems[iCurrent]);
                lNewSecond.Add(lItems[iCurrent]);
                GetAllPartitions(lItems, lAllPartitions, lNewFirst, lSecond, iCurrent + 1);
                GetAllPartitions(lItems, lAllPartitions, lFirst, lNewSecond, iCurrent + 1);
            }
        }

        
        public List<Action> KnowWhetherTagCompilationSplitConditions(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, 
            List<string> lExcludedTags, List<Predicate> lAdditionalPredicates)
        {
            string sName = Name + "-KW";
            foreach (string sTag in lIncludedTags)
                sName += "-" + sTag;
            ParametrizedAction aNewState = new ParametrizedAction(sName + "-State");
            ParametrizedAction aNewKnowledgeGain = new ParametrizedAction(sName + "-KnowledgeGain");
            ParametrizedAction aNewKnowledgeLoss = new ParametrizedAction(sName + "-KnowledgeLoss");

            ParameterizedPredicate ppInFirst = new ParameterizedPredicate("P1-" + sName);
            ParameterizedPredicate ppInSecond = new ParameterizedPredicate("P2-" + sName);
            GroundedPredicate gpNotInAction = new GroundedPredicate("NotInAction");

            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                {
                    aNewKnowledgeLoss.AddParameter(p);
                    aNewKnowledgeGain.AddParameter(p);
                    aNewState.AddParameter(p);
                    ppInFirst.AddParameter(p);
                    ppInSecond.AddParameter(p);
                }
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            CompoundFormula cfPreconditions = new CompoundFormula("and");
            Formula cfKWPreconditions = GetKnowWhetherPreconditions(dTags, d, lIncludedTags, lExcludedTags);
            cfPreconditions.AddOperand(cfKWPreconditions); //knowledge loss is the first action, so it will have all the preconditions
            cfPreconditions.AddOperand(gpNotInAction);

            if (Effects == null)
                throw new NotImplementedException();


            HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
            CompoundFormula cfStateEffects = new CompoundFormula("and");
            CompoundFormula cfKnowledgeLossEffects = new CompoundFormula("and");
            CompoundFormula cfKnowledgeGainEffects = new CompoundFormula("and");
            //CompoundFormula cfMandatoryEffects = new CompoundFormula("and");
            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
            }
            if (lKnowEffects.Count > 0)
            {
                foreach (string sTag in lIncludedTags)
                {
                    //~KNot t|?t -> effects|t
                    CompoundFormula cfKEffects = new CompoundFormula("and");

                    foreach (Predicate p in lKnowEffects)
                    {
                        Predicate pAdd = p.GenerateGiven(sTag);
                        cfKEffects.AddOperand(pAdd);
                        if (!d.AlwaysKnown(p))
                        {
                            pAdd = p.GenerateKnowGiven(sTag, true);
                            cfKEffects.AddOperand(pAdd);
                        }
                    }
                    cfStateEffects.SimpleAddOperand(cfKEffects);
                }
            }

            List<Action> lActions = new List<Action>();

            if (lConditions.Count > 0)
            {
                lAdditionalPredicates.Add(ppInFirst);
                lAdditionalPredicates.Add(ppInSecond);

                aNewKnowledgeLoss.Preconditions = cfPreconditions;
                aNewKnowledgeGain.Preconditions = new PredicateFormula(ppInFirst);
                aNewState.Preconditions = new PredicateFormula(ppInSecond);

                cfKnowledgeLossEffects.AddOperand(ppInFirst);
                cfKnowledgeLossEffects.AddOperand(gpNotInAction.Negate());

                cfKnowledgeGainEffects.AddOperand(ppInSecond);
                cfKnowledgeGainEffects.AddOperand(ppInFirst.Negate());

                cfStateEffects.AddOperand(ppInSecond.Negate());
                cfStateEffects.AddOperand(gpNotInAction);

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfK = null, cfAnd = null;
                    HashSet<Predicate> lConditionEffects = cfCondition.Operands[1].GetAllPredicates();
                    cfAnd = new CompoundFormula("and");

                    foreach (string sTag in lIncludedTags)
                    {
                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfStateEffects.SimpleAddOperand(cfK);
                        }
                    }

                    
                    cfK = CreateTaggedKnowledgeWhetherGainConditions(cfCondition, d, lIncludedTags);
                    if (cfK != null)
                    {
                        cfKnowledgeGainEffects.SimpleAddOperand(cfK);
                    }

                    cfK = CreateTaggedKnowledgeWhetherLossCondition(cfCondition, d, lIncludedTags);
                    if (cfK != null && cfK.Operands.Count > 0)
                    {
                        cfKnowledgeLossEffects.SimpleAddOperand(cfK);
                    }
                }
                aNewKnowledgeGain.Effects = cfKnowledgeGainEffects.Simplify();
                aNewKnowledgeLoss.Effects = cfKnowledgeLossEffects.Simplify();
                lActions.Add(aNewKnowledgeLoss);
                lActions.Add(aNewKnowledgeGain);
            }
            else
            {
                aNewState.Preconditions = cfPreconditions;

            }
            aNewState.Effects = cfStateEffects.Simplify();
            lActions.Add(aNewState);


            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return lActions;
        }


        public List<Action> SplitConditions(List<Predicate> lAdditionalPredicates)
        {
            List<Action> lActions = new List<Action>();

            ParametrizedAction aNewAdd = new ParametrizedAction(Name + "-Add");
            ParametrizedAction aNewRemove = new ParametrizedAction(Name + "-Remove");

            ParametrizedAction aNewTranslateRemove = new ParametrizedAction(Name + "-TranslateRemove");
            ParametrizedAction aNewTranslateAdd = new ParametrizedAction(Name + "-TranslateAdd");

            ParameterizedPredicate ppInFirst = new ParameterizedPredicate("P1-" + Name);
            ParameterizedPredicate ppInSecond = new ParameterizedPredicate("P2-" + Name);
            ParameterizedPredicate ppInThird = new ParameterizedPredicate("P3-" + Name);
            GroundedPredicate gpNotInAction = new GroundedPredicate("NotInAction");



            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                {
                    aNewAdd.AddParameter(p);
                    aNewRemove.AddParameter(p);
                    aNewTranslateAdd.AddParameter(p);
                    aNewTranslateRemove.AddParameter(p);

                    ppInFirst.AddParameter(p);
                    ppInSecond.AddParameter(p);
                    ppInThird.AddParameter(p);
                }
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            cfPreconditions.AddOperand(Preconditions);
            cfPreconditions.AddOperand(gpNotInAction);

            if (Effects == null)
                throw new NotImplementedException();

            HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
            CompoundFormula cfAddEffects = new CompoundFormula("and");
            CompoundFormula cfRemoveEffects = new CompoundFormula("and");
            CompoundFormula cfTranslateAddEffects = new CompoundFormula("and");
            CompoundFormula cfTranslateRemoveEffects = new CompoundFormula("and");
            List<Predicate> lRequireTranslation = new List<Predicate>();

            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
                cfAddEffects.AddOperand(f); //unconditional effects cannot conflict anyhow
            }


            if (lConditions.Count > 0)
            {
                lAdditionalPredicates.Add(ppInFirst);
                lAdditionalPredicates.Add(ppInSecond);
                lAdditionalPredicates.Add(ppInThird);

                aNewRemove.Preconditions = cfPreconditions;
                cfRemoveEffects.AddOperand(ppInFirst);
                cfRemoveEffects.AddOperand(gpNotInAction.Negate());

                aNewAdd.Preconditions = new PredicateFormula(ppInFirst);
                cfAddEffects.AddOperand(ppInSecond);
                cfAddEffects.AddOperand(ppInFirst.Negate());

                aNewTranslateRemove.Preconditions = new PredicateFormula(ppInSecond);
                cfTranslateRemoveEffects.AddOperand(ppInSecond.Negate());
                cfTranslateRemoveEffects.AddOperand(ppInThird);

                aNewTranslateAdd.Preconditions = new PredicateFormula(ppInThird);
                cfTranslateAddEffects.AddOperand(ppInThird.Negate());
                cfTranslateAddEffects.AddOperand(gpNotInAction);

                Dictionary<Predicate, Predicate> dTaggedPredicates = new Dictionary<Predicate, Predicate>();

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfAddCondition, cfRemoveCondition;
                    cfCondition.SplitAddRemove(dTaggedPredicates, out cfAddCondition, out cfRemoveCondition);
                    if (cfAddCondition != null)
                        cfAddEffects.AddOperand(cfAddCondition);
                    if (cfRemoveCondition != null)
                        cfRemoveEffects.AddOperand(cfRemoveCondition);

                }
                aNewAdd.Effects = cfAddEffects.Simplify();
                aNewRemove.Effects = cfRemoveEffects.Simplify();
                lActions.Add(aNewRemove);
                lActions.Add(aNewAdd);

                foreach (KeyValuePair<Predicate, Predicate> pair in dTaggedPredicates)
                {
                    CompoundFormula cfWhen = new CompoundFormula("when");
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    cfWhen.AddOperand(pair.Key);

                    cfAnd.SimpleAddOperand(pair.Value);
                    cfAnd.SimpleAddOperand(pair.Key.Negate());
                    cfWhen.SimpleAddOperand(cfAnd);

                    if (pair.Value.Negation)
                        cfTranslateRemoveEffects.AddOperand(cfWhen);
                    else
                        cfTranslateAddEffects.AddOperand(cfWhen);
                }

                aNewTranslateAdd.Effects = cfTranslateAddEffects;
                aNewTranslateRemove.Effects = cfTranslateRemoveEffects;
                lActions.Add(aNewTranslateRemove);
                lActions.Add(aNewTranslateAdd);
            }
            else
                throw new NotImplementedException();

            if (Observe != null)
            {
                throw new NotImplementedException();

            }
            return lActions;
        }


        public List<Action> TagCompilationSplitConditionsNoState(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags,
            List<string> lExcludedTags, List<Predicate> lAdditionalPredicates)
        {
            string sName = Name;
            foreach (string sTag in lIncludedTags)
                sName += "-" + sTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);

            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                {
                    aNew.AddParameter(p);
                }
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            CompoundFormula cfPreconditions = new CompoundFormula("and");
            Formula cfNoStatePreconditions = GetPreconditionsNoState(dTags, d, lIncludedTags, lExcludedTags);
            cfPreconditions.AddOperand(cfNoStatePreconditions); //knowledge loss is the first action, so it will have all the preconditions
            aNew.Preconditions = cfPreconditions;

            if (Effects == null)
                throw new NotImplementedException();


            HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
            CompoundFormula cfStateEffects = new CompoundFormula("and");
            //CompoundFormula cfMandatoryEffects = new CompoundFormula("and");
            foreach (Formula f in lObligatory)
            {
                f.GetAllPredicates(lKnowEffects);
            }
            if (lKnowEffects.Count > 0)
            {
                foreach (string sTag in lIncludedTags)
                {
                    //~KNot t|?t -> effects|t
                    CompoundFormula cfKEffects = new CompoundFormula("and");

                    foreach (Predicate p in lKnowEffects)
                    {
                        Predicate pAdd = p.GenerateGiven(sTag);
                        cfKEffects.AddOperand(pAdd);
                    }
                    cfStateEffects.SimpleAddOperand(cfKEffects);
                }
            }

            List<Action> lActions = new List<Action>();

            if (lConditions.Count > 0)
            {
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfK = null, cfAnd = null;
                    HashSet<Predicate> lConditionEffects = cfCondition.Operands[1].GetAllPredicates();
                    cfAnd = new CompoundFormula("and");

                    foreach (string sTag in lIncludedTags)
                    {
                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfStateEffects.SimpleAddOperand(cfK);
                        }
                    }
                }
            }
            
            aNew.Effects = cfStateEffects.Simplify();

            if (lConditions.Count > 0 && SDRPlanner.SplitConditionalEffects)
                lActions.AddRange(aNew.SplitConditions(lAdditionalPredicates));
            else
            {
                ((CompoundFormula)aNew.Preconditions).AddOperand(new GroundedPredicate("NotInAction"));
                lActions.Add(aNew);
            }


            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return lActions;
        }



        public Action KnowWhetherTagCompilation(Dictionary<string, List<Predicate>> dTags, Domain d, List<string> lIncludedTags, List<string> lExcludedTags)
        {
            string sName = Name + "-KW";
            foreach (string sTag in lIncludedTags)
                sName += "-" + sTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);
            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                    aNew.AddParameter(p);
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            aNew.Preconditions = GetKnowWhetherPreconditions(dTags, d, lIncludedTags, lExcludedTags);

            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                //CompoundFormula cfMandatoryEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                }
                if (lKnowEffects.Count > 0)
                {
                    List<Predicate> lFunctionExpressions = new List<Predicate>();
                    List<Predicate> lPredicates = new List<Predicate>();
                    foreach (Predicate p in lKnowEffects)
                    {
                        if (d.IsFunctionExpression(p.Name))
                            lFunctionExpressions.Add(p);
                        else
                            lPredicates.Add(p);
                    }

                    foreach (string sTag in lIncludedTags)
                    {
                        //~KNot t|?t -> effects|t
                        CompoundFormula cfKEffects = new CompoundFormula("and");

                        foreach (Predicate p in lPredicates)
                        {
                            Predicate pAdd = p.GenerateGiven(sTag);
                            cfKEffects.AddOperand(pAdd);
                            if (!d.AlwaysKnown(p))
                            {
                                pAdd = p.GenerateKnowGiven(sTag, true);
                                cfKEffects.AddOperand(pAdd);
                            }
                        }
                        cfEffects.SimpleAddOperand(cfKEffects);
                    }
                    foreach (Predicate p in lFunctionExpressions)
                        cfEffects.AddOperand(p);
                }
                List<Predicate> lAllKnowledgeToRemove = new List<Predicate>();
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfK = null, cfAnd = null;
                    HashSet<Predicate> lConditionEffects = cfCondition.Operands[1].GetAllPredicates();
                    cfAnd = new CompoundFormula("and");

                    foreach (string sTag in lIncludedTags)
                    {
                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            cfEffects.SimpleAddOperand(cfK);                          
                        }
                    }
                    if (SDRPlanner.RemoveAllKnowledge)
                    {
                        foreach (Predicate p in cfCondition.Operands[1].GetAllPredicates())
                        {
                            Predicate pTag = p;
                            if (p.Negation)
                                pTag = p.Negate();
                            if (!lAllKnowledgeToRemove.Contains(pTag))
                                lAllKnowledgeToRemove.Add(pTag);
                        }
                    }
                    else
                    {
                        cfK = CreateTaggedKnowledgeWhetherGainConditions(cfCondition, d, lIncludedTags);
                        if (cfK != null)
                        {
                            cfEffects.SimpleAddOperand(cfK);
                        }

                        cfK = CreateTaggedKnowledgeWhetherLossCondition(cfCondition, d, lIncludedTags);
                        if (cfK != null && cfK.Operands.Count > 0)
                        {
                            cfEffects.SimpleAddOperand(cfK);
                        }
                    }
                     /* causes the plan to add many merge actions
                    foreach (string sTag in lIncludedTags)
                    {
                        foreach (Predicate pForget in lConditionEffects)
                        {
                            if(pForget.Name != Domain.OPTION_PREDICATE)
                                cfEffects.AddOperand(pForget.GenerateKnowGiven(sTag, true).Negate());
                        }
                    }
                      * */
                }
                if (SDRPlanner.RemoveAllKnowledge)
                {
                    foreach (Predicate p in lAllKnowledgeToRemove)
                    {
                        foreach (string sTag in lIncludedTags)
                        {
                            Predicate pNegate = p.GenerateKnowGiven(sTag, true).Negate();
                            cfEffects.AddOperand(pNegate);
                        }
                    }
                }
                aNew.Effects = cfEffects.Simplify();
            }
            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return aNew;
        }


        public Action KnowWhetherTagCompilation(Dictionary<string, List<Predicate>> dTags, Domain d, string sActionTag)
        {
            string sName = Name + "-KW-" + sActionTag;
            ParametrizedAction aNew = new ParametrizedAction(sName);
            if (this is ParametrizedAction)
            {
                foreach (Parameter p in ((ParametrizedAction)this).Parameters)
                    aNew.AddParameter(p);
            }

            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            aNew.Preconditions = GetKnowWhetherPreconditions(dTags, d, sActionTag);

            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                CompoundFormula cfMandatoryEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                }
                if (lKnowEffects.Count > 0)
                {
                    foreach (string sTag in dTags.Keys)
                    {
                        //~KNot t|?t -> effects|t
                        CompoundFormula cfKEffects = new CompoundFormula("and");

                        foreach (Predicate p in lKnowEffects)
                        {
                            Predicate pAdd = p.GenerateGiven(sTag);
                            cfKEffects.AddOperand(pAdd);
                                //pAdd = p.GenerateKnowGiven(sTag, true);
                                //pAdd = p.GenerateGiven(sTag);
                                //cfKEffects.AddOperand(pAdd);
                            
                        }


                        if (sTag == sActionTag)
                            cfEffects.SimpleAddOperand(cfKEffects);
                        else
                        {
                            CompoundFormula cfCondition = new CompoundFormula("when");
                            Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, sActionTag));

                            cfCondition.AddOperand(pNotTag.Negate());
                            cfCondition.AddOperand(cfKEffects);

                            cfEffects.SimpleAddOperand(cfCondition);
                        }
                    }
                }
                /*
                //forgetting: ~K~p
                foreach (Predicate p in lKnowEffects)
                {
                    Predicate pKNotp = new KnowPredicate(p.Negate());
                    cfEffects.AddOperand(pKNotp.Negate());
                }
                 * */
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfK = null, cfAnd = null;
                    HashSet<Predicate> lConditionEffects = cfCondition.Operands[1].GetAllPredicates();
                    cfAnd = new CompoundFormula("and");
                    /*
                    //since this action is done only for a part of the states, you lose all information in the effects
                    foreach (Predicate p in lConditionEffects)
                    {
                        if (p.Name != Domain.OPTION_PREDICATE)
                        {
                            Predicate pK = new KnowPredicate(p);
                            cfAnd.AddOperand(pK.Negate());
                            pK = new KnowPredicate(p.Negate());
                            cfAnd.AddOperand(pK.Negate());
                            pK = new KnowWhetherPredicate(p);
                            cfAnd.AddOperand(pK.Negate());
                        }
                    }
                    if (cfAnd.Operands.Count > 0)
                        cfEffects.SimpleAddOperand(cfAnd);
                     * */
                    foreach (string sTag in dTags.Keys)
                    {

                        cfK = CreateTaggedCondition(cfCondition, d, sTag);
                        if (cfK != null)
                        {
                            if (sTag == sActionTag)
                            {
                                cfEffects.SimpleAddOperand(cfK);
                            }
                            else
                            {
                                Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, sActionTag));

                                cfAnd = new CompoundFormula("and");
                                cfAnd.AddOperand(pNotTag.Negate());
                                if (cfK.Operator == "when")
                                {
                                    cfAnd.AddOperand(cfK.Operands[0]);
                                    cfK.Operands[0] = cfAnd;
                                    cfEffects.SimpleAddOperand(cfK);
                                }
                                else
                                    throw new NotImplementedException();
                            }
                        }
                    }
                    /*
                    cfK = CreateTaggedKnowledgeWhetherGainConditions(cfCondition, d, dTags.Keys, sActionTag);
                    if (cfK != null)
                    {
                        cfEffects.SimpleAddOperand(cfK);
                    }

                    cfK = CreateTaggedKnowledgeWhetherLossCondition(cfCondition, d, dTags.Keys, sActionTag);
                    if (cfK != null && cfK.Operands.Count > 0)
                    {
                        cfEffects.SimpleAddOperand(cfK);
                    }
                    */
                }
                aNew.Effects = cfEffects.Simplify();
            }
            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return aNew;
        }

        public List<Action> AddTaggedNonDeterministicConditionsAgentChoice(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown)
        {
            Action aNew = Clone();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                aNew.Preconditions = cfPreconditions;
            }

            int cOptions = Effects.GetMaxNonDeterministicOptions();
            List<Action> lOptionalActions = new List<Action>();
            for (int iOption = 0; iOption < cOptions; iOption++)
            {
                Action aOption = aNew.Clone();
                aOption.Name = aNew.Name + "-op" + iOption;

                //currently only handling non-determinism in conditional effects
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    cfEffects.AddOperand(f);
                }
                foreach (Predicate p in lKnowEffects)
                {
                    if (!lAlwaysKnown.Contains(p.Name))
                    {
                        cfEffects.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                        foreach (string sTag in dTags.Keys)
                        {
                            Predicate pKEffect = p.GenerateKnowGiven(sTag);
                            cfEffects.AddOperand(pKEffect);
                            pKEffect = p.Negate().GenerateKnowGiven(sTag).Negate();
                            cfEffects.AddOperand(pKEffect);
                        }
                    }
                }

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfDeterministicCondition = cfCondition;
                    CompoundFormula cfK = null;
                    if (cfCondition.ContainsNonDeterministicEffect())
                    {
                        cfDeterministicCondition = (CompoundFormula)cfCondition.ChooseOption(iOption);
                        CompoundFormula cfForgetAll = new CompoundFormula("and");
                        HashSet<Predicate> lOptionalPredicates = cfCondition.GetAllOptionalPredicates();
                        foreach (Predicate p in lOptionalPredicates)
                        {
                            cfForgetAll.AddOperand(new PredicateFormula(new KnowPredicate(p)).Negate());
                        }
                        CompoundFormula cfForgetCondition = new CompoundFormula("when");
                        cfForgetCondition.AddOperand(cfCondition.Operands[0].Clone());
                        cfForgetCondition.AddOperand(cfForgetAll);
                        cfEffects.AddOperand(cfForgetCondition);
                    }
                    else
                    {
                        cfK = CreateKnowledgeGainCondition(cfDeterministicCondition, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                        cfK = CreateKnowledgeLossCondition(cfDeterministicCondition, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                    }
                    cfEffects.AddOperand(cfDeterministicCondition);
                    foreach (string sTag in dTags.Keys)
                    {
                        cfK = CreateTaggedKnowledgeGainCondition(cfDeterministicCondition, sTag, lAlwaysKnown, true);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                        cfK = CreateTaggedKnowledgeLossCondition(cfDeterministicCondition, sTag, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                    }
                }
                aOption.Effects = cfEffects;
                lOptionalActions.Add(aOption);
            }
            //assuming that there can't be any observations
            if (Observe != null)
                throw new NotImplementedException();
            return lOptionalActions;
        }

        public List<Action> AddTaggedNonDeterministicConditions(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown)
        {
            //return AddTaggedNonDeterministicConditionsStochasticChoice(dTags, lAlwaysKnown, Domain.TIME_STEPS);
            return AddTaggedNonDeterministicConditionsAgentChoice(dTags, lAlwaysKnown);
        }

        public List<Action> AddTaggedNonDeterministicConditionsStochasticChoice(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown, int cTimeSteps)
        {
            Action aNew = Clone();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                aNew.Preconditions = cfPreconditions;
            }

            int cOptions = Effects.GetMaxNonDeterministicOptions();
            List<Action> lOptionalActions = new List<Action>();
            for (int iTime = 0; iTime < cOptions; iTime++)
            {
                Action aOption = aNew.Clone();
                aOption.Name = aNew.Name + "-time" + iTime;
                ((CompoundFormula)aOption.Preconditions).AddOperand(new GroundedPredicate("time" + iTime));

                //currently only handling non-determinism in conditional effects
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                cfEffects.AddOperand(new GroundedPredicate("time" + iTime).Negate());
                cfEffects.AddOperand(new GroundedPredicate("time" + ((iTime + 1) % cTimeSteps)));
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    cfEffects.AddOperand(f);
                }
                foreach (Predicate p in lKnowEffects)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfEffects.AddOperand(new PredicateFormula(new KnowPredicate(p)));

                foreach (CompoundFormula cfCondition in lConditions)
                {
                    CompoundFormula cfDeterministicCondition = cfCondition;
                    CompoundFormula cfK = null;
                    if (cfCondition.ContainsNonDeterministicEffect())
                    {
                        int iOption = RandomGenerator.Next(cfCondition.GetMaxNonDeterministicOptions());
                        cfDeterministicCondition = (CompoundFormula)cfCondition.ChooseOption(iOption);
                        CompoundFormula cfForgetAll = new CompoundFormula("and");
                        HashSet<Predicate> lOptionalPredicates = cfCondition.GetAllOptionalPredicates();
                        foreach (Predicate p in lOptionalPredicates)
                        {
                            cfForgetAll.AddOperand(new PredicateFormula(new KnowPredicate(p)).Negate());
                        }
                        CompoundFormula cfForgetCondition = new CompoundFormula("when");
                        cfForgetCondition.AddOperand(cfCondition.Operands[0].Clone());
                        cfForgetCondition.AddOperand(cfForgetAll);
                        cfEffects.AddOperand(cfForgetCondition);
                    }
                    else
                    {
                        cfK = CreateKnowledgeGainCondition(cfDeterministicCondition, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                        cfK = CreateKnowledgeLossCondition(cfDeterministicCondition, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                    }
                    cfEffects.AddOperand(cfDeterministicCondition);
                    foreach (string sTag in dTags.Keys)
                    {
                        //in practice,non-det effects make the tag forget its current setting
                        //doing this as below is equivalent to "particle filtering" where we resample the tags continuosly from the next belief state
                        cfK = CreateTaggedKnowledgeGainCondition(cfDeterministicCondition, sTag, lAlwaysKnown, true);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                        cfK = CreateTaggedKnowledgeLossCondition(cfDeterministicCondition, sTag, lAlwaysKnown);
                        if (cfK != null)
                            cfEffects.AddOperand(cfK);
                    }
                }
                aOption.Effects = cfEffects;
                lOptionalActions.Add(aOption);
            }
            //assuming that there can't be any observations
            if (Observe != null)
                throw new NotImplementedException();
            return lOptionalActions;
        }

        public Action AddKnowledge(List<string> lAlwaysKnown)
        {
            Action aNew = Clone();

            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                aNew.Preconditions = cfPreconditions;
            }
            if (Effects != null)
            {
                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfEffects = new CompoundFormula("and");
                Effects.GetAllPredicates(lKnowEffects);
                cfEffects.AddOperand(Effects.Clone());

                foreach (Predicate p in lKnowEffects)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfEffects.AddOperand(new PredicateFormula(new KnowPredicate(p)));
                
                aNew.Effects = cfEffects;
            }
            if (Observe != null)
            {
                if (aNew.Effects == null)
                    aNew.Effects = new CompoundFormula("and");
                Predicate pObserve = ((PredicateFormula)Observe).Predicate;
                ((CompoundFormula)aNew.Effects).AddOperand(new KnowPredicate(pObserve));
            }
            return aNew;
        }


        public List<Action> SplitTaggedConditions(Dictionary<string, List<Predicate>> dTags, List<string> lAlwaysKnown)
        {
            List<Action> lSplitted = new List<Action>();

            CompoundFormula cfPreconditions = new CompoundFormula("and");
            HashSet<Predicate> lKnowPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
            {
                Preconditions.GetAllPredicates(lKnowPreconditions);
                cfPreconditions.AddOperand(Preconditions);
                foreach (Predicate p in lKnowPreconditions)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfPreconditions.AddOperand(new PredicateFormula(new KnowPredicate(p)));
            }

            if (Effects != null)
            {
                List<CompoundFormula> lConditions = new List<CompoundFormula>();
                List<Formula> lObligatory = new List<Formula>();
                SplitEffects(lConditions, lObligatory);

                HashSet<Predicate> lKnowEffects = new HashSet<Predicate>();
                CompoundFormula cfGeneralEffects = new CompoundFormula("and");
                foreach (Formula f in lObligatory)
                {
                    f.GetAllPredicates(lKnowEffects);
                    cfGeneralEffects.AddOperand(f);
                }
                foreach (Predicate p in lKnowEffects)
                    if (!lAlwaysKnown.Contains(p.Name))
                        cfGeneralEffects.AddOperand(new PredicateFormula(new KnowPredicate(p)));

                int iCondition = 0;
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    Action aNew = new Action(Name + "_" + iCondition);
                    aNew.Preconditions = cfPreconditions.Clone();
                    CompoundFormula cfEffects = (CompoundFormula)cfGeneralEffects.Clone();

                    cfEffects.AddOperand(cfCondition);
                    cfEffects.AddOperand(CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown));
                    cfEffects.AddOperand(CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown));
                    foreach (string sTag in dTags.Keys)
                    {
                        cfEffects.AddOperand(CreateTaggedKnowledgeGainCondition(cfCondition, sTag, lAlwaysKnown, false));
                        cfEffects.AddOperand(CreateTaggedKnowledgeLossCondition(cfCondition, sTag, lAlwaysKnown));
                    }
                    aNew.Effects = cfEffects;
                    iCondition++;
                    lSplitted.Add(aNew);
               }
            }
            if (Observe != null)
            {
                throw new NotImplementedException();
            }
            return lSplitted;
        }

        public virtual Action Clone()
        {
            Action aNew = new Action(Name);
            if (Preconditions != null)
                aNew.Preconditions = Preconditions.Clone();
            if (Effects != null)
            {
                aNew.Effects = Effects.Clone();
                aNew.Effects.ResetCache();
            }
            if (Observe != null)
                aNew.Observe = Observe.Clone();
            aNew.HasConditionalEffects = HasConditionalEffects;
            aNew.ContainsNonDeterministicEffect = ContainsNonDeterministicEffect;
            aNew.NonDeterministicEffects = new HashSet<Predicate>(NonDeterministicEffects);
            aNew.Original = Original;
            aNew.OriginalActionBeforeSplit = OriginalActionBeforeSplit;
            aNew.Cost = Cost;
            
            aNew.Preconditions.ResetCache();
            return aNew;
        }

        //(f->g) ==> (Kf->Kg)
        private CompoundFormula CreateKnowledgeGainCondition(CompoundFormula cfCondition, List<string> lAlwaysKnown)
        {
            throw new NotImplementedException();
            //return CreateKnowledgeGainCondition(cfCondition, lAlwaysKnown);
        }
        //(f->g) ==> (Kf->Kg)
        private CompoundFormula CreateKnowledgeGainCondition(CompoundFormula cfCondition, List<string> lAlwaysKnown, bool bKnowWhether)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            Formula fPreconditions = cfCondition.Operands[0].GetKnowledgeFormula(lAlwaysKnown, bKnowWhether, null);
            if (fPreconditions == null)
                return null;
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    if (bKnowWhether)
                        cfEffects.AddOperand(new KnowWhetherPredicate(p));
                    else
                    {
                        cfEffects.AddOperand(new KnowPredicate(p));
                        cfEffects.AddOperand(new KnowPredicate(p.Negate()).Negate());
                    }
                }
            if (cfEffects.Operands.Count == 0)
                return null;
            cfWhen.AddOperand(fPreconditions);
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //(f->g) ==> (Kf->Kg)
        private CompoundFormula CreateKnowledgeGainConditionII(CompoundFormula cfCondition, List<string> lAlwaysKnown, bool bKnowWhether)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            //cfPreconditions.AddOperand(cfCondition.Operands[0].Simplify());
            foreach (Predicate p in lPreconditions)
            //if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    return null;//we never know an option value
                if (bKnowWhether)
                    cfPreconditions.AddOperand(new KnowWhetherPredicate(p));
                else
                    cfPreconditions.AddOperand(new KnowPredicate(p));
            }
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
            //if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
            {
                if (bKnowWhether)
                    cfEffects.AddOperand(new KnowWhetherPredicate(p));
                else
                {
                    cfEffects.AddOperand(new KnowPredicate(p));
                    cfEffects.AddOperand(new KnowPredicate(p.Negate()).Negate());
                }
            }
            if (cfEffects.Operands.Count == 0)
                return null;
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   KC/t->KL/t
        private CompoundFormula CreateTaggedKnowledgeGainCondition(CompoundFormula cfCondition, string sTag, List<string> lAlwaysKnown, bool bNonDetEffect)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            Formula cfPreconditions = cfCondition.Operands[0].GenerateGiven(sTag, lAlwaysKnown);
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
            {
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    Predicate pKEffect = p.GenerateKnowGiven(sTag);
                    cfEffects.AddOperand(pKEffect);
                    if (bNonDetEffect)
                    {
                        KnowPredicate pK = new KnowPredicate(p);
                        cfEffects.AddOperand(pK.Negate());
                    }
                }
            }
            if (cfEffects.Operands.Count == 0)
                return null;
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   ~K~C/t->~K~L/t
        private CompoundFormula CreateTaggedKnowledgeLossCondition(CompoundFormula cfCondition, string sTag, List<string> lAlwaysKnown)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            bool bAllKnown = true;
            foreach (Predicate p in lPreconditions)
            {
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    Predicate pKGiven = p.Negate().GenerateKnowGiven(sTag);
                    cfPreconditions.AddOperand(pKGiven.Negate());
                    bAllKnown = false;
                }
                else
                    cfPreconditions.AddOperand(p);
            }
            if (bAllKnown)//when all given are known then there is no knowledge loss
                return null;
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
            {
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    Predicate pKGiven = p.Negate().GenerateKnowGiven(sTag);
                    cfEffects.AddOperand(pKGiven.Negate());
                }
            }
            if (cfEffects.Operands.Count == 0)
                return null;
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   C/t->L/t
        private CompoundFormula CreateTaggedCondition(CompoundFormula cfCondition, Domain d, string sTag)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            CompoundFormula cfEffects = new CompoundFormula("and");
           
            foreach (Predicate p in lPreconditions)
            {
                //Predicate pKGiven = p.Negate().GenerateGiven(sTag);
                //cfPreconditions.AddOperand(pKGiven.Negate());
                Predicate pKGiven = null;
                if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                    pKGiven = p;
                else
                    pKGiven = p.GenerateGiven(sTag);
                cfPreconditions.AddOperand(pKGiven);
            }
            foreach (Predicate p in lEffects)
            {
                Predicate pKEffect = p.GenerateGiven(sTag);
                cfEffects.AddOperand(pKEffect);
            }
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   KWC/t->KWL/t
        private CompoundFormula CreateTaggedKnowledgeWhetherGainCondition(CompoundFormula cfCondition, Domain d, string sTag)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    return null;
                Predicate pKGiven = null;
                
                if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                {
                    pKGiven = new KnowPredicate(p);
                    cfPreconditions.AddOperand(pKGiven);
                }
                else
                {
                    if (!d.AlwaysKnown(p))
                    {
                        pKGiven = p.GenerateKnowGiven(sTag, true);
                        cfPreconditions.AddOperand(pKGiven);
                    }
                    pKGiven = p.GenerateGiven(sTag);
                    cfPreconditions.AddOperand(pKGiven);
                }
            }
            foreach (Predicate p in lEffects)
            {
                Predicate pKEffect = p.GenerateKnowGiven(sTag, true);
                cfEffects.AddOperand(pKEffect);
            }
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   KWC/t->KWL/t
        private CompoundFormula CreateTaggedKnowledgeWhetherGainConditions(CompoundFormula cfCondition, Domain d, List<string> lIncludedTags)
        {
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);

            CompoundFormula cfWhen = new CompoundFormula("when");
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    return null;
                Predicate pKGiven = null;

                if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                {
                    pKGiven = new KnowPredicate(p);
                    cfPreconditions.AddOperand(pKGiven);
                }
            }
            foreach (string sKWTag in lIncludedTags)
            {


                CompoundFormula cfAnd = new CompoundFormula("and");
                foreach (Predicate p in lPreconditions)
                {
                    Predicate pKGiven = null;

                    if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                    {
                        continue;
                    }
                    else
                    {
                        if (!d.AlwaysKnown(p))
                        {
                            pKGiven = p.GenerateKnowGiven(sKWTag, true);
                            cfAnd.AddOperand(pKGiven);
                        }
                        pKGiven = p.GenerateGiven(sKWTag);
                        cfAnd.AddOperand(pKGiven);
                    }
                }
                if (cfAnd.Operands.Count > 0)
                {
                    cfPreconditions.AddOperand(cfAnd);
                }


                foreach (Predicate p in lEffects)
                {
                    Predicate pKEffect = p.GenerateKnowGiven(sKWTag, true);
                    cfEffects.AddOperand(pKEffect);
                }
            }
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());

            return cfWhen;
        }
        //C->L  ==>   KWC/t->KWL/t
        private CompoundFormula CreateTaggedKnowledgeWhetherGainConditions(CompoundFormula cfCondition, Domain d, IEnumerable<string> lTags, string sActionTag)
        {
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfAllConditions = new CompoundFormula("and");

            foreach (string sKWTag in lTags)
            {
                CompoundFormula cfWhen = new CompoundFormula("when");
                CompoundFormula cfPreconditions = new CompoundFormula("and");
                CompoundFormula cfEffects = new CompoundFormula("and");

                if (sKWTag != sActionTag)
                {
                    Predicate pNotKWTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sKWTag),new Constant(Domain.TAG, sActionTag));
                    cfPreconditions.AddOperand(pNotKWTag.Negate());
                }

                foreach (Predicate p in lPreconditions)
                {
                    if (p.Name == Domain.OPTION_PREDICATE)
                        return null;
                    Predicate pKGiven = null;

                    if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                    {
                        pKGiven = new KnowPredicate(p);
                        cfPreconditions.AddOperand(pKGiven);
                    }

                }

                foreach (string sTag in lTags)
                {
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    foreach (Predicate p in lPreconditions)
                    {
                        Predicate pKGiven = null;

                        //if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                        if (d.AlwaysKnown(p))
                        {
                            continue;
                        }
                        else
                        {
                            /*
                            if (!d.AlwaysKnown(p))
                            {
                                pKGiven = p.GenerateKnowGiven(sTag, true);
                                cfAnd.AddOperand(pKGiven);
                            }
                             * */
                            pKGiven = p.GenerateGiven(sTag);
                            cfAnd.AddOperand(pKGiven);
                        }
                    }
                    if (cfAnd.Operands.Count > 0)//if there are no conditions then it is always true, and we don't need to care about whether the tag is consistent or not
                    {
                        if (sTag == sActionTag)
                        {
                            cfPreconditions.AddOperand(cfAnd);
                        }
                        else
                        {
                            CompoundFormula cfOr = new CompoundFormula("or");
                            Predicate pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag),new Constant(Domain.TAG, sActionTag));
                            cfOr.AddOperand(pNotTag);
                            cfOr.AddOperand(cfAnd);
                            cfPreconditions.AddOperand(cfOr);
                        }
                    }
                }

                foreach (Predicate p in lEffects)
                {
                    //Predicate pKEffect = p.GenerateKnowGiven(sKWTag, true);
                    Predicate pKEffect = p.GenerateGiven(sKWTag);
                    cfEffects.AddOperand(pKEffect);
                }
                
                cfWhen.AddOperand(cfPreconditions.Simplify());
                cfWhen.AddOperand(cfEffects.Simplify());
                cfAllConditions.SimpleAddOperand(cfWhen);
            }
            return cfAllConditions;
        }
         //C->L  ==>   ~KW~C/t->~KW~L/t
        private CompoundFormula CreateTaggedKnowledgeWhetherLossCondition(CompoundFormula cfCondition, Domain d, string sTag)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            bool bContainsOption = false;
            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    bContainsOption = true;
                Predicate pKGiven = null;
                if (!d.AlwaysKnown(p))
                {
                    pKGiven = p.GenerateKnowGiven(sTag, true);
                    cfPreconditions.AddOperand(pKGiven.Negate());
                }
            }
            if (cfPreconditions.Operands.Count == 0)
                return null;
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    continue;
                Predicate pKGiven = p.Negate().GenerateKnowGiven(sTag, true);
                cfEffects.AddOperand(pKGiven.Negate());
            }
            if (cfEffects.Operands.Count == 0)
                return null;

            if (bContainsOption)
                return cfEffects;
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }
        //C->L  ==>   ~KW~C/t->~KW~L/t
        private CompoundFormula CreateTaggedKnowledgeWhetherLossCondition(CompoundFormula cfCondition, Domain d, List<string> lIncludedTags)
        {
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            bool bContainsOption = false;
            CompoundFormula cfAllConditions = new CompoundFormula("and");
            ParameterizedPredicate pNotTag = null;

            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    bContainsOption = true;
            }
            foreach (string sForgetTag in lIncludedTags)
            {
                CompoundFormula cfEffects = new CompoundFormula("and");
                foreach (Predicate p in lEffects)
                {
                    if (p.Name != Domain.OPTION_PREDICATE)
                    {
                        Predicate pKEffect = p.GenerateKnowGiven(sForgetTag, true);
                        cfEffects.AddOperand(pKEffect.Negate());
                    }
                }
                if (bContainsOption)
                {
                    cfAllConditions.AddOperand(cfEffects);                    
                }
                else
                {
                    foreach (string sTag in lIncludedTags)
                    {

                        CompoundFormula cfWhen = new CompoundFormula("when");
                        CompoundFormula cfPreconditions = new CompoundFormula("and");

                        CompoundFormula cfOr = new CompoundFormula("or");
                        foreach (Predicate p in lPreconditions)
                        {
                            Predicate pKGiven = null, pGiven = null;

                            if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                            {
                                continue;//there is an underlying assumption here that always known + always constant means that it is also always true
                            }
                            else
                            {
                                if (!d.AlwaysKnown(p))
                                {
                                    pKGiven = p.GenerateKnowGiven(sTag, true);
                                    cfOr.AddOperand(pKGiven.Negate());
                                    //pGiven = p.GenerateGiven(sTag);
                                    //cfOr.AddOperand(pGiven.Negate());
                                }
                            }
                        }
                        if (cfOr.Operands.Count > 0)
                        {
                            cfPreconditions.AddOperand(cfOr);
                            cfWhen.AddOperand(cfPreconditions.Simplify());
                            cfWhen.AddOperand(cfEffects.Simplify());

                            cfAllConditions.AddOperand(cfWhen);
                        }
                    }
                }
            }
            return cfAllConditions;
        }
        private CompoundFormula CreateTaggedKnowledgeWhetherLossCondition(CompoundFormula cfCondition, Domain d, IEnumerable<string> lTags, string sActionTag)
        {
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            bool bContainsOption = false;
            CompoundFormula cfAllConditions = new CompoundFormula("and");
            Predicate pNotTag = null;

            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    bContainsOption = true;
            }
            foreach (string sForgetTag in lTags)
            {
                CompoundFormula cfEffects = new CompoundFormula("and");
                foreach (Predicate p in lEffects)
                {
                    if (p.Name != Domain.OPTION_PREDICATE)
                    {
                        Predicate pKEffect = p.GenerateKnowGiven(sForgetTag, true);
                        cfEffects.AddOperand(pKEffect.Negate());                       
                    }
                }
                if (bContainsOption)
                {
                    CompoundFormula cfWhen = new CompoundFormula("when");
                    CompoundFormula cfPreconditions = new CompoundFormula("and");

                    if (sForgetTag != sActionTag)
                    {
                        pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sForgetTag),new Constant(Domain.TAG, sActionTag));
                        cfPreconditions.AddOperand(pNotTag.Negate());
                        cfWhen.AddOperand(cfPreconditions);
                        cfWhen.AddOperand(cfEffects);
                        cfAllConditions.SimpleAddOperand(cfWhen);
                    }
                    else
                    {
                        cfAllConditions.AddOperand(cfEffects);
                    }
                }
                else
                {
                    foreach (string sTag in lTags)
                    {

                        CompoundFormula cfWhen = new CompoundFormula("when");
                        CompoundFormula cfPreconditions = new CompoundFormula("and");

                        if (sForgetTag != sActionTag)
                        {
                            pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sForgetTag),new Constant(Domain.TAG, sActionTag));
                            cfPreconditions.AddOperand(pNotTag.Negate());
                        }

                        if (sTag != sActionTag)
                        {
                            pNotTag = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag), new Constant(Domain.TAG, sActionTag));
                            cfPreconditions.AddOperand(pNotTag.Negate());
                        }

                        CompoundFormula cfOr = new CompoundFormula("or");
                        foreach (Predicate p in lPreconditions)
                        {
                            Predicate pKGiven = null, pGiven = null;

                            if (d.AlwaysKnown(p) && d.AlwaysConstant(p))
                            {
                                continue;//there is an underlying assumption here that always known + always constant means that it is also always true
                            }
                            else
                            {
                                if (!d.AlwaysKnown(p))
                                {
                                    pKGiven = p.GenerateKnowGiven(sTag, true);
                                    cfOr.AddOperand(pKGiven.Negate());
                                    //pGiven = p.GenerateGiven(sTag);
                                    //cfOr.AddOperand(pGiven.Negate());
                                }
                            }
                        }
                        if(cfOr.Operands.Count > 0)
                        {
                            cfPreconditions.AddOperand(cfOr);
                            cfWhen.AddOperand(cfPreconditions.Simplify());
                            cfWhen.AddOperand(cfEffects.Simplify());

                            cfAllConditions.AddOperand(cfWhen);
                        }
                    }
                }
            }
            return cfAllConditions;
        }
        private CompoundFormula CreateKnowledgeLossCondition(CompoundFormula cfCondition, List<string> lAlwaysKnown)
        {
            return CreateKnowledgeLossCondition(cfCondition, lAlwaysKnown, false);
        }
        //(f->g) ==> (~K~f->~K~g)
        private CompoundFormula CreateKnowledgeLossCondition(CompoundFormula cfCondition, List<string> lAlwaysKnown, bool bKnowWhether)
        {
            CompoundFormula cfWhen = new CompoundFormula("when");
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            HashSet<Predicate> lEffects = new HashSet<Predicate>();
            cfCondition.Operands[0].GetAllPredicates(lPreconditions);
            cfCondition.Operands[1].GetAllPredicates(lEffects);
            CompoundFormula cfPreconditions = new CompoundFormula("and");
            //cfPreconditions.AddOperand(cfCondition.Operands[0].Simplify().Negate());
            bool bOptionPrecondition = false;
            foreach (Predicate p in lPreconditions)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    bOptionPrecondition = true;//we never know or not know the value of an option so we always forget here
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    if(bKnowWhether)
                        cfPreconditions.AddOperand(new KnowWhetherPredicate(p.Negate()).Negate());
                    else
                        cfPreconditions.AddOperand(new KnowPredicate(p.Negate()).Negate());
                 }
            }
            //if (bAllKnown)
            //    return null;
            if (cfPreconditions.Operands.Count == 0)//if all given are known then there is no knowledge loss
                return null;
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Predicate p in lEffects)
            {
                if (p.Name == Domain.OPTION_PREDICATE)
                    continue;
                if (lAlwaysKnown == null || !lAlwaysKnown.Contains(p.Name))
                {
                    if (bKnowWhether)
                        cfEffects.AddOperand(new KnowWhetherPredicate(p.Negate()).Negate());
                    else
                        cfEffects.AddOperand(new KnowPredicate(p.Negate()).Negate());

                }
            }
            if (cfEffects.Operands.Count == 0)
                return null;
            if (bOptionPrecondition)
                return cfEffects;
            cfWhen.AddOperand(cfPreconditions.Simplify());
            cfWhen.AddOperand(cfEffects.Simplify());
            return cfWhen;
        }

        public List<Predicate> GetNonDeterministicEffects()
        {
            if (Effects == null)
                return null;
            return Effects.GetNonDeterministicEffects();
        }
        public List<CompoundFormula> GetNonDeterministicOptions()
        {
            List<CompoundFormula> lOptions = new List<CompoundFormula>();
            if (Effects == null)
                return null;
            Effects.GetNonDeterministicOptions(lOptions);
            return lOptions;
        }

        public void SetChoice(int iCondition, int iChoice)
        {
            if (iChoice == -1)
                return;
            if (!m_mMapConditionsChoices.ContainsKey(iCondition))
                m_mMapConditionsChoices[iCondition] = new List<int>();
            m_mMapConditionsChoices[iCondition].Add(iChoice);
        }

        public void ClearConditionsChoices()
        {
            m_mMapConditionsChoices = new Dictionary<int, List<int>>();
        }
        public List<HashSet<Predicate>> PreconditionsForEffect(Predicate p)
        {
            bool bPossibleEffect = false;
            List<HashSet<Predicate>> lAll = new List<HashSet<Predicate>>();
            HashSet<Predicate> lPreconditions = new HashSet<Predicate>();
            if (Preconditions != null)
                Preconditions.GetAllPredicates(lPreconditions);
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);
            if (lObligatory.Contains(new PredicateFormula(p)))
            {
                bPossibleEffect = true;
                lAll.Add(lPreconditions);
            }
            else
            {
                foreach (CompoundFormula cf in lConditions)
                {
                    HashSet<Predicate> lConditionEffects = cf.Operands[1].GetAllPredicates();
                    if (lConditionEffects.Contains(p))
                    {
                        HashSet<Predicate> lCondition = new HashSet<Predicate>(lPreconditions);
                        cf.Operands[0].GetAllPredicates(lCondition);
                        lAll.Add(lCondition);
                        bPossibleEffect = true;
                    }
                }
            }
            if (bPossibleEffect)
                return lAll;
            return null;
        }
        /*
        public CompoundFormula ToTimeFormula(int iTime, List<Predicate> lObserved, List<Predicate> lUnknown)
        {
            //currently we will only do the conditional effects
            List<CompoundFormula> lConditions = GetConditions();
            CompoundFormula cfAnd = new CompoundFormula("and");
            Dictionary<Predicate, List<Formula>> dAdd = new Dictionary<Predicate, List<Formula>>();
            Dictionary<Predicate, List<Formula>> dDelete = new Dictionary<Predicate, List<Formula>>();
            List<Predicate> lAllEffects = new List<Predicate>();
            foreach (CompoundFormula cfCondition in lConditions)
            {
                //no need to add known effects into the belief
                if (!cfCondition.Operands[0].IsTrue(lObserved) && !cfCondition.Operands[0].IsFalse(lObserved))
                {
                    Formula fReducedCondition = cfCondition.Operands[0].Reduce(lObserved);
                    List<Predicate> lPredicates = cfCondition.Operands[1].GetAllPredicates();
                    foreach (Predicate p in lPredicates)
                    {
                        Predicate pTrue = p;
                        if (p.Negation)
                            pTrue = p.Negate();
                        if (!dAdd.ContainsKey(pTrue))
                        {
                            dAdd[pTrue] = new List<Formula>();
                            lAllEffects.Add(pTrue);
                        }
                        if (!dDelete.ContainsKey(pTrue))
                            dDelete[pTrue] = new List<Formula>();
                        if (p.Negation)
                            dDelete[pTrue].Add(fReducedCondition);
                        else
                            dAdd[pTrue].Add(fReducedCondition);
                    }
                }
            }
            foreach (Predicate p in lAllEffects)
            {
                CompoundFormula cfOneof1 = new CompoundFormula("oneof");
                CompoundFormula cfOrDeletes = new CompoundFormula("or");
                CompoundFormula cfOrAdds = new CompoundFormula("or");
                CompoundFormula cfEffectAndNotDelete = new CompoundFormula("and");
                CompoundFormula cfNotDeleteOrAdd = new CompoundFormula("or");

                foreach (Formula f in dDelete[p])
                    cfOrDeletes.AddOperand(f);

                foreach(Formula f in dAdd[p])
                    cfOrAdds.AddOperand(f);

                if (!lObserved.Contains(p.Negate()))
                {
                    cfEffectAndNotDelete.AddOperand(p);
                    cfEffectAndNotDelete.AddOperand(cfOrDeletes.Negate());
                    if(!cfEffectAndNotDelete.IsFalse(lObserved))
                        cfNotDeleteOrAdd.AddOperand(cfEffectAndNotDelete);
                }
                cfNotDeleteOrAdd.AddOperand(cfOrAdds);


                //seems right - can still have bugs
                if (cfNotDeleteOrAdd.IsFalse(lObserved))
                {
                    cfAnd.AddOperand(new TimePredicate(p.Negate(), iTime));
                }
                else if (cfNotDeleteOrAdd.IsTrue(lObserved))
                {
                    cfAnd.AddOperand(new TimePredicate(p, iTime));
                }
                else
                {
                    cfOneof1.AddOperand(cfNotDeleteOrAdd.AddTime(iTime - 1));
                    cfOneof1.AddOperand(new TimePredicate(p.Negate(), iTime));

                    cfAnd.AddOperand(cfOneof1);
                }

                CompoundFormula cfOneof2 = new CompoundFormula("oneof");
                CompoundFormula cfNotEffectAndNotAdd = new CompoundFormula("and");
                CompoundFormula cfNotAddOrDelete = new CompoundFormula("or");


                if (!lObserved.Contains(p))
                {
                    cfNotEffectAndNotAdd.AddOperand(p.Negate());
                    cfNotEffectAndNotAdd.AddOperand(cfOrAdds.Negate());
                    if (!cfNotEffectAndNotAdd.IsFalse(lObserved))
                        cfNotAddOrDelete.AddOperand(cfNotEffectAndNotAdd);
                }
                cfNotAddOrDelete.AddOperand(cfOrDeletes);


                //seems right - can still have bugs
                if (cfNotAddOrDelete.IsFalse(lObserved))
                {
                    cfAnd.AddOperand(new TimePredicate(p, iTime));
                }
                else if (cfNotAddOrDelete.IsTrue(lObserved))
                {
                    cfAnd.AddOperand(new TimePredicate(p.Negate(), iTime));
                }
                else
                {
                    cfOneof2.AddOperand(cfNotAddOrDelete.AddTime(iTime - 1));
                    cfOneof2.AddOperand(new TimePredicate(p, iTime));

                    cfAnd.AddOperand(cfOneof2);
                }
            }
            foreach (Predicate p in lUnknown)
            {
                if (!dAdd.ContainsKey(p))
                {
                    CompoundFormula cfOneOf = new CompoundFormula("oneof");
                    cfOneOf.AddOperand(new TimePredicate(p.Negate(), iTime - 1));
                    cfOneOf.AddOperand(new TimePredicate(p, iTime));
                    cfAnd.AddOperand(cfOneOf);
                }
            }
            return cfAnd.ToCNF();
        }

        */
        /* CompoundFormula ToTimeFormula(int iTime, HashSet<Predicate> lObserved, HashSet<Predicate> lUnknown)
        {
            //currently we will only do the conditional effects
            List<CompoundFormula> lConditions = GetConditions();
            CompoundFormula cfAnd = new CompoundFormula("and");
            Dictionary<Predicate, List<Formula>> dAdd = new Dictionary<Predicate, List<Formula>>();
            Dictionary<Predicate, List<Formula>> dDelete = new Dictionary<Predicate, List<Formula>>();
            List<Predicate> lEffects = new List<Predicate>();
            foreach (CompoundFormula cfCondition in lConditions)
            {
                //no need to add known effects into the belief
                if (!cfCondition.Operands[0].IsTrue(lObserved) && !cfCondition.Operands[0].IsFalse(lObserved))
                {
                    Formula fNotCondition = cfCondition.Operands[0].Reduce(lObserved).Negate();
                    HashSet<Predicate> lPredicates = cfCondition.Operands[1].GetAllPredicates();
                    foreach (Predicate p in lPredicates)
                    {
                        CompoundFormula cfOr = new CompoundFormula("or");
                        cfOr.AddOperand(fNotCondition.AddTime(iTime - 1));
                        cfOr.AddOperand(new TimePredicate(p, iTime));
                        cfAnd.AddOperand(cfOr);

                        Predicate pTrue = p;
                        if (p.Negation)
                            pTrue = p.Negate();
                        if (!dAdd.ContainsKey(pTrue))
                        {
                            dAdd[pTrue] = new List<Formula>();
                            dDelete[pTrue] = new List<Formula>();
                            lEffects.Add(pTrue);
                        }
                        if(p.Negation)
                            dDelete[pTrue].Add(cfCondition.Operands[0].Reduce(lObserved));
                        else
                            dAdd[pTrue].Add(cfCondition.Operands[0].Reduce(lObserved));
                    }
                }
            }
            foreach (Predicate p in lEffects)
            {
                CompoundFormula cfOr = new CompoundFormula("or");
                cfOr.AddOperand(new TimePredicate(p, iTime - 1));
                foreach (Formula f in dAdd[p])
                {
                    cfOr.AddOperand(f.AddTime(iTime - 1));
                }
                cfOr.AddOperand(new TimePredicate(p.Negate(), iTime));
                cfAnd.AddOperand(cfOr);

                cfOr = new CompoundFormula("or");
                cfOr.AddOperand(new TimePredicate(p.Negate(), iTime - 1));
                foreach (Formula f in dDelete[p])
                {
                    cfOr.AddOperand(f.AddTime(iTime - 1));
                }
                cfOr.AddOperand(new TimePredicate(p, iTime));
                cfAnd.AddOperand(cfOr);
            }

            HashSet<Predicate> lMandatory = GetMandatoryEffects();
            foreach (Predicate p in lUnknown)
            {
                if (!lEffects.Contains(p) && !lMandatory.Contains(p))
                {
                    CompoundFormula cfOneOf = new CompoundFormula("oneof");
                    cfOneOf.AddOperand(new TimePredicate(p.Negate(), iTime - 1));
                    cfOneOf.AddOperand(new TimePredicate(p, iTime));
                    cfAnd.AddOperand(cfOneOf);
                }
            }
            return (CompoundFormula)cfAnd.ToCNF();
        }*/

        public List<Action> SplitConflictingEffects()
        {
            List<Action> lActions = new List<Action>();
            if (!HasConditionalEffects)
            {
                lActions.Add(this);
                return lActions;
            }
            List<CompoundFormula> lConditions = GetConditions();
            Dictionary<Predicate, List<CompoundFormula>> dEffects = new Dictionary<Predicate, List<CompoundFormula>>();
            foreach (CompoundFormula cfCondition in lConditions)
            {
                HashSet<Predicate> lPredicates = cfCondition.Operands[1].GetAllPredicates();
                foreach (Predicate p in lPredicates)
                {
                    if (!dEffects.ContainsKey(p))
                        dEffects[p] = new List<CompoundFormula>();
                    dEffects[p].Add(cfCondition);
                }
            }
            List<Predicate> lConflicts = new List<Predicate>();
            foreach (Predicate p in dEffects.Keys)
            {
                if (dEffects.Keys.Contains(p.Negate()))
                {
                    if(!lConflicts.Contains(p.Canonical()))
                        lConflicts.Add(p.Canonical());
                }
            }
            int cActions = 0;
            Action aNew = Clone();
            aNew.Name += cActions;
            cActions++;
            aNew.Effects = new CompoundFormula("and");
            foreach (Formula fSub in ((CompoundFormula)Effects).Operands)
            {
                if (fSub is PredicateFormula)
                    ((CompoundFormula)aNew.Effects).AddOperand(fSub);
            }
            lActions.Add(aNew);

            foreach (Predicate pConflict in lConflicts)
            {

            }

            return lActions;
        }
        public override bool Equals(object obj)
        {
            if (obj is Action)
                return Name == ((Action)obj).Name;
            return false;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public Action ReplaceNonDeterministicEffectsWithOptions(List<string> lAlwaysKnown)
        {
            return ReplaceNonDeterministicEffectsWithOptions(lAlwaysKnown, -1);
        }
        public Action ReplaceNonDeterministicEffectsWithOptions(List<string> lAlwaysKnown, int cMaxOptions)
        {
            Action aNew = Clone();
            List<CompoundFormula> lConditions = new List<CompoundFormula>();
            List<Formula> lObligatory = new List<Formula>();
            SplitEffects(lConditions, lObligatory);

            //currently only handling non-determinism in conditional effects
            CompoundFormula cfEffects = new CompoundFormula("and");
            foreach (Formula f in lObligatory)
            {
                if (f is PredicateFormula)
                    cfEffects.AddOperand(f);
                else
                {
                    CompoundFormula cf = (CompoundFormula)f;
                    if (cf.Operator == "oneof" || cf.Operator == "or")
                    {
                        lConditions.Add(cf);
                    }
                    else
                        cfEffects.AddOperand(cf);

                }
            }

            foreach (CompoundFormula cfCondition in lConditions)
            {
                Formula fDeterministicCondition = null;
                if (cfCondition.ContainsNonDeterministicEffect())
                {
                    //BUGBUG - may cause problems when we have different number of options for each condition - not sure!
                    if (cMaxOptions < cfCondition.GetMaxNonDeterministicOptions())
                        cMaxOptions = cfCondition.GetMaxNonDeterministicOptions();
                    int[] aPermutation = Permutation(cMaxOptions);

                    foreach (int iOption in aPermutation)
                    {
                        fDeterministicCondition = cfCondition.ChooseOption(iOption);
                        GroundedPredicate gpOption = new GroundedPredicate(Domain.OPTION_PREDICATE);
                        gpOption.AddConstant(new Constant(Domain.OPTION, "opt" + iOption));
                        if (cfCondition.Operator == "when")
                        {
                            ((CompoundFormula)((CompoundFormula)fDeterministicCondition).Operands[0]).AddOperand(gpOption);
                            cfEffects.AddOperand(fDeterministicCondition);
                        }
                        else
                        {
                            CompoundFormula cfWhen = new CompoundFormula("when");
                            cfWhen.AddOperand(gpOption);
                            cfWhen.AddOperand(fDeterministicCondition);
                            cfEffects.AddOperand(cfWhen);

                        }
                    }
                }
                else
                    cfEffects.AddOperand(cfCondition);
            }
            if (!SDRPlanner.AllowChoosingNonDeterministicOptions)
            {
                for (int iOption = 0; iOption < cMaxOptions; iOption++)
                {
                    GroundedPredicate gpCurrentOption = new GroundedPredicate(Domain.OPTION_PREDICATE);
                    gpCurrentOption.AddConstant(new Constant(Domain.OPTION, "opt" + iOption));
                    GroundedPredicate gpNextOption = new GroundedPredicate(Domain.OPTION_PREDICATE);
                    gpNextOption.AddConstant(new Constant(Domain.OPTION, "opt" + (iOption + 1) % cMaxOptions));
                    CompoundFormula cfWhen = new CompoundFormula("when");
                    cfWhen.AddOperand(gpCurrentOption);
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    cfAnd.AddOperand(gpCurrentOption.Negate());
                    cfAnd.AddOperand(gpNextOption);
                    cfWhen.AddOperand(cfAnd);
                    cfEffects.AddOperand(cfWhen);
                }
            }
            aNew.Effects = cfEffects;
            if (Observe != null)//assuming that there can't be any observations
                throw new NotImplementedException();
            aNew.ContainsNonDeterministicEffect = false;
            return aNew;
        }

        private int[] Permutation(int cOptions)
        {
            int[] a = new int[cOptions];
            int i = 0, j = 0, aux = 0;
            for (i = 0; i < cOptions; i++)
                a[i] = i;
            for (i = 0; i < cOptions; i++)
            {
                i = RandomGenerator.Next(cOptions);
                j = RandomGenerator.Next(cOptions);
                aux = a[i];
                a[i] = a[j];
                a[j] = aux;
            }
            return a;
        }



        /*
        public void RemoveImpossibleOptions(IEnumerable<Predicate> lBefore, IEnumerable<Predicate> lAfter)
        {
            //incorrect - need to have both the state prior to the action and the state after the action. 
            if(Effects is CompoundFormula)
                Effects = ((CompoundFormula)Effects).RemoveImpossibleOptions(lObserved);       
        }
         * */

        public void IdentifyActivatedOptions(IEnumerable<Predicate> lBefore, IEnumerable<Predicate> lAfter)
        {
            //throw new NotImplementedException();
        }

        public Action ApplyOffline(IEnumerable<Predicate> lKnown)
        {
            if (Effects == null)
                return this;

            return null;
        }

        public Action ApplyObserved(IEnumerable<Predicate> lKnown)
        {
            if (Effects == null)
                return this;
            Action aTag = Clone();
            if (Original == null)
                aTag.Original = this;
            if (aTag.Effects != null)
            {
                aTag.Effects = Effects.ReduceConditions(lKnown);
                if (aTag.Effects != null)
                    aTag.HasConditionalEffects = aTag.Effects.ContainsCondition();
            }
            if (aTag.Preconditions != null)
                aTag.Preconditions = Preconditions;
            //aTag.Preconditions = Preconditions.Reduce(lKnown);

            if (m_mRegressions != null)
            {
                //aTag.m_mRegressions = m_mRegressions;
               

                aTag.m_mRegressions = new Dictionary<Predicate, Formula>();
                foreach (Predicate p in m_mRegressions.Keys)
                    aTag.m_mRegressions[p] = m_mRegressions[p].Reduce(lKnown);
                
            }
            return aTag;
        }
        public Action RemoveNonDeterminism(int iActionIndex, out CompoundFormula cfAndChoices)
        {
            Action aNew = Clone();
            int cChoices = 0;
            cfAndChoices = new CompoundFormula("and");
            CompoundFormula cfEffects = ((CompoundFormula)Effects).RemoveNonDeterminism(iActionIndex, ref cChoices, cfAndChoices);
            aNew.Effects = cfEffects;
            return aNew;
        }

        private bool CompareFormulas(Formula f1, Formula f2)
        {
            if (f1 == null && f2 == null)
                return true;
            if (f1 == null)
                return false;
            if (f2 == null)
                return false;
            return f1.Equals(f2);
        }

        public bool CompareTo(Action aOther)
        {
            if (Name != aOther.Name)
                return false;
            if (!CompareFormulas(Preconditions, aOther.Preconditions))
                return false;
            if (!CompareFormulas(Effects, aOther.Effects))
                return false;
            if (!CompareFormulas(Observe, aOther.Observe))
                return false;
            return true;
        }

        public Action RemoveUniversalQuantifiers(List<Constant> lConstants, List<Predicate> lConstantPredicates, Domain d)
        {
            Action aNew = Clone();
            
            if (Preconditions != null)
                aNew.Preconditions = Preconditions.RemoveUniversalQuantifiers(lConstants, lConstantPredicates, d);
            if (aNew.Preconditions is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)aNew.Preconditions).Predicate;
                if (p == Domain.TRUE_PREDICATE)
                    aNew.Preconditions = null;
                if (p == Domain.FALSE_PREDICATE)
                    return null;
            }
            if (Effects != null)
                aNew.Effects = Effects.RemoveUniversalQuantifiers(lConstants, lConstantPredicates, d);
            if (Observe != null)//assuming no universal quanitifiers in observe
                aNew.Observe = Observe;
            return aNew;
        }

        public void SimplifyConditions()
        {
            Debug.WriteLine("Converting action " + Name);
            if (Effects != null)
            {
                CompoundFormula cfNewEffects = new CompoundFormula("and");
                List<CompoundFormula> lConditions = new List<CompoundFormula>();
                List<Formula> lMandatory = new List<Formula>();
                SplitEffects(lConditions, lMandatory);
                foreach (Formula f in lMandatory)
                    cfNewEffects.SimpleAddOperand(f);
                int iCondition = 0, cConditions = lConditions.Count;
                foreach (CompoundFormula cfCondition in lConditions)
                {
                    if(cfCondition.Operands[0] is PredicateFormula)
                        cfNewEffects.SimpleAddOperand(cfCondition);
                    else
                    {
                        CompoundFormula cfNewCondition = new CompoundFormula("when");
                        CompoundFormula cfNewFirst = (CompoundFormula)((CompoundFormula)cfCondition.Operands[0]).ToCNF();
                        cfNewCondition.AddOperand(cfNewFirst);
                        cfNewCondition.AddOperand(cfCondition.Operands[1]);
                        cfNewEffects.SimpleAddOperand(cfNewCondition);
                    }
                    iCondition++;
                    Debug.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" + iCondition + "/" + cConditions);
                }
                Effects = cfNewEffects;
            }
        }

        public Formula RegressDet(Predicate p)
        {
            if (m_mRegressions != null)
            {
                if (m_mRegressions.ContainsKey(p))
                    return m_mRegressions[p];
                else
                    return new PredicateFormula(p);
            }
            return null;
        }
        public void ComputeRegressions()
        {
            if ( HasConditionalEffects)
            {
                m_mRegressions = new Dictionary<Predicate, Formula>();
                Dictionary<Predicate, List<Formula>> d = new Dictionary<Predicate, List<Formula>>();
                foreach (CompoundFormula cfCondition in GetConditions())
                {
                    HashSet<Predicate> lPredicates = cfCondition.Operands[1].GetAllPredicates();
                    foreach (Predicate p in lPredicates)
                    {
                        if (!d.ContainsKey(p))
                            d[p] = new List<Formula>();
                        d[p].Add(cfCondition.Operands[0]);
                    }
                }
                foreach (Predicate p in d.Keys)
                {
                    Predicate pNegate = p.Negate();
                    List<Formula> lAdd = d[p];
                    CompoundFormula cfOr = new CompoundFormula("or");

                    foreach (Formula f in lAdd)
                        cfOr.AddOperand(f.CreateRegression(p, -1));
                    cfOr.AddOperand(p);

                    if (d.ContainsKey(pNegate))
                    {
                        List<Formula> lRemove = d[pNegate];
                        CompoundFormula cfAndNot = new CompoundFormula("and");
                        foreach (Formula f in lRemove)
                            cfAndNot.AddOperand(f.CreateRegression(pNegate, -1).Negate());
                        cfAndNot.AddOperand(cfOr);
                        m_mRegressions[p] = cfAndNot;
                    }
                    else
                    {
                        m_mRegressions[p] = cfOr;

                        CompoundFormula cfAndNot = new CompoundFormula("and");
                        foreach (Formula f in lAdd)
                            cfAndNot.AddOperand(f.CreateRegression(pNegate, -1).Negate());
                        m_mRegressions[pNegate] = cfAndNot;
                    }
                }
            }
        }

        public List<Action> RemoveNonDeterministicEffects()
        {
            List<Action> lActions = new List<Action>();
            HashSet<Predicate> hsNonDetEffects = new HashSet<Predicate>();
            if (!ContainsNonDeterministicEffect)
            {
                lActions.Add(this);
                return lActions;
            }
            List<CompoundFormula> lConditions = GetConditions();

            if (lConditions.Count > 0)
                throw new NotImplementedException();
            if (Observe != null)
                throw new NotImplementedException();

            Action aNew = Clone();
            aNew.Effects = new CompoundFormula("and");
            lActions.Add(aNew);
            CompoundFormula cfEffects = (CompoundFormula)Effects;
            foreach (Formula f in cfEffects.Operands)
            {
                if (f is PredicateFormula)
                {
                    foreach (Action a1 in lActions)
                    {
                        ((CompoundFormula)a1.Effects).AddOperand(f);
                    }
                }
                else
                {
                    CompoundFormula cfNonDet = (CompoundFormula)f;
                    cfNonDet.GetAllPredicates(hsNonDetEffects);
                    List<Formula> lOptions = cfNonDet.GetAllOptions();
                    List<Action> lWithOptions = new List<Action>();
                    foreach (Formula fOption in lOptions)
                    {
                        foreach (Action a1 in lActions)
                        {
                            Action aNew1 = a1.Clone();
                            ((CompoundFormula)aNew1.Effects).AddOperand(fOption);
                            lWithOptions.Add(aNew1);

                        }

                    }
                    lActions = lWithOptions;
                }

            }
            for (int i = 0; i < lActions.Count; i++)
            {
                lActions[i].Name = lActions[i].Name + "_" + i;
                lActions[i].NonDeterministicEffects.UnionWith(hsNonDetEffects);
            }

            return lActions;
        }

        public void AddEffect(Predicate p)
        {   
            if (Effects == null)
            {
                PredicateFormula pf = new PredicateFormula(p);
                Effects = pf;
            }
            else if (Effects is PredicateFormula)
            {
                CompoundFormula cf = new CompoundFormula("and");
                cf.AddOperand(Effects);
                cf.AddOperand(new PredicateFormula(p));
            }
            else if (Effects is CompoundFormula)
            {
                ((CompoundFormula)Effects).AddOperand(new PredicateFormula(p));
            }
        }

        internal void AddConstraints(List<PredicateFormula> constraints)
        {
            if (Preconditions is CompoundFormula)
            {
                ((CompoundFormula)Preconditions).Operands.AddRange(constraints);
            }
            else
            {
                // if there is an exception here,
                // just add the constraints to the precondition of the action..
                throw new Exception();
            }
        }



        /*internal void SwitchAgent(string agent)
        {           
            Preconditions.ChangeAgent(agent);
            Effects.ChangeAgent(agent);
        }*/
        
        internal void RemoveConstant(Constant agent = null)
        {
            Preconditions.RemoveConstant(agent);
            Effects.RemoveConstant(agent);
        }

        internal void AddAgent(string agent)
        {
            Preconditions.AddAgent(agent);
            Effects.AddAgent(agent);
        }

        internal string GetOperationName()
        {
            //return Name;
            return Name.Split('_')[0];
        }

    }
}
