using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.Predicates;
using IMAP.Formulas;

namespace IMAP.General
{
    public class Problem
    {
        public string Name { get; private set; }
        public Formula Goal { get; set; }
        public Domain Domain{ get; private set;}

        private HashSet<Predicate> m_lKnown;
        private List<CompoundFormula> m_lHidden;
        public List<CompoundFormula> Hidden { get { return m_lHidden; } }
        public HashSet<Predicate> Known { get { return m_lKnown; } }
        public List<Action> ReasoningActions { get; private set; }
        public string MetricStatement { get; private set; }
        public string FilePath { get; set; }

        private HashSet<Predicate> m_lInitiallyUnknown;

        private Dictionary<GroundedPredicate, int> m_dMapPredicateToIndex;
        private List<GroundedPredicate> m_lIndexToPredicate;

        private Dictionary<GroundedPredicate,HashSet<GroundedPredicate>> m_dRelevantPredicates;

        public Problem(string sName, Domain d)
        {
            Domain = d;
            m_lKnown = new HashSet<Predicate>();
            m_lHidden = new List<CompoundFormula>();
            Name = sName;
            Goal = null;
            ReasoningActions = new List<Action>();
            m_dRelevantPredicates = new Dictionary<GroundedPredicate, HashSet<GroundedPredicate>>();
            m_lInitiallyUnknown = new HashSet<Predicate>();
            m_dMapPredicateToIndex = new Dictionary<GroundedPredicate, int>();
            m_lIndexToPredicate = new List<GroundedPredicate>();
        }

        public Problem(Problem baseProblem, Domain baseDomain)
        {
            // TODO: Complete member initialization
            Domain = baseDomain;
            Name = baseProblem.Name;
            Goal = baseProblem.Goal.Clone();
            MetricStatement = baseProblem.MetricStatement;
            m_lKnown = new HashSet<Predicate>();
            foreach (var known in baseProblem.m_lKnown)
            {
                m_lKnown.Add(known.Clone());
            }

            m_lHidden = new List<CompoundFormula>();
            foreach (Formula hidden in baseProblem.m_lHidden)
            {
                m_lHidden.Add((CompoundFormula)hidden.Clone());
            }

            foreach (var rAction in baseProblem.ReasoningActions)
            {
                ReasoningActions.Add(rAction.Clone());
            }

            m_dRelevantPredicates = new Dictionary<GroundedPredicate, HashSet<GroundedPredicate>>();
            foreach (var relevantPredicate in baseProblem.m_dRelevantPredicates)
            {
                m_dRelevantPredicates.Add(relevantPredicate.Key, new HashSet<GroundedPredicate>());
                foreach (var predicate in relevantPredicate.Value)
                {
                    m_dRelevantPredicates[relevantPredicate.Key].Add((GroundedPredicate)predicate.Clone());
                }
            }

            m_lInitiallyUnknown = new HashSet<Predicate>();
            foreach (var initKnown in baseProblem.m_lInitiallyUnknown)
            {
                m_lInitiallyUnknown.Add(initKnown.Clone());
            }


            m_dMapPredicateToIndex = new Dictionary<GroundedPredicate, int>();
            foreach (var mapPredicateIndex in baseProblem.m_dMapPredicateToIndex)
            {
                m_dMapPredicateToIndex.Add((GroundedPredicate)mapPredicateIndex.Key.Clone(), mapPredicateIndex.Value);
            }

            m_lIndexToPredicate = new List<GroundedPredicate>();
            foreach (var indexToPredicate in baseProblem.m_lIndexToPredicate)
            {
                m_lIndexToPredicate.Add((GroundedPredicate)indexToPredicate.Clone());
            }
        }
        public bool IsGoalState(State s)
        {
            return s.Contains(Goal);
        }

        public void AddKnown(Predicate p)
        {
            m_lKnown.Add(p);
        }
        public bool InitiallyUnknown(Predicate p)
        {
            return m_lInitiallyUnknown.Contains(p.Canonical());
        }
        public void AddHidden(CompoundFormula cf)
        {
            Domain.AddHidden(cf);

            HashSet<Predicate> hs = cf.GetAllPredicates();
            foreach (GroundedPredicate gp in hs)
            {
                m_lInitiallyUnknown.Add(gp.Canonical());
                GroundedPredicate gpCanonical = (GroundedPredicate)gp.Canonical();
                if (!m_dRelevantPredicates.ContainsKey(gpCanonical))
                    m_dRelevantPredicates[gpCanonical] = new HashSet<GroundedPredicate>();
                foreach (GroundedPredicate gpOther in hs)
                {
                    GroundedPredicate gpOtherCanonical = (GroundedPredicate)gpOther.Canonical();
                    if (gpOtherCanonical != gpCanonical)
                        m_dRelevantPredicates[gpCanonical].Add(gpOtherCanonical);
                }
                
            }

            m_lHidden.Add(cf);
        }
        public override string ToString()
        {
            string s = "(problem " + Name + "\n";
            s += "(domain " + Domain.Name + ")\n";
            s += "(init ";
            //s += "(known " + Parser.ListToString(m_lKnown) + ")\n";
            s += "(hidden " + Parser.ListToString(m_lHidden) + "))\n";
            s += ")";
            return s;
        }

        public void CompleteKnownState()
        {
            List<string> lKnownPredicates = new List<string>();
            foreach (Predicate p in m_lKnown)
                if (!lKnownPredicates.Contains(p.Name))
                    lKnownPredicates.Add(p.Name);
            // List<GroundedPredicate> lGrounded = Domain.GroundAllPredicates(lKnownPredicates);
            HashSet<GroundedPredicate> lGrounded = Domain.GroundAllPredicates();
            HashSet<Predicate> lUnknown = new HashSet<Predicate>();
            foreach (Formula f in m_lHidden)
                f.GetAllPredicates(lUnknown); 
            foreach (GroundedPredicate gp in lGrounded)
            {
                if (!(Domain.AlwaysConstant(gp) && Domain.AlwaysKnown(gp))) //not sure why I thouhgt that constant predicates do not apply here. We need them for planning in K domain.
                {
                    if (lUnknown.Contains(gp) || lUnknown.Contains(gp.Negate()) || m_lKnown.Contains(gp) || m_lKnown.Contains(gp.Negate()))
                    {
                        //do nothing
                    }
                    else
                    {

                        m_lKnown.Add(gp.Negate());
                    }
                }
            }
        }

        internal void SetGoals(List<Predicate> wantedGoals)
        {
            CompoundFormula cf = new CompoundFormula("and");
            foreach (var item in wantedGoals)
            {
                cf.AddOperand(new PredicateFormula(item));
            }
            Goal = cf;
        }

        public void AddReasoningActions()
        {
            ReasoningActions = new List<Action>();
            foreach (CompoundFormula cf in m_lHidden)
            {
                if (cf.Operator == "oneof")
                {
                    foreach (Formula f in cf.Operands)
                    {
                        if (cf.Operands.Count > 2)
                        {
                            CompoundFormula cfNegativeEffects = new CompoundFormula("and");
                            CompoundFormula cfPositiveEffects = new CompoundFormula("or");
                            foreach (Formula fOther in cf.Operands)
                            {
                                if (!fOther.Equals(f))
                                {
                                    cfNegativeEffects.AddOperand(f.Negate());
                                }
                                AddReasoningAction(f, cfNegativeEffects);
                            }
                        }
                        else
                        {
                            AddReasoningAction(cf.Operands[0], cf.Operands[1].Negate());
                            AddReasoningAction(cf.Operands[1], cf.Operands[0].Negate());
                        }
                    }
                }
                else
                    throw new NotImplementedException("Not implementing or for now");
            }
        }

        private void AddReasoningAction(Formula fPreconditions, Formula fEffect)
        {
            Action a = new Action("Reasoning" + ReasoningActions.Count);
            a.Preconditions = fPreconditions;
            a.SetEffects( fEffect);
            ReasoningActions.Add(a);
        }

        
        private void WriteResoningAction(StreamWriter sw, HashSet<Predicate> lPreconditions, HashSet<Predicate> lEffects, int iNumber)
        {
            sw.WriteLine("(:action R" + iNumber);
            sw.Write(":precondition (and");
            foreach (Predicate pPrecondition in lPreconditions)
            {
                sw.Write(pPrecondition);
            }
            sw.WriteLine(")");
            sw.Write(":effect (and");
            foreach (Predicate pEffect in lEffects)
            {
                sw.Write(pEffect);
            }
            sw.WriteLine(")");
            sw.WriteLine(")");
        }

        private void WriteResoningAxiom(StreamWriter sw, HashSet<Predicate> lPreconditions, HashSet<Predicate> lEffects, int iNumber)
        {
            sw.WriteLine("(:axiom");
            sw.Write(":context (and");
            foreach (Predicate pPrecondition in lPreconditions)
            {
                sw.Write(pPrecondition);
            }
            sw.WriteLine(")");
            sw.Write(":implies (and");
            foreach (KnowPredicate pEffect in lEffects)
            {
                sw.Write(pEffect);
            }
            sw.WriteLine(")");
            sw.WriteLine(")");
        }

        private void AddReasoningAction(HashSet<Predicate> lPreconditions, HashSet<Predicate> lEffects, Dictionary<List<Predicate>, List<Predicate>> dActions)
        {
            List<Predicate> lKnowPreconditions = new List<Predicate>();
            foreach (GroundedPredicate p in lPreconditions)
            {
                KnowPredicate pKnow = new KnowPredicate(p);
                lKnowPreconditions.Add(pKnow);
                lKnowPreconditions.Add(p);
            }
            List<Predicate> lKnowEffects = new List<Predicate>();
            foreach (GroundedPredicate p in lEffects)
            {
                KnowPredicate pKnow = new KnowPredicate(p);
                lKnowEffects.Add(pKnow);
            }
            if (dActions.ContainsKey(lKnowPreconditions))
            {
                if (dActions.Comparer.Equals(lKnowEffects, dActions[lKnowPreconditions]))
                    return;
                throw new NotImplementedException();
            }
            dActions[lKnowPreconditions] = lKnowEffects;
        }

        private void AddReasoningAction(List<GroundedPredicate> lAssignment, List<KnowPredicate> lKnown, List<Predicate> lEffects, Dictionary<List<Predicate>, List<Predicate>> dActions)
        {
            List<Predicate> lPreconditions = new List<Predicate>(lAssignment);
            lPreconditions.AddRange(lKnown);
            
            List<Predicate> lKnowEffects = new List<Predicate>();
            foreach (GroundedPredicate p in lEffects)
            {
                KnowPredicate pKnow = new KnowPredicate(p);
                lKnowEffects.Add(pKnow);
            }
            if (dActions.ContainsKey(lPreconditions))
            {
                if (dActions.Comparer.Equals(lKnowEffects, dActions[lPreconditions]))
                    return;
                throw new NotImplementedException();
            }
            dActions[lPreconditions] = lKnowEffects;
        }

        private void AddReasoningActions(Formula fUnknown, HashSet<Predicate> lUnassigned, HashSet<Predicate> lAssigned, Dictionary<List<Predicate>, List<Predicate>> dActions)
        {
            if (fUnknown is PredicateFormula)
            {
                HashSet<Predicate> lEffects = new HashSet<Predicate>();
                Predicate pEffect = ((PredicateFormula)fUnknown).Predicate;
                if (pEffect != Domain.TRUE_PREDICATE)
                {
                    lEffects.Add(pEffect);
                    AddReasoningAction(lAssigned, lEffects, dActions);
                }
                return;
            }
            CompoundFormula cfUnknown = (CompoundFormula)fUnknown;
            if (cfUnknown.Operator == "and")
            {
                bool bAllKnown = true;
                foreach (Formula f in cfUnknown.Operands)
                    if (f is CompoundFormula)
                        bAllKnown = false;
                if (bAllKnown)
                {
                    AddReasoningAction(lAssigned, lUnassigned, dActions);
                    return;
                }
            }
            Formula fReduced = null;
            foreach (Predicate p in lUnassigned)
            {
                HashSet<Predicate> lUnassignedReduced = new HashSet<Predicate>(lUnassigned);
                lUnassignedReduced.Remove(p);
                lAssigned.Add(p);
                fReduced = cfUnknown.Reduce(lAssigned);
                AddReasoningActions(fReduced, lUnassignedReduced, lAssigned, dActions);
                lAssigned.Remove(p);
                lAssigned.Add(p.Negate());
                fReduced = cfUnknown.Reduce(lAssigned);
                AddReasoningActions(fReduced, lUnassignedReduced, lAssigned, dActions);
                lAssigned.Remove(p.Negate());
            }
        }

        private bool IsRedundant(List<Predicate> lPreconditions, List<Predicate> lEffects, Dictionary<List<Predicate>, List<Predicate>> dActions)
        {
            if (lPreconditions.Count == 0)
                return false;
            else
            {
                foreach (Predicate p in lPreconditions)
                {
                    List<Predicate> lReduced = new List<Predicate>();
                    foreach (Predicate pOther in lPreconditions)
                        if (p != pOther)
                            lReduced.Add(pOther);
                    if (dActions.ContainsKey(lReduced))
                    {
                        List<Predicate> lContainingActionEffects = dActions[lReduced];
                        foreach (Predicate pEffect in lEffects)
                            if (!lContainingActionEffects.Contains(pEffect))
                                return false;
                        return true;
                    }
                    if (IsRedundant(lReduced, lEffects, dActions))
                        return true;
                }
                return false;
            }
        }
        private Dictionary<List<Predicate>, List<Predicate>> FilterRedundancies(Dictionary<List<Predicate>, List<Predicate>> dActions)
        {
            Dictionary<List<Predicate>, List<Predicate>> dFiltered = new Dictionary<List<Predicate>, List<Predicate>>();
            foreach (KeyValuePair<List<Predicate>, List<Predicate>> p in dActions)
            {
                if (!IsRedundant(p.Key, p.Value, dActions))
                    dFiltered[p.Key] = p.Value;
            }
            return dFiltered;
        }
        
        
        
        public void AddMetric(string sMetricStatement)
        {
            MetricStatement = sMetricStatement;
        }

        private string GenerateKnowGivenLine(GroundedPredicate gp, string sTag, bool bKnowWhether)
        {
            string sKP = "";
            if (bKnowWhether)
                sKP = "(KWGiven" + gp.Name;
            else
            {
                if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
                    sKP = "(KGiven" + gp.Name;
                else
                    sKP = "(Given" + gp.Name;

            }
            foreach (Constant c in gp.Constants)
            {
                sKP += " " + c.Name;
            }

            sKP += " " + sTag;

            if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
            {
                if (gp.Negation)
                    sKP += " " + Domain.FALSE_VALUE;
                else
                    sKP += " " + Domain.TRUE_VALUE;
            }

            return sKP + ")";
        }

        public List<PartiallySpecifiedState> GetInitialStates()
        {
            List<PartiallySpecifiedState> lpssInitialPossibleStates = new List<PartiallySpecifiedState>();
            BeliefState bsInitial = GetInitialBelief();//, bsCurrent = bsInitial, bsNext = null;
            PartiallySpecifiedState pssInitial = bsInitial.GetPartiallySpecifiedState();

            lpssInitialPossibleStates.Add(pssInitial);

            foreach (var hiddenItems in Hidden)
            {
                if (hiddenItems is CompoundFormula)
                {
                    List<PartiallySpecifiedState> stateAdditions = new List<PartiallySpecifiedState>();
                    foreach (var item in hiddenItems.Operands)
                    {
                        foreach (var pssCurrentCheckedState in lpssInitialPossibleStates)
                        {
                            PartiallySpecifiedState pssNew = pssCurrentCheckedState.Clone();
                            pssNew.AddObserved(item);
                            stateAdditions.Add(pssNew);
                        }
                    }
                    lpssInitialPossibleStates = stateAdditions;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return lpssInitialPossibleStates;
        }
        public BeliefState GetInitialBelief()
        {
            Debug.WriteLine("Generating initial belief state");
            BeliefState bs = new BeliefState(this);
            foreach (GroundedPredicate p in m_lKnown)
            {
                if (p.Name == "=")
                {
                    bs.FunctionValues[p.Constants[0].Name] = double.Parse(p.Constants[1].Name);
                }
                else
                    bs.AddObserved(p);
            }
            foreach (CompoundFormula cf in m_lHidden)
            {
                Formula fReduced = cf.Reduce(m_lKnown);
                if (fReduced is CompoundFormula)
                    bs.AddInitialStateFormula(((CompoundFormula)fReduced));
            }
            //bs.InitDNF();
            return bs;
        }


        public MemoryStream WriteKnowledgeProblem(HashSet<Predicate> lObserved, HashSet<Predicate> lHidden, int cMinMishaps, int cMishaps, bool bRemoveNegativePreconditions)
        {
            MemoryStream msProblem = new MemoryStream();
            StreamWriter sw = new StreamWriter(msProblem);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine(";;" + SDRPlanner.Translation);
            sw.WriteLine("(:init"); //ff doesn't like the and (and");

            string sKP = "", sP = "";

            HashSet<string> lNegativePreconditons = new HashSet<string>();
            if (bRemoveNegativePreconditions)
            {
                foreach (Predicate p in Domain.IdentifyNegativePreconditions())
                    lNegativePreconditons.Add(p.Name);
            }

            foreach (GroundedPredicate gp in lObserved)
            {
                if (gp.Name == "Choice")
                    continue;
                if (Domain.AlwaysKnown(gp))
                    sw.WriteLine(gp);
                if (!Domain.AlwaysKnown(gp))
                {
                    Predicate kp = new KnowPredicate(gp);
                    sw.WriteLine(kp);
                }
                if(gp.Negation && bRemoveNegativePreconditions && lNegativePreconditons.Contains(gp.Name))
                {
                    Predicate np = gp.Negate().Clone();
                    np.Name = "Not" + gp.Name;
                    sw.WriteLine(np);
                }
            }
            if (bRemoveNegativePreconditions)
            {
                foreach (GroundedPredicate gp in lHidden)
                {
                    Predicate nkp = gp.Clone();
                    nkp.Name = "NotK" + gp.Name;
                    sw.WriteLine(nkp);
                    Predicate nknp = gp.Clone();
                    nknp.Name = "NotKN" + gp.Name;
                    sw.WriteLine(nknp);
                }
            }

            if (cMinMishaps > cMishaps)
            {
                sw.WriteLine("(MishapCount m" + cMishaps + ")");
            }

            if (SDRPlanner.AddActionCosts)
            {
                sw.WriteLine("(= (total-cost) 0)");
            }

            sw.WriteLine(")");

            HashSet<Predicate> lGoalPredicates = Goal.GetAllPredicates();


            CompoundFormula cfGoal = new CompoundFormula("and");
            foreach (Predicate p in lGoalPredicates)
            {
                if (Domain.AlwaysKnown(p))
                    cfGoal.AddOperand(p);
                else
                    cfGoal.AddOperand(new KnowPredicate(p));
            }

            CompoundFormula cfAnd = new CompoundFormula(cfGoal);
            if (cMinMishaps > cMishaps && SDRPlanner.Translation != SDRPlanner.Translations.Conformant)
            {
                GroundedPredicate gp = new GroundedPredicate("MishapCount");
                gp.AddConstant(new Constant("mishaps", "m" + cMinMishaps));
                cfAnd.AddOperand(gp);
            }

            sw.WriteLine("(:goal " + cfAnd.Simplify() + ")");
            if (MetricStatement != null)
            {
                sw.WriteLine(MetricStatement);
            }

            sw.WriteLine(")");
            sw.Flush();


            return msProblem;
        }


        public MemoryStream WriteKnowledgeProblem(HashSet<Predicate> lObserved, HashSet<Predicate> lAllValues)
        {
            MemoryStream msProblem = new MemoryStream();
            StreamWriter sw = new StreamWriter(msProblem);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine(";;" + SDRPlanner.Translation);
            sw.WriteLine("(:init"); //ff doesn't like the and (and");


            foreach (GroundedPredicate gp in lObserved)
            {
                if (gp.Name == "Choice")
                    continue;
                sw.WriteLine(gp);
                if (!Domain.AlwaysKnown(gp))
                {
                    Predicate kp = new KnowPredicate(gp);
                    sw.WriteLine(kp);
                }

            }
            HashSet<Predicate> lHidden = new HashSet<Predicate>(lAllValues.Except(lObserved));
                


            foreach (GroundedPredicate gp in lHidden)
            {
                sw.WriteLine(gp);
            }

            

            sw.WriteLine(")");

            HashSet<Predicate> lGoalPredicates = Goal.GetAllPredicates();


            CompoundFormula cfGoal = new CompoundFormula("and");
            foreach (Predicate p in lGoalPredicates)
            {
                if (Domain.AlwaysKnown(p))
                    cfGoal.AddOperand(p);
                else
                    cfGoal.AddOperand(new KnowPredicate(p));
            }

            CompoundFormula cfAnd = new CompoundFormula(cfGoal);
            
            sw.WriteLine("(:goal " + cfAnd.Simplify() + ")");
            //sw.WriteLine("))");

            sw.WriteLine(")");
            sw.Flush();


            return msProblem;
        }


        public MemoryStream WriteTaggedProblem(Dictionary<string, List<Predicate>> dTags, CompoundFormula cfGoal, IEnumerable<Predicate> lObserved, 
                                        List<Predicate> lTrueState, Dictionary<string, double> dFunctionValues, bool bOnlyIdentifyStates)
        {
            MemoryStream msProblem = new MemoryStream();
            StreamWriter sw = new StreamWriter(msProblem);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine("(:init"); //ff doesn't like the and (and");

            string sKP = "", sP = "";
            if (Domain.TIME_STEPS > 0)
                sw.WriteLine("(time0)");
            if (SDRPlanner.SplitConditionalEffects)
                sw.WriteLine("(NotInAction)\n");
            foreach (KeyValuePair<string, double> f in dFunctionValues)
            {
                sw.WriteLine("(= " + f.Key + " " + f.Value + ")");
            }
            foreach (GroundedPredicate gp in lObserved)
            {
                if (gp.Name == "Choice")
                    continue;
                sKP = "(K" + gp.Name;
                sP = "(" + gp.Name;
                foreach (Constant c in gp.Constants)
                {
                    sKP += " " + c.Name;
                    sP += " " + c.Name;
                }
                if (gp.Negation)
                    sKP += " " + Domain.FALSE_VALUE;
                else
                    sKP += " " + Domain.TRUE_VALUE;
                if (!Domain.AlwaysKnown(gp))
                    sw.WriteLine(sKP + ")");
                if (!gp.Negation)
                    sw.WriteLine(sP + ")");
            }
            foreach (GroundedPredicate gp in lTrueState)
            {
                if (gp.Name == "Choice")
                    continue;
                if (!gp.Negation)
                {
                    sP = "(" + gp.Name;
                    foreach (Constant c in gp.Constants)
                    {
                        sP += " " + c.Name;
                    }
                    sw.WriteLine(sP + ")");
                }
            }
            foreach (KeyValuePair<string, List<Predicate>> p in dTags)
            {

                foreach (GroundedPredicate gp in p.Value)
                {
                    if (gp.Name == "Choice")
                        continue;
                    sKP = GenerateKnowGivenLine(gp, p.Key, false);
                    sw.WriteLine(sKP);
                }

                if (SDRPlanner.AddAllKnownToGiven)
                {
                    foreach (GroundedPredicate gp in lObserved)
                    {
                        if (gp.Name == "Choice")
                            continue;
                        if (!Domain.AlwaysKnown(gp))
                        {
                            sKP = GenerateKnowGivenLine(gp, p.Key, false);
                            sw.WriteLine(sKP);
                        }
                    }
                }

            }


            sw.WriteLine(")");

            HashSet<Predicate> lGoalPredicates = new HashSet<Predicate>();
            cfGoal.GetAllPredicates(lGoalPredicates);

            foreach (Predicate p in lGoalPredicates)
            {
                if (!Domain.AlwaysKnown(p))
                    cfGoal.AddOperand(new KnowPredicate(p));
            }
            
           
            sw.WriteLine("(:goal " + cfGoal + ")");
            //sw.WriteLine("))");
            if (MetricStatement != null)
            {
                sw.WriteLine(MetricStatement);
            }
            sw.WriteLine(")");
            sw.Flush();


            return msProblem;
        }

        public MemoryStream WriteTaggedProblem(Dictionary<string, List<Predicate>> dTags, IEnumerable<Predicate> lObserved,
                                        List<Predicate> lTrueState, Dictionary<string, double> dFunctionValues, bool bOnlyIdentifyStates)
        {
            MemoryStream msProblem = new MemoryStream();
            StreamWriter sw = new StreamWriter(msProblem);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine("(:init"); //ff doesn't like the and (and");

            string sKP = "", sP = "";
            if (Domain.TIME_STEPS > 0)
                sw.WriteLine("(time0)");
            if (SDRPlanner.SplitConditionalEffects)
                sw.WriteLine("(NotInAction)\n");
            foreach (KeyValuePair<string, double> f in dFunctionValues)
            {
                sw.WriteLine("(= " + f.Key + " " + f.Value + ")");
            }
            foreach (GroundedPredicate gp in lObserved)
            {
                if (gp.Name == "Choice" || gp.Name.ToLower().Contains(Domain.OPTION_PREDICATE))
                    continue;
                if (gp.Negation)
                    sKP = "(KN" + gp.Name;
                else
                    sKP = "(K" + gp.Name;
                sP = "(" + gp.Name;
                foreach (Constant c in gp.Constants)
                {
                    sKP += " " + c.Name;
                    sP += " " + c.Name;
                }
                /*
                if (gp.Negation)
                    sKP += " " + Domain.FALSE_VALUE;
                else
                    sKP += " " + Domain.TRUE_VALUE;
                    */
                if (!Domain.AlwaysKnown(gp))
                    sw.WriteLine(sKP + ")");
                if (!gp.Negation)
                    sw.WriteLine(sP + ")");
            }
            foreach (GroundedPredicate gp in lTrueState)
            {
                if (gp.Name == "Choice" || gp.Name.ToLower().Contains("_" + Domain.OPTION_PREDICATE))
                    continue;
                if (!gp.Negation)
                {
                    sP = "(" + gp.Name;
                    foreach (Constant c in gp.Constants)
                    {
                        sP += " " + c.Name;
                    }
                    sw.WriteLine(sP + ")");
                }
            }
            foreach (KeyValuePair<string, List<Predicate>> p in dTags)
            {

                foreach (GroundedPredicate gp in p.Value)
                {
                    if (gp.Name == "Choice" || gp.Name.ToLower().Contains("_" + Domain.OPTION_PREDICATE))
                        continue;
                    sKP = GenerateKnowGivenLine(gp, p.Key, false);
                    sw.WriteLine(sKP);
                }

                if (SDRPlanner.AddAllKnownToGiven)
                {
                    foreach (GroundedPredicate gp in lObserved)
                    {
                        if (gp.Name == "Choice" || gp.Name.ToLower().Contains("_" + Domain.OPTION_PREDICATE))
                            continue;
                        if (!Domain.AlwaysKnown(gp))
                        {
                            sKP = GenerateKnowGivenLine(gp, p.Key, false);
                            sw.WriteLine(sKP);
                        }
                    }
                }

            }


            sw.WriteLine(")");
            CompoundFormula cfGoal = new CompoundFormula("and");
            cfGoal.AddOperand(Goal);
            HashSet<Predicate> lGoalPredicates = new HashSet<Predicate>();
            cfGoal.GetAllPredicates(lGoalPredicates);

            foreach (Predicate p in lGoalPredicates)
            {
                if (!Domain.AlwaysKnown(p))
                    cfGoal.AddOperand(new KnowPredicate(p));
            }


            sw.WriteLine("(:goal " + cfGoal + ")");
            //sw.WriteLine("))");
            if (MetricStatement != null)
            {
                sw.WriteLine(MetricStatement);
            }
            sw.WriteLine(")");
            sw.Flush();


            return msProblem;
        }

        public MemoryStream WriteTaggedProblemNoState(Dictionary<string, List<Predicate>> dTags, IEnumerable<Predicate> lObserved,
                                                 Dictionary<string, double> dFunctionValues)
        {
            MemoryStream ms = new MemoryStream(1000);
            StreamWriter sw = new StreamWriter(ms);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine("(:init"); //ff doesn't like the and (and");

            string sKP = "";
            if (Domain.TIME_STEPS > 0)
                sw.WriteLine("(time0)");
            foreach (KeyValuePair<string, double> f in dFunctionValues)
            {
                sw.WriteLine("(= " + f.Key + " " + f.Value + ")");
            }
            foreach (GroundedPredicate gp in lObserved)
            {
                //if (gp.Negation)
                //    continue;
                if (gp.Name == "Choice" || gp.Name == Domain.OPTION_PREDICATE)
                    continue;
                if (Domain.AlwaysKnown(gp) && Domain.AlwaysConstant(gp))
                {
                    sKP = "(" + gp.Name;
                    foreach (Constant c in gp.Constants)
                    {
                        sKP += " " + c.Name;
                    }
                    sw.WriteLine(sKP + ")");
                }
                else
                {
                    foreach (string sTag in dTags.Keys)
                    {
                        if (!gp.Negation)
                        {
                            Predicate pGiven = gp.GenerateGiven(sTag);
                            sw.WriteLine(pGiven);
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, List<Predicate>> p in dTags)
            {

                foreach (GroundedPredicate gp in p.Value)
                {
                    if (gp.Negation)
                        continue;
                    if (gp.Name == "Choice")
                        continue;
                    if (!gp.Negation)
                    {
                        sw.WriteLine(gp.GenerateGiven(p.Key));
                    }
                    //sKP = GenerateKnowGivenLine(gp, p.Key, true);
                    //sw.WriteLine(sKP);
                }

             }

            //if (Problem.Domain.HasNonDeterministicActions())
            //    sw.WriteLine("(option opt0)");

            //if (SDRPlanner.SplitConditionalEffects)
                sw.WriteLine("(NotInAction)");

            sw.WriteLine(")");

            CompoundFormula cfGoal = new CompoundFormula("and");

            HashSet<Predicate> lGoalPredicates = new HashSet<Predicate>();
            Goal.GetAllPredicates(lGoalPredicates);


            for (int iTag = 0; iTag < dTags.Count; iTag++)
            {
                if (SDRPlanner.ConsiderStateNegations && iTag == dTags.Count - 1)
                    break;//What is that?
                string sTag = dTags.Keys.ElementAt(iTag);
                foreach (Predicate p in lGoalPredicates)
                {
                    if (!Domain.AlwaysKnown(p) || !Domain.AlwaysConstant(p))
                    {
                        cfGoal.AddOperand(p.GenerateGiven(sTag));
                    }
                }
            }

            if (SDRPlanner.ForceTagObservations)
            {
                foreach (string sTag1 in dTags.Keys)
                    foreach (string sTag2 in dTags.Keys)
                        if (sTag1 != sTag2)
                        {
                            Predicate gpNot = Predicate.GenerateKNot(new Constant(Domain.TAG, sTag1),new Constant(Domain.TAG, sTag2));
                            cfGoal.AddOperand(gpNot);
                        }
            }

            sw.WriteLine("(:goal " + cfGoal + ")");
            //sw.WriteLine("))");
            if (MetricStatement != null)
            {
                sw.WriteLine(MetricStatement);
            }
            sw.WriteLine(")");
            sw.Flush();

            return ms;
        }


        public MemoryStream WriteSimpleProblem(string sProblemFile, State sCurrent)
        {
            MemoryStream msProblem = new MemoryStream();
            StreamWriter sw = new StreamWriter(msProblem);
            sw.WriteLine("(define (problem K" + Name + ")");
            sw.WriteLine("(:domain K" + Domain.Name + ")");
            sw.WriteLine("(:init"); //ff doesn't like the and (and");
            string sP = "";
            foreach (GroundedPredicate gp in sCurrent.Predicates)
            {
                if (!gp.Negation)
                {
                    sP = "(" + gp.Name;
                    foreach (Constant c in gp.Constants)
                    {
                        sP += " " + c.Name;
                    }
                    sw.WriteLine("\t" + sP + ")");
                }
            }



            sw.WriteLine(")");

            

            sw.WriteLine("(:goal " + Goal + ")");
            //sw.WriteLine("))");

            sw.WriteLine(")");
            sw.Flush();

            if (SDRPlanner.UseFilesForPlanners)
            {
                bool bDone = false;
                while (!bDone)
                {
                    try
                    {
                        msProblem.Position = 0;
                        StreamReader sr = new StreamReader(msProblem);
                        StreamWriter swFile = new StreamWriter(sProblemFile);
                        swFile.Write(sr.ReadToEnd());
                        swFile.Close();
                        bDone = true;
                    }
                    catch (Exception e)
                    { }
                }

            }

            return msProblem;
        }

        public void RemoveUniversalQuantifiers()
        {
            Goal = Goal.RemoveUniversalQuantifiers(Domain.Constants, null, null);
        }

        public HashSet<GroundedPredicate> GetRelevantPredicates(GroundedPredicate gp)
        {
            if(m_dRelevantPredicates.ContainsKey(gp))
                return m_dRelevantPredicates[gp];
            return new HashSet<GroundedPredicate>();
        }

        public bool IsRelevantFor(GroundedPredicate gp, GroundedPredicate gpRelevant)
        {
            if (!m_dRelevantPredicates.ContainsKey((GroundedPredicate)gp.Canonical()))
                return false;
            return m_dRelevantPredicates[(GroundedPredicate)gp.Canonical()].Contains((GroundedPredicate)gpRelevant.Canonical());
        }


        public void ComputeRelevanceClosure()
        {
            bool bDone = false;
            while (!bDone)
            {
                bDone = true;
                foreach (GroundedPredicate gp in m_dRelevantPredicates.Keys)
                {
                    HashSet<Predicate> hsCurrentRelevant = new HashSet<Predicate>(m_dRelevantPredicates[gp]);
                    foreach (GroundedPredicate gpRelevant in hsCurrentRelevant)
                    {
                        foreach (GroundedPredicate gpOther in m_dRelevantPredicates[gpRelevant])
                        {
                            if(!gpOther.Equals(gp))
                                if (m_dRelevantPredicates[gp].Add(gpOther))
                                    bDone = false;
                        }
                    }

                }
            }
        }

        public int GetPredicateIndex(GroundedPredicate gp)
        {
            if (!m_dMapPredicateToIndex.ContainsKey(gp))
            {
                m_dMapPredicateToIndex[gp] = m_lIndexToPredicate.Count;
                m_lIndexToPredicate.Add(gp);
            }
            return m_dMapPredicateToIndex[gp];
        }
        public GroundedPredicate GetPredicateByIndex(int idx)
        {
            return m_lIndexToPredicate[idx];
        }

        internal void RemoveConstant(Constant agent)
        {
            HashSet<Predicate> newKnown = new HashSet<Predicate>();
            foreach (var item in m_lKnown)
            {
                if (!item.ContainsConstant(agent))
                    newKnown.Add(item);
            }
            m_lKnown = newKnown;
        }

        public void AddTime(int iMaxTimeLength)
        {
            GroundedPredicate gp;
            // add time adjucacny.. e.g. next-time t1 t2
            for (int i = 1; i < iMaxTimeLength; i++)
            {
                gp = new GroundedPredicate("next-time");
                gp.Constants.Add(new Constant("time", "t" + i));
                gp.Constants.Add(new Constant("time", "t" + (i + 1)));
                AddKnown(gp);
            }
            // Add infinite time. 
            gp = new GroundedPredicate("next-time");
            gp.Constants.Add(new Constant("time", "t" + iMaxTimeLength));
            gp.Constants.Add(new Constant("time", "t" + iMaxTimeLength));
            AddKnown(gp);

            gp = new GroundedPredicate("current-time");
            gp.Constants.Add(new Constant("time", "t1"));
            AddKnown(gp);
        }

        public List<Predicate> GetGoals()
        {
            List<Predicate> Goals = new List<Predicate>();
            Formula fGoal = Goal;
            if (fGoal is CompoundFormula)
            {
                CompoundFormula cfGoal = (CompoundFormula)fGoal;
                foreach (var item in cfGoal.Operands)
                {
                    if (item is PredicateFormula)
                    {
                        Goals.Add(((PredicateFormula)item).Predicate);
                    }
                }
            }
            else if (fGoal is PredicateFormula)
            {
                Goals.Add(((PredicateFormula)fGoal).Predicate);
            }
            else
            {
                throw new Exception();
            }
            return Goals;
        }
    }
}
