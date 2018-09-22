using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IMAP.SDRPlanners;
using IMAP.Predicates;
using IMAP.Formulas;


//using Microsoft.Z3;
//using Microsoft.SolverFoundation.Solvers;
//using Microsoft.SolverFoundation.Services;
//using Gurobi;
//using Microsoft.SolverFoundation.Solvers;
//using Microsoft.SolverFoundation.Services;
//using Google.OrTools;
//using Google.OrTools.ConstraintSolver;

namespace IMAP.General
{
    public class BeliefState
    {
        public IEnumerable<Predicate> Observed { get { return m_lObserved; } }
        public List<CompoundFormula> Hidden { get { return m_lHiddenFormulas; } }
        public HashSet<Predicate> Unknown{ get; private set;}   
        private List<CompoundFormula> m_lHiddenFormulas;
        private List<CompoundFormula> m_lOriginalHiddenFormulas;
        private Dictionary<GroundedPredicate, List<int>> m_dMapPredicatesToFormulas;

        private List<EfficientFormula> m_lEfficientHidden;
        private Dictionary<GroundedPredicate, int> m_dMapPredicatesToIndexes;
        private List<GroundedPredicate> m_dMapIndexToPredicate;
        private List<List<int>> m_dMapPredicateToEfficientFormula;

        private CompoundFormula m_cfCNFHiddenState;
        //protected List<Predicate> m_lObserved;
        protected HashSet<Predicate> m_lObserved;
        public List<Action> AvailableActions { get; protected set; }
        private BeliefState m_sPredecessor;
        public Problem Problem { get; private set; }
        public State UnderlyingEnvironmentState { get; set; }
        private List<List<Predicate>> m_lCurrentTags;
        private List<Predicate> m_lProblematicTag;
        public bool MaintainProblematicTag { get; set; }
        public string OutputType { get { return "SAS"; } }

        public static int bsCOUNT = 0;
        public int ID;

        public static bool UseEfficientFormulas = false;

        private int m_cNonDetChoices;

        public Dictionary<string, double> FunctionValues{ get; private set;}

        public BeliefState(Problem p)
        {
            Problem = p;
            m_sPredecessor = null;
            m_lObserved = new HashSet<Predicate>();
            Unknown = new HashSet<Predicate>();
            m_lHiddenFormulas = new List<CompoundFormula>();
            m_lOriginalHiddenFormulas = new List<CompoundFormula>();
            m_dMapPredicatesToFormulas = new Dictionary<GroundedPredicate, List<int>>();

            m_lEfficientHidden = new List<EfficientFormula>();
            m_dMapPredicatesToIndexes = new Dictionary<GroundedPredicate, int>();
            m_dMapIndexToPredicate = new List<GroundedPredicate>();
            m_dMapPredicateToEfficientFormula = new List<List<int>>();

            AvailableActions = new List<Action>();
            UnderlyingEnvironmentState = null;
            m_cfCNFHiddenState = new CompoundFormula("and");
            FunctionValues = new Dictionary<string, double>();
            foreach (string sFunction in Problem.Domain.Functions)
            {
                FunctionValues[sFunction] = 0.0;
            }

            bsCOUNT++;
            ID = bsCOUNT;
            if (ID == 75)
                Console.Write("*");
            m_cNonDetChoices = 0;
        }
        public BeliefState(BeliefState sToCopy)
            : this(sToCopy.Problem)
        {
            m_lHiddenFormulas = new List<CompoundFormula>(sToCopy.Hidden);
            m_lOriginalHiddenFormulas = new List<CompoundFormula>(sToCopy.m_lOriginalHiddenFormulas);
            m_dMapPredicatesToFormulas = new Dictionary<GroundedPredicate, List<int>>();
            foreach (KeyValuePair<GroundedPredicate, List<int>> pair in sToCopy.m_dMapPredicatesToFormulas)
            {
                m_dMapPredicatesToFormulas[pair.Key] = new List<int>();
                foreach (int i in pair.Value)
                {
                    m_dMapPredicatesToFormulas[pair.Key].Add(i);
                }
            }
            m_dMapPredicatesToIndexes = new Dictionary<GroundedPredicate, int>(sToCopy.m_dMapPredicatesToIndexes);
            m_dMapPredicateToEfficientFormula = new List<List<int>>();
            foreach (List<int> list in sToCopy.m_dMapPredicateToEfficientFormula)
            {
                List<int> newList = new List<int>();
                foreach (int i in list)
                {
                    newList.Add(i);
                }
                m_dMapPredicateToEfficientFormula.Add(newList);
            }
            m_lObserved = new HashSet<Predicate>(sToCopy.Observed);
            Unknown = new HashSet<Predicate>(sToCopy.Unknown);
            m_cfCNFHiddenState = sToCopy.m_cfCNFHiddenState;

            /*m_sPredecessor = sToCopy.m_sPredecessor;
            AvailableActions = new List<Action>(sToCopy.AvailableActions);
            m_dMapIndexToPredicate = new List<GroundedPredicate>(sToCopy.m_dMapIndexToPredicate);
            UnderlyingEnvironmentState = sToCopy.UnderlyingEnvironmentState;
            if (sToCopy.m_lProblematicTag != null) m_lProblematicTag = new List<Predicate>(sToCopy.m_lProblematicTag);
            m_lCurrentTags = new List<List<Predicate>>();
            foreach (List<Predicate> lp in sToCopy.m_lCurrentTags)
            {
                m_lCurrentTags.Add(new List<Predicate>(lp));
            }*/

            m_dSATVariables = sToCopy.m_dSATVariables;
            m_lSATVariables = sToCopy.m_lSATVariables;

            bsCOUNT++;
            ID = bsCOUNT;
            m_cNonDetChoices = sToCopy.m_cNonDetChoices;
        }

        public bool ConsistentWith(Predicate p, bool bConsiderHiddenState)
        {
            foreach (Predicate pState in Observed)
            {
                if (!p.ConsistentWith(pState))
                    return false;
            }
            if (bConsiderHiddenState)
            {
                //need to also check whether p is consistent with the hidden state
                /*
                List<CompoundFormula> lReducedHidden = new List<CompoundFormula>();
                List<Predicate> lKnown = new List<Predicate>(Observed);
                lKnown.Add(p);
                foreach (CompoundFormula cfHidden in m_lHidden)
                {
                    Formula cfReduced = cfHidden.Reduce(lKnown);
                    if (cfReduced.IsFalse(lKnown))
                        return false;
                    if (cfReduced is PredicateFormula)
                    {
                        Predicate pReduced = ((PredicateFormula)cfReduced).Predicate;
                        lKnown.Add(pReduced);
                    }
                    else
                        lReducedHidden.Add((CompoundFormula)cfReduced);
                }
                if (RunSatSolver(lReducedHidden, 1).Count == 0)
                    return false;
                 * */
                CompoundFormula cfCNF = (CompoundFormula)m_cfCNFHiddenState.Clone();
                cfCNF.AddOperand(p);
                throw new NotImplementedException();
                List<List<Predicate>> lConsistentAssignments = null;// RunSatSolver(cfCNF, 1);
                if (lConsistentAssignments.Count == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool ConsistentWith(State s)
        {
            foreach (Predicate pState in s.Predicates)
            {
                if (!ConsistentWith(pState, false))
                    return false;
            }
            return true;
        }

        public bool ConsistentWith(Formula fOriginal)
        {
            /*
            CompoundFormula cfCNF = (CompoundFormula)m_cfCNFHiddenState.Clone();
            cfCNF.AddOperand(f);
            cfCNF = cfCNF.ToCNF();
            if (cfCNF.IsFalse(Observed))
            {
                MaintainProblematicTag = false;
                return false;
            }
             * */
            Formula f = fOriginal.Reduce(Observed);
            /*
            if (f.Contains(Domain.FALSE_PREDICATE))
                return false;
            if (f.Contains(Domain.TRUE_PREDICATE))
                return true;
                */
            CompoundFormula cf = null;
            List<PredicateFormula> lPredicates = new List<PredicateFormula>();
            List<CompoundFormula> lFormulas = new List<CompoundFormula>();
            if (f is PredicateFormula)
            {
                lPredicates.Add((PredicateFormula)f);
            }
            else
            {
                cf = (CompoundFormula)f;
                CompoundFormula cfCNF = (CompoundFormula)cf.ToCNF();
                if (cfCNF.Operator == "and")
                {
                    foreach (Formula fSub in cfCNF.Operands)
                    {
                        if (fSub is PredicateFormula)
                            lPredicates.Add((PredicateFormula)fSub);
                        else
                            lFormulas.Add((CompoundFormula)fSub);
                    }
                }
                else
                    lFormulas.Add(cfCNF);

            }

            if (lFormulas.Count == 0)
            {
                HashSet<Predicate> lAssignment = new HashSet<Predicate>();
                List<Formula> lHidden = new List<Formula>(m_lHiddenFormulas);
                foreach (PredicateFormula pf in lPredicates)
                {
                    lHidden.Add(pf);
                }
                bool bValid = ApplyUnitPropogation(lHidden, lAssignment);
                return bValid;
            }
            else
            {
                if (lPredicates.Count > 0)
                {
                    HashSet<int> lIndexes = new HashSet<int>();
                    List<Predicate> lKnown = new List<Predicate>();
                    foreach (PredicateFormula pf in lPredicates)
                    {
                        if (m_dMapPredicatesToFormulas.ContainsKey((GroundedPredicate)pf.Predicate.Canonical()))
                        {
                            lKnown.Add(pf.Predicate);
                            foreach (int idx in m_dMapPredicatesToFormulas[(GroundedPredicate)pf.Predicate.Canonical()])
                                lIndexes.Add(idx);
                        }
                    }
                    foreach (int idx in lIndexes)
                    {
                        if (m_lHiddenFormulas[idx] != null && m_lHiddenFormulas[idx].IsFalse(lKnown))
                            return false;
                    }

                }
                if (lFormulas.Count > 0)
                {
                    List<Formula> lHidden = new List<Formula>(m_lHiddenFormulas);
                    lHidden.AddRange(lFormulas);
                    lHidden.AddRange(lPredicates);
                    List<List<Predicate>> lConsistentAssignments = RunSatSolver(lHidden, 1);
                    if (MaintainProblematicTag)
                        m_lProblematicTag = null;
                    if (lConsistentAssignments.Count > 0)
                    {
                        if (MaintainProblematicTag)
                        {
                            m_lProblematicTag = lConsistentAssignments[0];
                        }
                    }
                    else
                    {
                        //we have just discovered something that is inconsistent with the initial belief, so we can add its negation to the initial belief
                        AddReasoningFormula(f.Negate(), new HashSet<int>());
                    }
                    MaintainProblematicTag = false;
                    return lConsistentAssignments.Count > 0;
                }
            }
            if (false)//for debugging the ongoing reduction of hidden formulas given observations - here I use the original formulas + the observed
            {
                List<Formula> lHidden = new List<Formula>(m_lOriginalHiddenFormulas);
                foreach (GroundedPredicate gp in Observed)
                    lHidden.Add(new PredicateFormula(gp));
                lHidden.AddRange(lPredicates);
                List<List<Predicate>> lConsistentAssignments = RunSatSolver(lHidden, 1);
                if (MaintainProblematicTag)
                    m_lProblematicTag = null;
                if (lConsistentAssignments.Count > 0)
                {
                    if (MaintainProblematicTag)
                    {
                        m_lProblematicTag = lConsistentAssignments[0];
                        /*
                        foreach (CompoundFormula cfHidden in m_lOriginalHiddenFormulas)
                        {
                            if (cfHidden.ToString().Contains("obs1-at p2_1"))
                                    Console.WriteLine("*");
                            Formula fReduced = cfHidden.Reduce(m_lProblematicTag);
                            if (fReduced is PredicateFormula)
                            {
                                PredicateFormula pf = (PredicateFormula)fReduced;
                                if (pf.Predicate.Name == "P_FALSE")
                                    Console.WriteLine("*");
                                else if (pf.Predicate.Name != "P_TRUE" && !pf.Predicate.Negation)
                                    Console.WriteLine(pf);
                                 
                            }
                        }
                         * */
                    }
                }
                MaintainProblematicTag = false;
                return lConsistentAssignments.Count > 0;
            }
            return true;
        }
        public bool AddObserved(Predicate p)
        {
            bool bNew = false;
            if (p == Domain.TRUE_PREDICATE)
                return false;
            Debug.Assert(p != Domain.FALSE_PREDICATE, "Adding P_FALSE");

#if DEBUG
            if (UnderlyingEnvironmentState != null && 
                ((p.Negation == false && !UnderlyingEnvironmentState.Contains(p)) || 
                (p.Negation == true && UnderlyingEnvironmentState.Contains(p.Negate()))))
                Console.WriteLine("Adding a predicate that doesn't exist");
#endif 

            Unknown.Remove(p.Canonical());
            if (!m_lObserved.Contains(p))
            {
                Predicate pNegate = p.Negate();
                if(m_lObserved.Contains(pNegate))
                    m_lObserved.Remove(pNegate);
                bNew = true;
            }
            m_lObserved.Add(p);
            return bNew;
        }
        public void AddObserved(Formula f)
        {
            if (f is PredicateFormula)
                AddObserved(((PredicateFormula)f).Predicate);
            else
            {
                CompoundFormula cf = (CompoundFormula)f;
                if (cf.Operator == "and")
                    foreach (Formula fSub in cf.Operands)
                        AddObserved(fSub);
                else
                    throw new NotImplementedException();
            }
        }

        public void ToSimpleForm()
        {
            List<CompoundFormula> lHidden = new List<CompoundFormula>();
            foreach (CompoundFormula cfHidden in m_lHiddenFormulas)
            {
                lHidden.Add(cfHidden.ToSimpleForm());
            }
            m_lHiddenFormulas = lHidden;
        }

        public override bool Equals(object obj)
        {
            if (obj is BeliefState)
            {
                BeliefState bs = (BeliefState)obj;
                if (bs.m_lObserved.Count != m_lObserved.Count)
                    return false;
                if (bs.m_lHiddenFormulas.Count != m_lHiddenFormulas.Count)
                    return false;
                foreach (Predicate p in bs.Observed)
                    if (!m_lObserved.Contains(p))
                        return false;
                foreach (Formula f in bs.m_lHiddenFormulas)
                    if (!m_lHiddenFormulas.Contains(f))
                        return false;
                return true;
            }


            return false;
        }

        public bool Contains(Formula f)
        {
            return f.ContainedIn(Observed, true);
        }

        private bool AllObserved(Formula f)
        {
            if (f is PredicateFormula)
            {
                Predicate p = ((PredicateFormula)f).Predicate;
                if (Observed.Contains(p))
                    return true;
                return false;
            }
            else
            {
                CompoundFormula cf = (CompoundFormula)f;
                bool bObserved = false;
                foreach (Formula fOperand in cf.Operands)
                {
                    bObserved = AllObserved(fOperand);
                    if (cf.Operator == "or" && bObserved)
                        return true;
                    if (cf.Operator == "and" && !bObserved)
                        return false;
                }
                return cf.Operator == "and";
            }
        }

        public virtual BeliefState Clone()
        {
            return new BeliefState(this);
        }
        public static TimeSpan tsTotalTime = new TimeSpan();

        private bool Contains(CompoundFormula cf)
        {
            HashSet<Predicate> lPredicates = cf.GetAllPredicates();
            Predicate pCanonical = null;
            foreach (Predicate p in lPredicates)
            {
                pCanonical = p.Canonical();
                if (!m_dMapPredicatesToFormulas.ContainsKey((GroundedPredicate)pCanonical))
                    return false;
            }
            List<int> lFormulas = m_dMapPredicatesToFormulas[(GroundedPredicate)pCanonical];
            foreach (int idx in lFormulas)
                if (m_lHiddenFormulas[idx] != null && m_lHiddenFormulas[idx].Equals(cf))
                    return true;
            return false;
        }


        public HashSet<Predicate> AddReasoningFormula(Formula f, HashSet<int> hsModifiedClauses)
        {
            DateTime dtStart = DateTime.Now;
            bool bLearnedNewPredicate = false;
            HashSet<Predicate> lAllLearnedPredicates = new HashSet<Predicate>();
            List<PredicateFormula> lLearnedPredicates = new List<PredicateFormula>();
            if (f is CompoundFormula)
            {
                CompoundFormula cf = (CompoundFormula)f.ToCNF();
                List<CompoundFormula> lFormulas = new List<CompoundFormula>();
                if (cf.Operator == "and")
                {
                    foreach (Formula fSub in cf.Operands)
                    {
                        if (fSub is PredicateFormula)
                            lLearnedPredicates.Add((PredicateFormula)fSub);
                        else
                            lFormulas.Add((CompoundFormula)fSub);
                    }
                }
                else
                {
                    lFormulas.Add(cf);
                }

                foreach (CompoundFormula cfSub in lFormulas)
                {
                    if (!Contains(cfSub))
                    {
                        AddInitialStateFormula(cfSub);
                    }
                    m_cfCNFHiddenState.SimpleAddOperand(cfSub);
                }
            }
            else
            {
                lLearnedPredicates.Add((PredicateFormula)f);
            }
            int cIndexes = 0, cValidIndexes = 0, cLearned = 0;
            DateTime dt1 = DateTime.Now;
            TimeSpan ts1 = new TimeSpan(0), ts2 = new TimeSpan(0), ts3 = new TimeSpan(0);


            int cReductions = 0;
            while (lLearnedPredicates.Count > 0)
            {
                bLearnedNewPredicate = true;
                HashSet<int> lIndexes = new HashSet<int>();
                List<Predicate> lKnown = new List<Predicate>();
                foreach (PredicateFormula pf in lLearnedPredicates)
                {
                    GroundedPredicate p = (GroundedPredicate)pf.Predicate.Canonical();
                    if (AddObserved(pf.Predicate))
                    {
                        lAllLearnedPredicates.Add(pf.Predicate);
                        cLearned++;
                    }
                    lKnown.Add(pf.Predicate);

                    if (m_dMapPredicatesToFormulas.ContainsKey(p))
                    {
                        List<int> lFormulas = m_dMapPredicatesToFormulas[p];
                        foreach (int idx in lFormulas)
                            lIndexes.Add(idx);
                        m_dMapPredicatesToFormulas[p] = new List<int>();
                    }
                }
                DateTime dt2 = DateTime.Now;
                ts1 += dt2 - dt1;
                dt1 = dt2;
                lLearnedPredicates = new List<PredicateFormula>();
                foreach (int idx in lIndexes)
                {
                    cIndexes++;
                    CompoundFormula cfPrevious = m_lHiddenFormulas[idx];

                    if (cfPrevious != null)
                    {
                        hsModifiedClauses.Add(idx);
                        cValidIndexes++;

                        DateTime dt3 = DateTime.Now;
                        Formula fNew = cfPrevious.Reduce(lKnown);
                        cReductions++;
                        /*
                        if (ID == 361 && idx == 77)
                            Console.Write("*");
                        if (idx == 77 && fNew.IsTrue(null))
                            Console.Write("*");
                         * */
                        ts2 += DateTime.Now - dt3;

                        if (fNew is PredicateFormula)
                        {
                            m_lHiddenFormulas[idx] = null;
                            if(!fNew.IsTrue(null))
                                lLearnedPredicates.Add((PredicateFormula)fNew);
                        }
                        else
                        {
                            CompoundFormula cfNew = (CompoundFormula)fNew;
                            if (cfNew.IsSimpleConjunction())
                            {
                                foreach (PredicateFormula pf in cfNew.Operands)
                                {
                                    if (!fNew.IsTrue(null))
                                        lLearnedPredicates.Add(pf);
                                }
                                m_lHiddenFormulas[idx] = null;
                            }
                            else
                                m_lHiddenFormulas[idx] = cfNew;
                        }
                    }
                }
                dt2 = DateTime.Now;
                ts3 += dt2 - dt1;
                dt1 = dt2;

            }

            

          
            TimeSpan tsCurrent = DateTime.Now - dtStart;
            tsTotalTime += tsCurrent;
            //Debug.WriteLine("AddReasoningFormula: indexes " + cIndexes + ", valid " + cValidIndexes + ", learned " + cLearned + " time " + tsCurrent.TotalSeconds);
            //Debug.WriteLine(cReductions + ", " + ts1.TotalSeconds + ", " + ts2.TotalSeconds + ", " + ts3.TotalSeconds);
            return lAllLearnedPredicates;
        }

        public int NextNonDetChoice()
        {
            int idx = m_cNonDetChoices;
            //Console.WriteLine("bs" + ID + " non det " + idx);
            m_cNonDetChoices++;
            return idx;
        }

        public HashSet<Predicate> AddReasoningFormulaEfficient(Formula f)
        {
            throw new NotImplementedException();
            DateTime dtStart = DateTime.Now;
            List<Predicate> lAllLearnedPredicates = new List<Predicate>();
            bool bLearnedNewPredicate = false;
            List<PredicateFormula> lLearnedPredicates = new List<PredicateFormula>();
            if (f is CompoundFormula)
            {
                CompoundFormula cf = (CompoundFormula)f;
                List<CompoundFormula> lFormulas = new List<CompoundFormula>();
                if (cf.Operator == "and")
                {
                    foreach (Formula fSub in cf.Operands)
                    {
                        if (fSub is PredicateFormula)
                        {
                            PredicateFormula pf = (PredicateFormula)fSub;
                            lLearnedPredicates.Add(pf);
                            lAllLearnedPredicates.Add(pf.Predicate);
                        }
                        else
                            lFormulas.Add((CompoundFormula)fSub);
                    }
                }
                else
                {
                    lFormulas.Add(cf);
                }

                foreach(CompoundFormula cfSub  in lFormulas)
                {
                    if (!Contains(cfSub))
                    {
                        AddInitialStateFormula(cfSub);
                    }
                    m_cfCNFHiddenState.SimpleAddOperand(cfSub);
                }
            }
            else
            {
                lLearnedPredicates.Add((PredicateFormula)f);
            }
            int cIndexes = 0, cValidIndexes = 0, cLearned = 0;
            //DateTime dt1 = DateTime.Now;
            //TimeSpan ts1 = new TimeSpan(0), ts2 = new TimeSpan(0), ts3 = new TimeSpan(0);
            int cReductions = 0;
            while (lLearnedPredicates.Count > 0)
            {
                bLearnedNewPredicate = true;
                List<Predicate> lKnown = new List<Predicate>();
                List<int> lKnownAssignments = new List<int>();
                HashSet<int> lIndexes = new HashSet<int>();
                foreach (PredicateFormula pf in lLearnedPredicates)
                {
                    GroundedPredicate pCanonical = (GroundedPredicate)pf.Predicate.Canonical();
                    if (AddObserved(pf.Predicate))
                        cLearned++;
                    lKnown.Add(pf.Predicate);
                    if (m_dMapPredicatesToIndexes.ContainsKey(pCanonical))
                    {
                        int iPredicateIndex = m_dMapPredicatesToIndexes[(GroundedPredicate)pCanonical];
                        if (pf.Predicate.Negation)
                            lKnownAssignments.Add(iPredicateIndex * 2 + 1);
                        else
                            lKnownAssignments.Add(iPredicateIndex * 2);

                        List<int> lFormulas = m_dMapPredicateToEfficientFormula[iPredicateIndex];
                        if (lFormulas != null)
                        {
                            foreach (int idx in lFormulas)
                                lIndexes.Add(idx);
                        }
                        m_dMapPredicateToEfficientFormula[iPredicateIndex] = null;
                    }
                }

                //DateTime dt2 = DateTime.Now;
                //ts1 += dt2 - dt1;
                //dt1 = dt2;
                lLearnedPredicates = new List<PredicateFormula>();
                List<int> lNewAssignments = new List<int>();
                foreach (int idx in lIndexes)
                {
                    cIndexes++;
                    EfficientFormula efCurrent = m_lEfficientHidden[idx];
                    if (efCurrent != null && !efCurrent.IsTrue() && !efCurrent.IsFalse())
                    {
                        cValidIndexes++;
                        //DateTime dt3 = DateTime.Now;
                        foreach (int iAssignment in lKnownAssignments)
                        {
                            int iPredicate = iAssignment / 2;
                            bool bPositive = iAssignment % 2 == 0;
                            if (efCurrent.Reduce(iPredicate, bPositive, lNewAssignments))
                            {
                                //not sure what to do here
                                m_lEfficientHidden[idx] = null;
                                break;
                            }
                        }


                        cReductions++;
                        //ts2 += DateTime.Now - dt3;
                    }
                }
                foreach (int iAssignment in lNewAssignments)
                {
                    int iPredicate = iAssignment / 2;

                    GroundedPredicate p = new GroundedPredicate(m_dMapIndexToPredicate[iPredicate]);
                    if (iAssignment % 2 == 1)
                        p = (GroundedPredicate)p.Negate();
                    lLearnedPredicates.Add(new PredicateFormula(p));
                    lAllLearnedPredicates.Add(p);
                    //Console.WriteLine("Learned: " + p);

                }

                //dt2 = DateTime.Now;
                //ts3 += dt2 - dt1;
                //dt1 = dt2;

            }

            
            //TimeSpan tsCurrent = DateTime.Now - dtStart;
            //tsTotalTime += tsCurrent;
            //Console.WriteLine("AddReasoningFormula: indexes " + cIndexes + ", valid " + cValidIndexes + ", learned " + cLearned + " time " + tsCurrent.TotalSeconds);
            //Console.WriteLine(cReductions + ", " + ts1.TotalSeconds + ", " + ts2.TotalSeconds + ", " + ts3.TotalSeconds);
            /*
            List<int> lRegularFormulaIndexes = new List<int>();
            foreach (Predicate p in lAllLearnedPredicates)
            {
                GroundedPredicate pCanonical = (GroundedPredicate)p.Canonical();
                if (m_dMapPredicatesToFormulas.ContainsKey(pCanonical))
                {
                    List<int> lFormulas = m_dMapPredicatesToFormulas[pCanonical];
                    foreach (int idx in lFormulas)
                        lIndexes.Add(idx);
                    m_dMapPredicatesToFormulas[pCanonical] = new List<int>();
                }
            }
            */


            //BUGBUG: need to implement returning all learned predicates
            return null;
        }

        public void AddInitialStateFormula(CompoundFormula cf)
        {

            if (false && !cf.IsSimpleFormula())
            {
                CompoundFormula cfCNF = (CompoundFormula)cf.ToCNF();
                foreach (CompoundFormula cfSub in cfCNF.Operands)
                    AddInitialStateFormula(cfSub);
                return;
            }

            m_lOriginalHiddenFormulas.Add(cf);
            m_lHiddenFormulas.Add((CompoundFormula)cf);
            EfficientFormula ef = new EfficientFormula(cf.Operator);
            ef.OriginalFormula = cf;
            m_lEfficientHidden.Add(ef);
            HashSet<Predicate> lHidden = cf.GetAllPredicates();
            foreach (Predicate p in lHidden)
            {
                GroundedPredicate pCanonical = (GroundedPredicate)p.Canonical();
                if (!Unknown.Contains(pCanonical))
                    Unknown.Add(pCanonical);
                if (!m_dMapPredicatesToFormulas.ContainsKey(pCanonical))
                {
                    m_dMapPredicatesToFormulas[pCanonical] = new List<int>();

                    int iIndex = m_dMapIndexToPredicate.Count;
                    m_dMapIndexToPredicate.Add(pCanonical);
                    m_dMapPredicatesToIndexes[pCanonical] = iIndex;
                    m_dMapPredicateToEfficientFormula.Add(new List<int>());
                }

                int iPredicate = m_dMapPredicatesToIndexes[pCanonical];

                ef.SetVariableValue(iPredicate, !p.Negation);
                m_dMapPredicateToEfficientFormula[iPredicate].Add(m_lEfficientHidden.Count - 1);

                m_dMapPredicatesToFormulas[pCanonical].Add(m_lHiddenFormulas.Count - 1);
               
            }

            


            m_cfCNFHiddenState.AddOperand(cf);
            if (!cf.IsSimpleConjunction() && !cf.IsSimpleOneOf() && SDRPlanner.EnforceCNF)
            {
                
                m_cfCNFHiddenState = (CompoundFormula)m_cfCNFHiddenState.ToCNF();
            }
        }

        public List<string> Reasoned = new List<string>();
        private void AddReasonedPredicate(Predicate p)
        {
            if (p == Domain.TRUE_PREDICATE)
                return;
            Debug.Assert(p != Domain.FALSE_PREDICATE, "Adding P_FALSE to the state");
            if (!m_lObserved.Contains(p))
            {
                if (p.Name != "Choice" && UnderlyingEnvironmentState != null)
                {
                    if (p.Negation)
                        Debug.Assert(!UnderlyingEnvironmentState.Predicates.Contains(p.Negate()), "Adding the negation of a state variable" );
                    else
                        Debug.Assert(UnderlyingEnvironmentState.Predicates.Contains(p), "Adding the negation of a state variable");
                }
                AddObserved(p);
                Debug.WriteLine("Reasoned: " + p);
                Reasoned.Add(p.ToString());
            }
        }
        /*
        public bool ApplyReasoning()
        {
            bool bChanged = true, bReasoned = false;
            while (bChanged)
            {
                bugbug;
                bChanged = false;
                List<CompoundFormula> lNew = new List<CompoundFormula>();

                foreach (CompoundFormula cf in m_lHiddenFormulas)
                {
                    CompoundFormula cfCopy = new CompoundFormula(cf);
                    Formula fAfter = cfCopy.Reduce(Observed);
                    if (fAfter != null)
                    {
                        if (fAfter is CompoundFormula)
                        {
                            CompoundFormula cfAfter = (CompoundFormula)fAfter;
                            if (cfAfter.Operator == "and")
                            {
                                foreach (Formula fOperand in cfAfter.Operands)
                                {
                                    if (fOperand is PredicateFormula)
                                    {
                                        AddReasonedPredicate(((PredicateFormula)fOperand).Predicate);
                                        bChanged = true;
                                    }
                                    else
                                        lNew.Add((CompoundFormula)fOperand);
                                }
                            }
                            else
                                lNew.Add(cfAfter);
                        }
                        else
                        {
                            AddReasonedPredicate(((PredicateFormula)fAfter).Predicate);
                            bChanged = true;
                        }
                    }

                }
                if (bChanged)
                    bReasoned = true;
                m_lHiddenFormulas = lNew;
            }
            return bReasoned;
        }
        public bool ApplyReasoningII()
        {
            bool bChanged = true, bReasoned = false;
            while (bChanged)
            {
                bChanged = false;
                List<CompoundFormula> lNew = new List<CompoundFormula>();

                foreach (CompoundFormula cf in m_lHiddenFormulas)
                {
                    CompoundFormula cfCopy = new CompoundFormula(cf);
                    Formula fAfter = cfCopy.Reduce(Observed);
                    if (fAfter != null)
                    {
                        if (fAfter is CompoundFormula)
                        {
                            CompoundFormula cfAfter = (CompoundFormula)fAfter;
                            if (cfAfter.Operator == "and")
                            {
                                foreach (Formula fOperand in cfAfter.Operands)
                                {
                                    if (fOperand is PredicateFormula)
                                    {
                                        AddReasonedPredicate(((PredicateFormula)fOperand).Predicate);
                                        bChanged = true;
                                    }
                                    else
                                        lNew.Add((CompoundFormula)fOperand);
                                }
                            }
                            else
                                lNew.Add(cfAfter);
                        }
                        else
                        {
                            AddReasonedPredicate(((PredicateFormula)fAfter).Predicate);
                            bChanged = true;
                        }
                    }

                }
                if (bChanged)
                    bReasoned = true;
                m_lHiddenFormulas = lNew;
            }
            return bReasoned;
        }
        */
        public override string ToString()
        {
            foreach (Predicate p in Observed)
                if (p.Name == "at" && !p.Negation)
                    return p.ToString();
            return "";
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        

        private int GetRandomUniquePosition(List<int> lPositions, int iSize)
        {
            while (true)
            {
                int x = RandomGenerator.Next(iSize) + 1;
                int y = RandomGenerator.Next(iSize) + 1;
                int pos = y * 1000 + x;
                if (!lPositions.Contains(pos))
                {
                    lPositions.Add(pos);
                    return pos;
                }
            }
        }

        public State ChooseState(bool bRemoveNegativePredicates)
        {
            State s = new State(Problem);
            if (Problem.Domain.Name == "mines4")
            {
                string[] a = new string[]{
                         "(obs0-at p1-1)",
                        "(obs0-at p2-1)",
                        "(obs2-at p3-1)",
                        "(mine-at p4-1)",
                        "(obs1-at p4-1)",
                        "(obs2-at p1-2)",
                        "(obs2-at p2-2)",
                        "(obs3-at p3-2)",
                        "(mine-at p4-2)",
                        "(obs1-at p4-2)",
                        "(mine-at p1-3)",
                        "(obs1-at p1-3)",
                        "(mine-at p2-3)",
                        "(obs1-at p2-3)",
                        "(obs2-at p3-3)",
                        "(obs1-at p4-3)",
                        "(obs2-at p1-4)",
                        "(obs2-at p2-4)",
                        "(obs1-at p3-4)",
                        "(obs0-at p4-4)"};
                foreach (GroundedPredicate p in m_lObserved)
                    s.AddPredicate(p);
                foreach (string str in a)
                {
                    string[] a1 = str.Replace("(", "").Replace(")", "").Split(' ');
                    GroundedPredicate gp = new GroundedPredicate(a1[0]);
                    gp.AddConstant(new Constant("pos", a1[1]));
                    s.AddPredicate(gp);
                }

            }
            else if (SDRPlanner.SimulationStartState == null || UnderlyingEnvironmentState != null)
            {
                //if (Problem.Name.Contains("Large"))
                    //s = ChooseStateForLargeDomains();
                //else
                {
                    List<Predicate> lAssignment = null;
                    Debug.Write("Choosing hidden variables ");
                    while (lAssignment == null)
                    {
                        Debug.Write(".");
                        lAssignment = ChooseHiddenPredicates(m_lHiddenFormulas, true);
                    }
                    Debug.WriteLine("");
                    foreach (Predicate p in lAssignment)
                    {
                        s.AddPredicate(p);
                    }
                    foreach (GroundedPredicate p in m_lObserved)
                    {
                        s.AddPredicate(p);
                    }
                }
            }
            else
            {
                string sNextState = SDRPlanner.SimulationStartState[0];
                SDRPlanner.SimulationStartState.RemoveAt(0);
                CompoundFormula cfInit = Parser.ParseFormula(sNextState, Problem.Domain);
                foreach (PredicateFormula pf in cfInit.Operands)
                    s.AddPredicate(pf.Predicate);
                foreach (GroundedPredicate gp in Observed)
                    s.AddPredicate(gp);
            }
            if (bRemoveNegativePredicates)
                s.RemoveNegativePredicates();
            if (UnderlyingEnvironmentState == null)
                UnderlyingEnvironmentState = s;
            return s;
        }

        private List<CompoundFormula> Reduce(List<CompoundFormula> lHidden, List<Predicate> lAssignment, List<Predicate> lUnknown)
        {
            List<CompoundFormula> lReduced = new List<CompoundFormula>();
            bool bAssignmentChanged = false;
            foreach (CompoundFormula cf in lHidden)
            {
                if (cf != null)
                {
                    Formula fReduced = cf.Reduce(lAssignment);
                    if (fReduced.IsFalse(lAssignment))
                        return null;
                    if (fReduced is PredicateFormula)
                    {
                        Predicate pReasoned = ((PredicateFormula)fReduced).Predicate;
                        if (pReasoned != Domain.TRUE_PREDICATE)
                        {
                            if (lAssignment.Contains(pReasoned.Negate()))
                                return null;
                            if (!lAssignment.Contains(pReasoned))
                            {
                                lAssignment.Add(pReasoned);
                                lUnknown.Remove(pReasoned);
                                lUnknown.Remove(pReasoned.Negate());
                                bAssignmentChanged = true;
                            }
                        }
                    }
                    else
                    {
                        CompoundFormula cfReduced = (CompoundFormula)fReduced;
                        if (cfReduced.Operator == "and")
                        {
                            CompoundFormula cfAnd = new CompoundFormula("and");
                            foreach (Formula fSub in cfReduced.Operands)
                            {
                                if (fSub is PredicateFormula)
                                {
                                    Predicate pReasoned = ((PredicateFormula)fSub).Predicate;
                                    if (pReasoned != Domain.TRUE_PREDICATE)
                                    {

                                        if (lAssignment.Contains(pReasoned.Negate()))
                                            return null;
                                        if (!lAssignment.Contains(pReasoned))
                                        {
                                            lAssignment.Add(pReasoned);
                                            lUnknown.Remove(pReasoned);
                                            lUnknown.Remove(pReasoned.Negate());
                                            bAssignmentChanged = true;
                                        }
                                    }
                                }
                                else
                                    cfAnd.AddOperand(fSub);
                            }
                            if (cfAnd.Operands.Count == 1)
                                lReduced.Add((CompoundFormula)cfAnd.Operands[0]);
                            else if (cfAnd.Operands.Count > 1)
                                lReduced.Add(cfAnd);
                        }
                        else
                        {
                            lReduced.Add(cfReduced);
                        }
                    }
                }
            }
            if (bAssignmentChanged)
                return Reduce(lReduced, lAssignment, lUnknown);
            else
                return lReduced;
        }

        private List<CompoundFormula> AddAssignment(List<CompoundFormula> lHidden, List<Predicate> lAssignment, List<Predicate> lUnknown, Predicate pAssignment)
        {
            lAssignment.Add(pAssignment);
            return Reduce(lHidden, lAssignment, lUnknown);
        }
        private List<Predicate> ChooseHiddenPredicatesIII()
        {
            HashSet<Predicate> lAllPredicates = new HashSet<Predicate>();
            foreach (Formula f in m_lHiddenFormulas)
                f.GetAllPredicates(lAllPredicates);
            List<Predicate> lCanonicalPredicates = new List<Predicate>();
            foreach (Predicate p in lAllPredicates)
                if (!p.Negation)
                    lCanonicalPredicates.Add(p);
            return ChooseHiddenPredicates(new List<Predicate>(), lCanonicalPredicates, 0);
        }

        private List<Predicate> ChooseHiddenPredicates(List<Predicate> lAssignment, List<Predicate> lUnknown, int iCurrent)
        {
            if (iCurrent == lUnknown.Count)
                return lAssignment;
            Predicate pCurrent = lUnknown[iCurrent];
            bool bValid = true;
            if (RandomGenerator.NextDouble() < 0.5)
                pCurrent = pCurrent.Negate();
            lAssignment.Add(pCurrent);
            List<Predicate> lFullAssignment = null;
            //trying p
            foreach (CompoundFormula cfHidden in m_lHiddenFormulas)
            {
                if (cfHidden.IsFalse(lAssignment))
                {
                    bValid = false;
                    break;
                }
            }
            if (bValid)
                lFullAssignment = ChooseHiddenPredicates(lAssignment, lUnknown, iCurrent + 1);
            if (lFullAssignment != null)
                return lFullAssignment;
            //now trying ~p
            lAssignment.Remove(pCurrent);
            lAssignment.Add(pCurrent.Negate());
            bValid = true;
            foreach (CompoundFormula cfHidden in m_lHiddenFormulas)
                if (cfHidden.IsFalse(lAssignment))
                {
                    bValid = false;
                    break;
                }
            if (bValid)
                lFullAssignment = ChooseHiddenPredicates(lAssignment, lUnknown, iCurrent + 1);
            return lFullAssignment;
        }

        private List<CompoundFormula> RemoveOneOf(List<CompoundFormula> lHidden)
        {
            List<CompoundFormula> lClean = new List<CompoundFormula>();
            foreach (CompoundFormula cf in lHidden)
            {
                if (cf.Operator == "oneof")
                {
                    CompoundFormula cfAnd = new CompoundFormula("and");
                    int idx = RandomGenerator.Next(cf.Operands.Count);
                    cfAnd.AddOperand(cf.Operands[idx]);
                    for (int i = 0; i < cf.Operands.Count; i++)
                    {
                        if (i != idx)
                            cfAnd.AddOperand(cf.Operands[i].Negate());
                    }
                    lClean.Add(cfAnd);
                }
                else
                {
                    lClean.Add(cf);
                }
            }
            return lClean;
        }

        private List<Predicate> ChooseHiddenPredicates(List<CompoundFormula> lHidden, bool bCheatUsingAt)
        {
            return ChooseHiddenPredicates(lHidden, null, bCheatUsingAt);
        }

        private List<Predicate> ChooseHiddenPredicates(List<CompoundFormula> lHidden, List<List<Predicate>> lCurrentAssignments, bool bCheatUsingAt)
        {
            HashSet<Predicate> lAllPredicates = new HashSet<Predicate>();
            HashSet<Predicate> lOneoffPredicates = new HashSet<Predicate>();
            List<CompoundFormula> lOneOfs = new List<CompoundFormula>();
            foreach (CompoundFormula f in lHidden)
            {
                if (f != null)
                {
                    if (f.IsSimpleOneOf())
                    {
                        lOneOfs.Add(f);
                        f.GetAllPredicates(lOneoffPredicates);
                    }
                    else
                        f.GetAllPredicates(lAllPredicates);
                }
            }
            List<Predicate> lCanonicalPredicates = new List<Predicate>();
            List<Predicate> lCanonicalOneofPredicates = new List<Predicate>();
            foreach (Predicate p in lAllPredicates)
            {
                Predicate pCanonical = p.Canonical();
                if (!lCanonicalPredicates.Contains(pCanonical))
                    lCanonicalPredicates.Add(pCanonical);

            }
            foreach (Predicate p in lOneoffPredicates)
            {
                Predicate pCanonical = p.Canonical();
                if (!lCanonicalOneofPredicates.Contains(pCanonical))
                    lCanonicalOneofPredicates.Add(pCanonical);

            }
            if (!SDRPlanner.ComputeCompletePlanTree)
            {
                lCanonicalPredicates = Permute(lCanonicalPredicates);
                lCanonicalOneofPredicates = Permute(lCanonicalOneofPredicates);
            }
            List<Predicate> lToAssign = new List<Predicate>();
            foreach (Predicate p in lCanonicalOneofPredicates)
                if (!lToAssign.Contains(p))
                    lToAssign.Add(p);
            foreach (Predicate p in lCanonicalPredicates)
                if (!lToAssign.Contains(p))
                    lToAssign.Add(p);

            List<Predicate> lInitialAssignment = new List<Predicate>();
            if (false && bCheatUsingAt)
                CheatUsingAtPredicate(lToAssign, lInitialAssignment);

            //ApplySimpleOneOfs(lOneOfs, lInitialAssignment, lCanonicalPredicates);

            List<Predicate> lAssignment = null;
            if (lCurrentAssignments == null)
                lAssignment = ChooseHiddenPredicates(lHidden, lInitialAssignment, lToAssign);
            else
            {
                lAssignment = SimpleChooseHiddenPredicates(new List<Formula>(lHidden), new HashSet<Predicate>(lInitialAssignment), lToAssign, lCurrentAssignments);
                if(lAssignment == null)
                    lAssignment = ChooseHiddenPredicates(lHidden, lInitialAssignment, lToAssign, lCurrentAssignments);
            }
     
            return lAssignment;
        }
        /*
        private List<Predicate> ChooseHiddenPredicates(List<CompoundFormula> lHidden, List<List<Predicate>> lCurrentAssignments)
        {
            HashSet<Predicate> lAllPredicates = new HashSet<Predicate>();
            foreach (CompoundFormula f in lHidden)
            {
                if( f != null)
                    f.GetAllPredicates(lAllPredicates);
            }
            List<Predicate> lCanonicalPredicates = new List<Predicate>();
            foreach (Predicate p in lAllPredicates)
            {
                if (!p.Negation)
                {
                    if (!lCanonicalPredicates.Contains(p))
                        lCanonicalPredicates.Add(p);
                }
                else
                {
                    Predicate pNegate = p.Negate();
                    if (!lCanonicalPredicates.Contains(pNegate))
                        lCanonicalPredicates.Add(pNegate);
                }
            }

            List<Predicate> lInitialAssignment = new List<Predicate>();

            List<Predicate> lAssignment = ChooseHiddenPredicates(lHidden, lInitialAssignment, lCanonicalPredicates, lCurrentAssignments);
            return lAssignment;
        }*/

        //doesn't work - need to correlate between oneofs...
        private void ApplySimpleOneOfs(List<CompoundFormula> lOneOfs, List<Predicate> lInitialAssignment, List<Predicate> lCanonicalPredicates)
        {
            foreach (CompoundFormula cf in lOneOfs)
            {
                List<Predicate> lUnassignedPredicates = new List<Predicate>();
                bool bAlreadyTrue = false;
                foreach (PredicateFormula pf in cf.Operands)
                {
                    if (lCanonicalPredicates.Contains(pf.Predicate))
                    {
                        lUnassignedPredicates.Add(pf.Predicate);
                        lCanonicalPredicates.Remove(pf.Predicate);
                    }
                    if (lCanonicalPredicates.Contains(pf.Predicate.Negate()))
                    {
                        lUnassignedPredicates.Add(pf.Predicate);
                        lCanonicalPredicates.Remove(pf.Predicate.Negate());
                    }
                    if (lInitialAssignment.Contains(pf.Predicate))
                        bAlreadyTrue = true;
                }
                if (!bAlreadyTrue)
                {
                    int idx = RandomGenerator.Next(lUnassignedPredicates.Count);
                    Predicate pTrue = lUnassignedPredicates[idx];
                    lUnassignedPredicates.RemoveAt(idx);
                    lInitialAssignment.Add(pTrue);
                }
                foreach (Predicate p in lUnassignedPredicates)
                    lInitialAssignment.Add(p.Negate());
            }
        }

        private void CheatUsingAtPredicate(List<Predicate> lCanonicalPredicates, List<Predicate> lInitialAssignment)
        {
            List<Predicate> lValidAt = new List<Predicate>();
            foreach (Predicate p in lCanonicalPredicates)
                if (p.Name == "at")
                    lValidAt.Add(p);
            if (lValidAt.Count > 0)
            {
                int idx = RandomGenerator.Next(lValidAt.Count);
                lInitialAssignment.Add(lValidAt[idx]);
                for (int i = 0; i < lValidAt.Count; i++)
                {
                    if (i != idx)
                        lInitialAssignment.Add(lValidAt[i].Negate());
                    lCanonicalPredicates.Remove(lValidAt[i]);
                }
            }
        }

        private List<Predicate> Permute(List<Predicate> lPredicates)
        {
            List<Predicate> lPermutation = new List<Predicate>();
            while (lPredicates.Count > 0)
            {
                int idx = RandomGenerator.Next(lPredicates.Count);
                lPermutation.Add(lPredicates[idx]);
                lPredicates.RemoveAt(idx);

            }
            return lPermutation;
        }

        private Predicate GetNonDiversePredicate(List<Predicate> lUnknown, List<List<Predicate>> lCurrentAssignments, out bool bAllTrue, out bool bAllFalse)
        {
            bAllTrue = true;
            bAllFalse = true;

            if (lCurrentAssignments.Count == 0)
            {
                bAllTrue = false;//choosing true first is better in some domains
                bAllFalse = false;//but not in others...
                return lUnknown[0];//the order of the variables is already randomized - might as well return the first one. This is important because oneofs appear first.
                //return lUnknown[RandomGenerator.Next(lUnknown.Count)]; the order of the variables is already randomized - might as well return the first one. This is important because oneofs appear first.
            }
            List<Predicate>[] alPredicates = new List<Predicate>[3];
            alPredicates[0] = new List<Predicate>();
            alPredicates[1] = new List<Predicate>();
            alPredicates[2] = new List<Predicate>();
            foreach (Predicate p in lUnknown)
            {
                bAllTrue = true;
                bAllFalse = true;
                foreach (List<Predicate> lAssignment in lCurrentAssignments)
                {
                    if (lAssignment.Contains(p))
                        bAllFalse = false;
                    else
                        bAllTrue = false;
                    if (!bAllTrue && !bAllFalse)
                        break;
                }
                if (bAllTrue)
                    alPredicates[0].Add(p);
                if (bAllFalse)
                    alPredicates[1].Add(p);
                if (!bAllFalse && !bAllTrue)
                    alPredicates[2].Add(p);
               // if (bAllFalse || bAllTrue)
                //    return p;
            }
            if (alPredicates[0].Count > 0)
            {
                bAllTrue = true;
                bAllFalse = false;
                return alPredicates[0][0];//the order of the variables is already randomized - might as well return the first one. This is important because oneofs appear first.
                //return alPredicates[0][RandomGenerator.Next(alPredicates[0].Count)];
            }
            if (alPredicates[1].Count > 0)
            {
                bAllTrue = false;
                bAllFalse = true;
                return alPredicates[1][0];//the order of the variables is already randomized - might as well return the first one. This is important because oneofs appear first.
                //return alPredicates[1][RandomGenerator.Next(alPredicates[1].Count)];
            }
            if (alPredicates[2].Count > 0)
            {
                bAllTrue = false;
                bAllFalse = false;
                return alPredicates[2][0];//the order of the variables is already randomized - might as well return the first one. This is important because oneofs appear first.
                //return alPredicates[2][RandomGenerator.Next(alPredicates[2].Count)];
            }
            return lUnknown.First();
        }
        private List<Predicate> SimpleChooseHiddenPredicates(List<Formula> lHidden, HashSet<Predicate> lAssignment, List<Predicate> lUnknown, List<List<Predicate>> lCurrentAssignments)
        {
            while (lUnknown.Count > 0)
            {
                bool bAllTrue = false, bAllFalse = false;
                Predicate pCurrent = GetNonDiversePredicate(lUnknown, lCurrentAssignments, out bAllTrue, out bAllFalse);
                lUnknown.Remove(pCurrent);
                if (bAllTrue)
                    pCurrent = pCurrent.Negate();
                else if (!bAllFalse && !SDRPlanner.ComputeCompletePlanTree && RandomGenerator.NextDouble() < 0.5)
                {
                    pCurrent = pCurrent.Negate();
                }
                lHidden.Add(new PredicateFormula(pCurrent));

                bool bValid = ApplyUnitPropogation(lHidden, lAssignment);
                if (!bValid)
                    return null;
                //List<CompoundFormula> lReduced = AddAssignment(lHidden, lNewAssignment, lNewUnknown, pCurrent);
                foreach (Predicate p in lAssignment)
                    lUnknown.Remove(p.Canonical());
            }
            return new List<Predicate>(lAssignment);
        }

        private List<Predicate> ChooseHiddenPredicates(List<CompoundFormula> lHidden, List<Predicate> lAssignment, List<Predicate> lUnknown, List<List<Predicate>> lCurrentAssignments)
        {
            if (lHidden == null)
                return null;
            if (lUnknown.Count == 0)
                return lAssignment;//BUGBUG - does not work - need to check why!!
            bool bAllTrue = false, bAllFalse = false;
            Predicate pCurrent = GetNonDiversePredicate(lUnknown, lCurrentAssignments, out bAllTrue, out bAllFalse);
            lUnknown.Remove(pCurrent);
            if (bAllTrue)
                pCurrent = pCurrent.Negate();
            else if (!bAllFalse && !SDRPlanner.ComputeCompletePlanTree && RandomGenerator.NextDouble() < 0.5)
            {
                pCurrent = pCurrent.Negate();
            }
            List<Predicate> lNewHidden = new List<Predicate>(lUnknown);
            List<Predicate> lNewAssignment = new List<Predicate>(lAssignment);
            List<CompoundFormula> lReduced = AddAssignment(lHidden, lNewAssignment, lNewHidden, pCurrent);
            List<Predicate> lFullAssignment = ChooseHiddenPredicates(lReduced, lNewAssignment, lNewHidden, lCurrentAssignments);
            if (lFullAssignment != null)
                return lFullAssignment;
            lNewHidden = new List<Predicate>(lUnknown);
            lNewAssignment = new List<Predicate>(lAssignment);
            lReduced = AddAssignment(lHidden, lNewAssignment, lNewHidden, pCurrent.Negate());
            lFullAssignment = ChooseHiddenPredicates(lReduced, lNewAssignment, lNewHidden, lCurrentAssignments);
            return lFullAssignment;
        }
        //random
        private List<Predicate> ChooseHiddenPredicates(List<CompoundFormula> lHidden, List<Predicate> lAssignment, List<Predicate> lUnknown)
        {
            if (lHidden == null)
                return null;
            if (lUnknown.Count == 0)
                return lAssignment;//BUGBUG - does not work - need to check why!!
            Predicate pCurrent = lUnknown.First();
            lUnknown.Remove(pCurrent);
            if (!SDRPlanner.ComputeCompletePlanTree)
                if (RandomGenerator.NextDouble() < 0)
                    pCurrent = pCurrent.Negate();
            List<Predicate> lNewHidden = new List<Predicate>(lUnknown);
            List<Predicate> lNewAssignment = new List<Predicate>(lAssignment);
            List<CompoundFormula> lReduced = AddAssignment(lHidden, lNewAssignment, lNewHidden, pCurrent);
            List<Predicate> lFullAssignment = ChooseHiddenPredicates(lReduced, lNewAssignment, lNewHidden);
            if (lFullAssignment != null)
                return lFullAssignment;
            lNewHidden = new List<Predicate>(lUnknown);
            lNewAssignment = new List<Predicate>(lAssignment);
            lReduced = AddAssignment(lHidden, lNewAssignment, lNewHidden, pCurrent.Negate());
            lFullAssignment = ChooseHiddenPredicates(lReduced, lNewAssignment, lNewHidden);
            return lFullAssignment;
        }

        private List<Formula> FindObligatory(List<Predicate> lAssignment, List<Formula> lOptional)
        {
            List<Formula> lObligatory = new List<Formula>();
            foreach (Formula f in lOptional)
            {
                if (f.IsTrue(lAssignment))
                    lObligatory.Add(f);
            }
            return lObligatory;
        }

        private bool SelectAtLeastOne(List<Predicate> lAssignment, List<Formula> lOptional)
        {
            List<Formula> lConsistent = RemoveInconsistencies(lAssignment, lOptional);
            if (lConsistent.Count == 0)
                return false;
            List<Formula> lObligatory = FindObligatory(lAssignment, lConsistent);
            foreach (Formula fObligatory in lObligatory)
            {
                if (fObligatory is PredicateFormula)
                    lAssignment.Add(((PredicateFormula)fObligatory).Predicate);
                else
                    throw new NotImplementedException("Need to implement behavior for compound formulas.");
                lConsistent.Remove(fObligatory);
            }
            if (lConsistent.Count > 0)
            {
                int iTermCount = RandomGenerator.Next(lConsistent.Count) + 1;//n+1 because we want n to be also valid
                while (iTermCount > 0)
                {
                    int iRandomIndex = RandomGenerator.Next(lConsistent.Count);
                    Formula f = lConsistent[iRandomIndex];
                    if (f is PredicateFormula)
                        lAssignment.Add(((PredicateFormula)f).Predicate);
                    else
                    {
                        throw new NotImplementedException("Need to implement behavior for compound formulas.");
                    }
                    lConsistent.RemoveAt(iRandomIndex);
                    iTermCount--;
                }
                //for the rest of the terms - set the value to false
                foreach (Formula f in lConsistent)
                {
                    if (f is PredicateFormula)
                        lAssignment.Add(((PredicateFormula)f).Predicate.Negate());
                    else
                        throw new NotImplementedException("Need to implement behavior for compound formulas.");
                }
            }
            return true;
        }


        private bool SelectOneOf(List<Predicate> lAssignment, List<Formula> lOptional)
        {
            List<Formula> lConsistent = RemoveInconsistencies(lAssignment, lOptional);
            if (lConsistent.Count == 0)
                return false;
            List<Formula> lObligatory = FindObligatory(lAssignment, lConsistent);
            if (lObligatory.Count > 1)
                return false;
            int iRandomIndex = RandomGenerator.Next(lConsistent.Count);
            if (lObligatory.Count == 1)
                iRandomIndex = lConsistent.IndexOf(lObligatory[0]);
            Formula f = lConsistent[iRandomIndex];
            if (f is PredicateFormula)
                lAssignment.Add(((PredicateFormula)f).Predicate);
            else
            {
                CompoundFormula cf = (CompoundFormula)f;
                if (cf.Operator == "or")
                {
                    bool bSuccess = SelectAtLeastOne(lAssignment, cf.Operands);
                    if (!bSuccess)
                        return false;
                }
                else
                    throw new NotImplementedException("Need to implement behavior for compound formulas.");
            }
            lConsistent.RemoveAt(iRandomIndex);
            foreach (Formula fOther in lConsistent)
            {
                if (fOther is PredicateFormula)
                {
                    Predicate cpNegate = ((PredicateFormula)fOther).Predicate.Negate();
                    lAssignment.Add(cpNegate);
                }
                else
                {
                    CompoundFormula cf = (CompoundFormula)fOther;
                    if (cf.Operator == "or")//must make all sub-forumlas false
                    {
                        foreach (Formula fSub in cf.Operands)
                        {
                            if (fSub is PredicateFormula)
                            {
                                Predicate p = ((PredicateFormula)fSub).Predicate.Negate();
                                lAssignment.Add(p);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }

                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            return true;
        }

        private List<Formula> RemoveInconsistencies(List<Predicate> lAssignment, List<Formula> lOptional)
        {
            List<Formula> lConsistent = new List<Formula>();
            foreach (Formula f in lOptional)
            {
                if (!f.IsFalse(lAssignment))
                    lConsistent.Add(f);
            }
            return lConsistent;
        }


        public bool IsGoalState()
        {
            return Contains(Problem.Goal);
        }

        private List<State> ApplyActions(List<List<Predicate>> lChosen, List<Action> lActions)
        {
            List<State> lCurrent = new List<State>();
            foreach (List<Predicate> lState in lChosen)
            {
                State s = new State(Problem);
                foreach (Predicate p in lState)
                    s.AddPredicate(p);
                foreach (Predicate p in Observed)
                    s.AddPredicate(p);
                lCurrent.Add(s);
            }

            List<State> lNext = null;
            foreach (Action a in lActions)
            {
                lNext = new List<State>();
                foreach (State s in lCurrent)
                {
                    State sTag = s.Apply(a);
                    if (sTag == null)
                        sTag = s.Apply(a);
                    lNext.Add(sTag);
                }
                lCurrent = lNext;
            }

            return lCurrent;
        }


        public State WriteTaggedDomainAndProblem(string sDomainFile, string sProblemFile, CompoundFormula cfGoal, List<Action> lAppliedActions, out int cTags, out MemoryStream msModels)
        {
            List<List<Predicate>> lChosen = ChooseStateSet();
            List<State> lStates = ApplyActions(lChosen, lAppliedActions);

            msModels = null;

            if (lStates.Count == 1)
            {
                MemoryStream msProblem = Problem.WriteSimpleProblem(sProblemFile, lStates[0]);
                MemoryStream msDomain = Problem.Domain.WriteSimpleDomain(sDomainFile);

                msModels = new MemoryStream();
                StreamWriter sw = new StreamWriter(msModels);
                msDomain.Position = 0;
                StreamReader srDomain = new StreamReader(msDomain);
                sw.Write(srDomain.ReadToEnd());
                sw.Write('\0');
                msProblem.Position = 0;
                StreamReader srProblem = new StreamReader(msProblem);
                sw.Write(srProblem.ReadToEnd());
                sw.Write('\0');
                sw.Flush();

                cTags = 1;
                return lStates[0];
            }

            if (SDRPlanner.ConsiderStateNegations)
            {
                List<List<Predicate>> lAllOthers = new List<List<Predicate>>();
                lAllOthers.Add(GetNonAppearingPredicates(lChosen));
                lStates.AddRange(ApplyActions(lAllOthers, lAppliedActions));
            }
            return WriteTaggedDomainAndProblem(sDomainFile, sProblemFile, cfGoal, lStates, false, out cTags, out msModels);
        }

        public State WriteTaggedDomainAndProblem(PartiallySpecifiedState pssCurrent, string sDomainFile, string sProblemFile, List<Action> lAppliedActions, out int cTags, out MemoryStream msModels)
        {
            List<List<Predicate>> lChosen = ChooseStateSet();
            List<State> lStates = ApplyActions(lChosen, lAppliedActions);

            msModels = null;

            if (lStates.Count == 1 && !Problem.Domain.ContainsNonDeterministicActions && SDRPlanner.Translation != SDRPlanner.Translations.BestCase)
            {
                MemoryStream msProblem = Problem.WriteSimpleProblem(sProblemFile, lStates[0]);
                MemoryStream msDomain = Problem.Domain.WriteSimpleDomain(sDomainFile);

                msModels = new MemoryStream();
                StreamWriter sw = new StreamWriter(msModels);
                msDomain.Position = 0;
                StreamReader srDomain = new StreamReader(msDomain);
                string sDomain = srDomain.ReadToEnd();
                sw.Write(sDomain);
                sw.Write('\0');
                msProblem.Position = 0;            
                StreamReader srProblem = new StreamReader(msProblem);
                string sProblem = srProblem.ReadToEnd();
                sw.Write(sProblem);
                sw.Write('\0');
                sw.Flush();
                //sw.Close();
                cTags = 1;
                return lStates[0];
            }

            if (SDRPlanner.WriteAllKVariations)
            {
                /*
                for (int i = 0; i < lStates.Count; i++)
                {
                    WriteTaggedDomainAndProblem(sDomainFile.Replace(".pddl", i + ".pddl"), sProblemFile.Replace(".pddl", i + ".pddl"), lStates, false, out cTags);
                    lStates.Add(lStates[0]);
                    lStates.RemoveAt(0);
                }
                WriteTaggedDomainAndProblem(sDomainFile.Replace(".pddl", lStates.Count + ".pddl"), sProblemFile.Replace(".pddl", lStates.Count + ".pddl"), lStates, true, out cTags);
                 * */
                int cVersions = 0;
                if (lStates.Count > 2)
                {
                    /*
                    for (int i = 0; i < lStates.Count - 1; i++)
                    {
                        for (int j = i + 1; j < lStates.Count; j++)
                        {
                            List<State> lSelectedStates = new List<State>();
                            lSelectedStates.Add(lStates[i]);
                            lSelectedStates.Add(lStates[j]);
                            WriteTaggedDomainAndProblem(sDomainFile.Replace(".pddl", cVersions + ".pddl"), sProblemFile.Replace(".pddl", cVersions + ".pddl"), lSelectedStates, false, out cTags);
                            cVersions++;
                        }
                    }
                     * */
                    for (int i = 0; i < lStates.Count - 1; i++)
                    {
                        List<State> lSelectedStates = new List<State>();
                        lSelectedStates.Add(lStates[i]);
                        lSelectedStates.Add(lStates[i + 1]);
                        if (SDRPlanner.ConsiderStateNegations)
                        {
                            List<List<Predicate>> lCurrentChosen = new List<List<Predicate>>();
                            lCurrentChosen.Add(lChosen[i]);
                            lCurrentChosen.Add(lChosen[i + 1]);
                            List<List<Predicate>> lAllOthers = new List<List<Predicate>>();
                            lAllOthers.Add(GetNonAppearingPredicates(lCurrentChosen));
                            lSelectedStates.AddRange(ApplyActions(lAllOthers, lAppliedActions));
                        }
                        WriteTaggedDomainAndProblem(pssCurrent, sDomainFile.Replace(".pddl", cVersions + ".pddl"), sProblemFile.Replace(".pddl", cVersions + ".pddl"), lSelectedStates, false, out cTags, out msModels);
                        cVersions++;
                    }
                }
                else
                {
                    if (SDRPlanner.ConsiderStateNegations)
                    {
                        List<List<Predicate>> lAllOthers = new List<List<Predicate>>();
                        lAllOthers.Add(GetNonAppearingPredicates(lChosen));
                        lStates.AddRange(ApplyActions(lAllOthers, lAppliedActions));
                    } 
                    WriteTaggedDomainAndProblem(pssCurrent, sDomainFile, sProblemFile, lStates, false, out cTags, out msModels);
                    cVersions = 1;
                }
                cTags = cVersions;
                return lStates[0];
            }

            if (SDRPlanner.ConsiderStateNegations)
            {                
                List<List<Predicate>> lAllOthers = new List<List<Predicate>>();
                lAllOthers.Add(GetNonAppearingPredicates(lChosen));
                lStates.AddRange(ApplyActions(lAllOthers, lAppliedActions));
            } 
            return WriteTaggedDomainAndProblem(pssCurrent, sDomainFile, sProblemFile, lStates, false, out cTags, out msModels);
        }
        public State WriteTaggedDomainAndProblem(string sDomainFile, string sProblemFile, CompoundFormula cfGoal, List<State> lStates, bool bOnlyIdentifyStates, out int cTags, out MemoryStream msModels)
        {
            HashSet<Predicate> lObserved = new HashSet<Predicate>();
            Dictionary<string, List<Predicate>> dTags = GetTags(lStates, lObserved);

            cTags = dTags.Count;

            msModels = new MemoryStream();
            BinaryWriter swModels = new BinaryWriter(msModels);

            //Debug.WriteLine("Writing tagged domain");
            MemoryStream msDomain = null, msProblem = null;
            if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
                msDomain = Problem.Domain.WriteTaggedDomain(dTags, Problem);
            else
                msDomain = Problem.Domain.WriteTaggedDomainNoState(dTags, Problem);


            msDomain.Position = 0;
            BinaryReader sr = new BinaryReader(msDomain);
            byte b = sr.ReadByte();
            while (b >= 0)
            {
                swModels.Write(b);
                if (sr.BaseStream.Position == sr.BaseStream.Length)
                {
                    break;
                }
                b = sr.ReadByte();
            }
            swModels.Write('\0');
            swModels.Flush();
            //sr.Close();


#if !DEBUG
            if (SDRPlanner.UseFilesForPlanners)
            {
#endif
            StreamWriter swDomainFile = new StreamWriter(sDomainFile);
            msDomain.Position = 0;
            StreamReader srDomainFile = new StreamReader(msDomain);
            swDomainFile.Write(srDomainFile.ReadToEnd());
            swDomainFile.Close();
#if !DEBUG
            }
#endif


            //Debug.WriteLine("Writing tagged problem");
            if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
                msProblem = Problem.WriteTaggedProblem(dTags, cfGoal, lObserved, dTags.Values.First(), lStates.First().FunctionValues, bOnlyIdentifyStates); //the first tag is the real state
            else
                msProblem = Problem.WriteTaggedProblemNoState(dTags, lObserved, lStates.First().FunctionValues);


            msProblem.Position = 0;
            sr = new BinaryReader(msProblem);
            b = sr.ReadByte();
            while (b >= 0)
            {
                swModels.Write(b);
                if (sr.BaseStream.Position == sr.BaseStream.Length)
                {
                    break;
                }
                b = sr.ReadByte();
            }
            swModels.Write('\0');
            //sr.Close();
            swModels.Flush();


#if !DEBUG
            if (SDRPlanner.UseFilesForPlanners)
            {
#endif
            StreamWriter swProblemFile = new StreamWriter(sProblemFile);
            msProblem.Position = 0;
            StreamReader srProblemFile = new StreamReader(msProblem);
            swProblemFile.Write(srProblemFile.ReadToEnd());
            swProblemFile.Close();
#if !DEBUG

            }
#endif

            return lStates[0];
        }
        public State WriteTaggedDomainAndProblem(PartiallySpecifiedState pssCurrent, string sDomainFile, string sProblemFile, List<State> lStates, bool bOnlyIdentifyStates, out int cTags, out MemoryStream msModels)
        {
            HashSet<Predicate> lObserved = new HashSet<Predicate>();
            Dictionary<string, List<Predicate>> dTags = GetTags(lStates, lObserved);
           
            cTags = dTags.Count;

            msModels = new MemoryStream();
            BinaryWriter swModels = new BinaryWriter(msModels);

            //Debug.WriteLine("Writing tagged domain");
            MemoryStream msDomain = null, msProblem = null;
            if (SDRPlanner.Translation == SDRPlanner.Translations.BestCase || SDRPlanner.Translation == SDRPlanner.Translations.Conformant)
                msDomain = Problem.Domain.WriteKnowledgeDomain(Problem, pssCurrent.MishapCount, pssCurrent.MinMishapCount, pssCurrent.MishapType, false, SDRPlanner.Planner == SDRPlanner.Planners.CPT);
            else if (SDRPlanner.Translation == SDRPlanner.Translations.SingleStateK)
                msDomain = Problem.Domain.WriteKnowledgeDomain(Problem, pssCurrent.MishapCount, pssCurrent.MinMishapCount, pssCurrent.MishapType, true, SDRPlanner.Planner == SDRPlanner.Planners.CPT);
            else if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
                msDomain = Problem.Domain.WriteTaggedDomain(dTags, Problem);
            else
                msDomain = Problem.Domain.WriteTaggedDomainNoState( dTags, Problem);


            msDomain.Position = 0;
            BinaryReader sr = new BinaryReader(msDomain);
            byte b = sr.ReadByte();
            while (b >= 0)
            {
                swModels.Write(b);
                if (sr.BaseStream.Position == sr.BaseStream.Length)
                {
                    break;
                }
                b = sr.ReadByte();
            }
            swModels.Write('\0');
            swModels.Flush();

            //sr.Close();



            if (SDRPlanner.UseFilesForPlanners)
            {
                bool bDone = false;
                while (!bDone)
                {
                    try
                    {
                        StreamWriter swDomainFile = new StreamWriter(sDomainFile);
                        msDomain.Position = 0;
                        StreamReader srDomainFile = new StreamReader(msDomain);
                        swDomainFile.Write(srDomainFile.ReadToEnd());
                        swDomainFile.Close();
                        bDone = true;
                    }
                    catch (Exception e) { }
                }
            }


            //Debug.WriteLine("Writing tagged problem");
            if (SDRPlanner.Translation == SDRPlanner.Translations.BestCase || SDRPlanner.Translation == SDRPlanner.Translations.Conformant)
                msProblem = Problem.WriteKnowledgeProblem(new HashSet<Predicate>(pssCurrent.Observed), new HashSet<Predicate>(pssCurrent.Hidden), pssCurrent.MinMishapCount, pssCurrent.MishapCount, SDRPlanner.Planner == SDRPlanner.Planners.CPT);
            else if (SDRPlanner.Translation == SDRPlanner.Translations.SingleStateK )
                msProblem = Problem.WriteKnowledgeProblem(new HashSet<Predicate>(pssCurrent.Observed), new HashSet<Predicate>(lStates[0].Predicates));
            else if (SDRPlanner.Translation == SDRPlanner.Translations.SDR)
                msProblem = Problem.WriteTaggedProblem(dTags, lObserved, dTags.Values.First(), lStates.First().FunctionValues, bOnlyIdentifyStates); //the first tag is the real state
            else
                msProblem = Problem.WriteTaggedProblemNoState(dTags, lObserved, lStates.First().FunctionValues);


            msProblem.Position = 0;
            sr = new BinaryReader(msProblem);
            b = sr.ReadByte();
            while (b >= 0)
            {
                swModels.Write(b);
                if (sr.BaseStream.Position == sr.BaseStream.Length)
                {
                    break;
                }
                b = sr.ReadByte();
            }
            swModels.Write('\0');
            //sr.Close();
            swModels.Flush();

            /*
            msModels.Position = 0;
            StreamReader sr2 = new StreamReader(msModels);
            for (int i = 0; i < 10; i++)
                Console.Write(sr2.Read() + ",");
            Console.WriteLine();
            */
            if (SDRPlanner.UseFilesForPlanners)
            {
                bool bDoneIO = false;
                //while (!bDone)
                {
                    //try
                    {
                        StreamWriter swProblemFile = new StreamWriter(sProblemFile);
                        msProblem.Position = 0;
                        StreamReader srProblemFile = new StreamReader(msProblem);
                        swProblemFile.Write(srProblemFile.ReadToEnd());
                        swProblemFile.Close();
                        bDoneIO = true;
                    }
                    //catch (Exception e) { }
                }


            }

                    // SASWriter sw = new SASWriter(Problem.Domain, Problem, dTags, dTags.Values.First(), lObserved);
                    //sw.WriteDomainAndProblem(sDomainFile.Replace(".pddl",".sas"));

                    return lStates[0];
        }
        private Dictionary<string, List<Predicate>> GetTags(List<State> lStates, HashSet<Predicate> lObserved)
        {
            Dictionary<string, List<Predicate>> dTags = new Dictionary<string, List<Predicate>>();
            int iTag = 0;
            //bugbug - what happens when there is only a single state?

            foreach (Predicate p in lStates[0].Predicates)
            {
                bool bObserved = true;
                for (int i = 1; i < lStates.Count && bObserved; i++)
                {
                    if (!lStates[i].Predicates.Contains(p))
                        bObserved = false;
                }
                if (bObserved)
                    lObserved.Add(p);
            }


            foreach (State s in lStates)
            {
                string sTag = "tag" + iTag;
                iTag++;
                List<Predicate> lHidden = new List<Predicate>();
                foreach (Predicate p in s.Predicates)
                {
                    if (!lObserved.Contains(p))
                        lHidden.Add(p);
                }
                dTags[sTag] = lHidden;
            }
            return dTags;
        }

        private Dictionary<string, List<Predicate>> GetTags(List<List<Predicate>> lStates)
        {
            Dictionary<string, List<Predicate>> dTags = new Dictionary<string, List<Predicate>>();
            int iTag = 0;
            foreach (List<Predicate> s in lStates)
            {
                string sTag = "tag" + iTag;
                iTag++;
                /*
                foreach (Predicate p in s)
                {
                    string sPredicateName = p.ToString();
                    sPredicateName = sPredicateName.Replace("(", "");
                    sPredicateName = sPredicateName.Replace(")", "");
                    sPredicateName = sPredicateName.Replace(" ", "");
                    sTag += "_" + sPredicateName;
                }
                 */
                dTags[sTag] = s;
            }
            return dTags;
        }

        private List<List<Predicate>> ChooseStateSet()
        {
            if (SDRPlanner.Translation == SDRPlanner.Translations.BestCase ||  (Unknown.Count == 0 && !Problem.Domain.ContainsNonDeterministicActions))
            {
                List<List<Predicate>> lState = new List<List<Predicate>>();
                lState.Add(new List<Predicate>(m_lObserved));
                return lState;
            }

            //return ChooseRandomStateSet(TagsCount);
            return ReviseExistingTags(SDRPlanner.TagsCount);
            //return ChooseOrthogonalArrayStateSet();
            //return ChooseDiverseStateSet(SDRPlanner.TagsCount, null);
        }

        private List<List<Predicate>> ReviseExistingTags(int cTags)
        {
            if (m_lCurrentTags == null || m_lCurrentTags.Count == 0 || m_lProblematicTag == null)
            {
                //m_lCurrentTags = ChooseRandomStateSet(cTags);
                m_lCurrentTags = ChooseDiverseStateSet(cTags, null);//this is the real one!
                //if (m_lCurrentTags.Count == 1)
                //    Console.WriteLine("BUGBUG");
                /*
                List<GroundedPredicate> lSeedPredicates = new List<GroundedPredicate>();
                for (int i = 3; i <= 5; i++)
                {
                    GroundedPredicate gp = new GroundedPredicate("wumpus-at");
                    gp.AddConstant(new Constant("pos", "p" + i + "-" + (i-1)));
                    lSeedPredicates.Add(gp);
                    gp = new GroundedPredicate("wumpus-at");
                    gp.AddConstant(new Constant("pos", "p" + (i - 1) + "-" + i));
                    lSeedPredicates.Add(gp);
                }
                m_lCurrentTags = ChooseStateSetForLandmarkDetection(lSeedPredicates);
                 * */
                //Console.WriteLine("ChooseRandomStateSet diversity level: " + DiversityLevel(ChooseRandomStateSet(cTags)));
                //Console.WriteLine("ChooseDiverseStateSet diversity level: " + DiversityLevel(ChooseDiverseStateSet(cTags, null)));


            }
            else if (m_lProblematicTag != null)
            {
                if (ConsistentWith(m_lCurrentTags[0]))//current tags are still valid
                {
                    List<List<Predicate>> lRefutationTags = new List<List<Predicate>>();
                    int cContinuingTags = Math.Min(m_lCurrentTags.Count, SDRPlanner.TagsCount - 1);
                    if (cContinuingTags == 0)
                        cContinuingTags = 1;
                    for (int i = 0; i < cContinuingTags; i++)                
                        lRefutationTags.Add(m_lCurrentTags[i]);
                    lRefutationTags.Add(m_lProblematicTag);
                    m_lCurrentTags = lRefutationTags;
                    /*
                    if (m_lCurrentTags.Count == cTags)
                        m_lCurrentTags[cTags - 1] = m_lProblematicTag;
                    else
                        m_lCurrentTags.Add(m_lProblematicTag);
                     * */
                }
                else
                {
                    
                    //m_lCurrentTags = RunSatSolver(m_cfCNFHiddenState, cTags - 1, m_lProblematicTag);
                    //m_lCurrentTags.Add(m_lProblematicTag);

                    m_lCurrentTags = ChooseDiverseStateSet(cTags - 1, m_lProblematicTag);
                }
                m_lProblematicTag = null;
            }
            /*
            if (m_lCurrentTags.Count == 0)//hack...
            {
                if( m_lHidden.Count > 0)
                {
                    m_cfCNFHiddenState = new CompoundFormula("and");
                    foreach (Formula f in m_lHidden)
                        m_cfCNFHiddenState.AddOperand(f);
                    m_cfCNFHiddenState = m_cfCNFHiddenState.ToCNF();
                    return ReviseExistingTags(cTags);
                }
            }
             * */


            return m_lCurrentTags;
        }

        private double DiversityLevel(List<List<Predicate>> m_lCurrentTags)
        {
            int cTags = m_lCurrentTags[0].Count;
            double cDifferent = 0;
            foreach (Predicate p in m_lCurrentTags[0])
            {
                Predicate pNegate = p.Negate();
                for (int i = 1; i < m_lCurrentTags.Count; i++)
                {
                    if (m_lCurrentTags[i].Contains(pNegate))
                    {
                        cDifferent++;
                        break;
                    }
                }
            }
            return cDifferent / cTags;
        }

        private int Choose(int k, int n)
        {
            double dNChooseKLogSpace = 0;
            int i = 0;
            for (i = 1; i <= n - k; i++)
            {
                dNChooseKLogSpace += Math.Log(k + i);
                dNChooseKLogSpace -= Math.Log(i);
            }
            double dExp = Math.Exp(dNChooseKLogSpace);
            return (int)Math.Round( dExp );
        }

        private bool ConsistentWith(List<Predicate> lPredicates)
        {
            /*
            CompoundFormula cfAnd = new CompoundFormula("and");
            foreach (Predicate p in lPredicates)
                cfAnd.AddOperand(p);
            return ConsistentWith(cfAnd);
             * */

            //simpler version - checking only the observed
            foreach(Predicate p in Observed)
                if(lPredicates.Contains(p.Negate()))
                    return false;
            return true;
        }


        private bool Equals(List<Predicate> l1, List<Predicate> l2)
        {
            if (l1.Count != l2.Count)
                return false;
            foreach (Predicate p1 in l1)
                if (!Contains(l2, p1))
                    return false;
            return true;
        }

        private bool Contains(List<Predicate> l, Predicate p)
        {
            foreach (Predicate pTag in l)
                if (p.Equals(pTag))
                    return true;
            return false;
        }

        private bool Contains(List<List<Predicate>> lStates, List<Predicate> lState)
        {
            if (lState == null)
                return true;
            foreach (List<Predicate> lExisting in lStates)
            {
                if (Equals(lExisting, lState))
                    return true;
            }
            return false;
        }

        private List<List<Predicate>> ChooseRandomStateSet(int cStates)
        {
            //List<List<Predicate>> lAssignments = RunSatSolver(m_cfCNFHiddenState, cStates);
            //return lAssignments;

            List<List<Predicate>> lStates = new List<List<Predicate>>();
            List<CompoundFormula> lConstraints = new List<CompoundFormula>(m_lHiddenFormulas);
            while (cStates > 0 || lStates.Count == 0)
            {
                List<Predicate> lAssignment = ChooseHiddenPredicates(lConstraints, false);

                if (!Contains(lStates, lAssignment))
                {
                    lStates.Add(lAssignment);
                    CompoundFormula cfOr = new CompoundFormula("or");
                    foreach (Predicate p in lAssignment)
                        cfOr.AddOperand(p.Negate());
                    lConstraints.Add(cfOr);
                }

                cStates--;
            }
            lStates = RandomPermutation(lStates);
            return lStates;

        }


        private List<List<Predicate>> ChooseStateSetForLandmarkDetection(List<GroundedPredicate> lSeedPredicates)
        {


            List<List<Predicate>> lStates = new List<List<Predicate>>();
            List<CompoundFormula> lConstraints = new List<CompoundFormula>(m_lHiddenFormulas);

            foreach (GroundedPredicate gp in lSeedPredicates)
            {
                List<Predicate> lAssignment = new List<Predicate>();
                List<Predicate> lUnknown = new List<Predicate>();
                List<CompoundFormula> lReducedConstraints = new List<CompoundFormula>();
                List<CompoundFormula> lReduced = AddAssignment(lConstraints, lAssignment, lUnknown, gp);
                lStates.Add(lAssignment);
            }
            return lStates;

        }


        private List<List<Predicate>> ChooseDiverseStateSet(int cStates, List<Predicate> lCurrentTag)
        {
            List<List<Predicate>> lStates = new List<List<Predicate>>();
            if (lCurrentTag != null)
                lStates.Add(lCurrentTag);
            List<CompoundFormula> lConstraints = new List<CompoundFormula>(m_lHiddenFormulas);
            if (Problem.Domain.HasNonDeterministicActions())
            {
                lConstraints.Add(Problem.Domain.GetOptionsStatement());
            }
            List<Predicate> lNotAppearing = null;
            if (lCurrentTag != null)
            {
                lNotAppearing = new List<Predicate>();
                foreach (Predicate p in lCurrentTag)
                    lNotAppearing.Add(p.Negate());
            }
            int cFailedAttempts = 0;
            int cChosenStates = 0;
            while ((cChosenStates < cStates && cFailedAttempts < cStates * 2) || lStates.Count == 0)//1 here if we want to add state negation
            {
                List<Predicate> lAssignment = ChooseHiddenPredicates(lConstraints, lStates, true);
                if (lNotAppearing == null)
                {
                    lNotAppearing = new List<Predicate>();
                    foreach (Predicate p in lAssignment)
                        lNotAppearing.Add(p.Negate());
                }
                else
                {
                    foreach (Predicate p in lAssignment)
                    {
                        lNotAppearing.Remove(p);
                    }
                }
                if (!Contains(lStates, lAssignment))
                {
                    lStates.Add(lAssignment);
                    /*
                    CompoundFormula cfOr = new CompoundFormula("or");
                    foreach (Predicate p in lAssignment)
                        cfOr.AddOperand(p.Negate());
                    lConstraints.Add(cfOr);
                     * */
                    cChosenStates++;
                }
                else
                    cFailedAttempts++; //here and not inside because there might be a case where we cannot reach cStates tags
            }
            /*
            if (lNotAppearing.Count > 0)
            {
                foreach (Predicate p in lStates.Last())
                {
                    if (!lNotAppearing.Contains(p.Negate()))
                        lNotAppearing.Add(p);
                }
                lStates.Add(lNotAppearing);
            }
            */
            //lStates = RandomPermutation(lStates);
            return lStates;

        }

        private List<Predicate> GetNonAppearingPredicates(List<List<Predicate>> lChosen)
        {
            List<Predicate> lNotAppearing = new List<Predicate>();
            foreach (Predicate p in lChosen[0])
                lNotAppearing.Add(p.Negate());
            
            for(int i = 1; i < lChosen.Count ; i++)
            {
                foreach (Predicate p in lChosen[i])
                {
                    lNotAppearing.Remove(p);
                }
            }           
            return lNotAppearing;           
        }

        private List<List<Predicate>> RandomPermutation(List<List<Predicate>> lStates)
        {
            List<List<Predicate>> lPermuted = new List<List<Predicate>>();
            while (lStates.Count > 1)
            {
                int idx = RandomGenerator.Next(lStates.Count);
                lPermuted.Add(lStates[idx]);
                lStates.RemoveAt(idx);
            }
            lPermuted.Add(lStates[0]);
            return lPermuted;
        }

        public List<Predicate> RunSatSolver()
        {
            List<Formula> lFormulas = new List<Formula>(m_lHiddenFormulas);
            List<List<Predicate>> l = RunSatSolver(lFormulas, 1, null);
            return l[0];
        }

        public List<List<Predicate>> RunSatSolver(List<Formula> lFormulas, int cAttempts)
        {
            return RunSatSolver(lFormulas, cAttempts, null);
        }

        private string m_sFFOutput;
        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            m_sFFOutput += outLine.Data + "\n";
        }

        TimeSpan tsTotalRunSatSolver = new TimeSpan();
        TimeSpan tsTotalRunMiniSat = new TimeSpan();
        long cRuns = 0, iSize = 0;



        public bool ApplyUnitPropogation(List<Formula> lFormulas, HashSet<Predicate> lAssignment)
        {
            DateTime dtStart = DateTime.Now;
            List<PredicateFormula> lLearnedPredicates = new List<PredicateFormula>();
            for(int iFormula = 0 ; iFormula < lFormulas.Count ; iFormula++)
            {
                Formula f = lFormulas[iFormula];
                if (f is PredicateFormula)
                {
                    lLearnedPredicates.Add((PredicateFormula)f);
                    lFormulas[iFormula] = null;
                }
            }
            int cIndexes = 0, cValidIndexes = 0, cLearned = 0;
            DateTime dt1 = DateTime.Now;
            TimeSpan ts1 = new TimeSpan(0), ts2 = new TimeSpan(0), ts3 = new TimeSpan(0);


            int cReductions = 0;
            while (lLearnedPredicates.Count > 0)
            {
                HashSet<int> lIndexes = new HashSet<int>();
                List<Predicate> lKnown = new List<Predicate>();
                foreach (PredicateFormula pf in lLearnedPredicates)
                {
                    GroundedPredicate p = (GroundedPredicate)pf.Predicate.Canonical();
                    lKnown.Add(pf.Predicate);
                    lAssignment.Add(pf.Predicate);
                    if (m_dMapPredicatesToFormulas.ContainsKey(p))
                    {
                        List<int> lRelevantFormulas = m_dMapPredicatesToFormulas[p];
                        foreach (int idx in lRelevantFormulas)
                            lIndexes.Add(idx);
                    }
                    else if(p.Name == Domain.OPTION_PREDICATE)
                    {
                        for (int i = 0; i < lFormulas.Count; i++)
                            lIndexes.Add(i);
                    }
                }
                DateTime dt2 = DateTime.Now;
                ts1 += dt2 - dt1;
                dt1 = dt2;
                lLearnedPredicates = new List<PredicateFormula>();
                foreach (int idx in lIndexes)
                {
                    cIndexes++;
                    CompoundFormula cfPrevious = (CompoundFormula)lFormulas[idx];
                    if (cfPrevious != null)
                    {
                        cValidIndexes++;

                        DateTime dt3 = DateTime.Now;
                        Formula fNew = cfPrevious.Reduce(lKnown);
                        cReductions++;
                        ts2 += DateTime.Now - dt3;

                        if (fNew.IsFalse(null))
                            return false;

                        if (fNew is PredicateFormula)
                        {
                            if (!fNew.IsTrue(null))
                                lLearnedPredicates.Add((PredicateFormula)fNew);
                            lFormulas[idx] = null;
                        }
                        else
                        {
                            CompoundFormula cfNew = (CompoundFormula)fNew;
                            if (cfNew.IsSimpleConjunction())
                            {
                                foreach (PredicateFormula pf in cfNew.Operands)
                                {
                                    if (!fNew.IsTrue(null))
                                        lLearnedPredicates.Add(pf);
                                }
                                lFormulas[idx] = null;
                            }
                            else
                                lFormulas[idx] = fNew;
                        }
                    }
                }
                dt2 = DateTime.Now;
                ts3 += dt2 - dt1;
                dt1 = dt2;

            }

            TimeSpan tsCurrent = DateTime.Now - dtStart;
            tsTotalTime += tsCurrent;
            //Debug.WriteLine("ApplyUnitPropogation: indexes " + cIndexes + ", valid " + cValidIndexes + ", learned " + cLearned + " time " + tsCurrent.TotalSeconds);
            //Debug.WriteLine(cReductions + ", " + ts1.TotalSeconds + ", " + ts2.TotalSeconds + ", " + ts3.TotalSeconds);
            return true;
        }




        public List<List<Predicate>> RunSatSolver(List<Formula> lFormulas, int cAttempts, List<Predicate> lProblematicTag)
        {
            HashSet<Predicate> lPartialAssignment = new HashSet<Predicate>();
            if (!ApplyUnitPropogation(lFormulas, lPartialAssignment))
                return new List<List<Predicate>>();
            bool bAllNull  = true;

            bool bDoneIO = false;
            while(!bDoneIO)
            {
                try
                {
                    File.Delete(Problem.Domain.Path + "solution.sat");
                    File.Delete(Problem.Domain.Path + "problem.sat");
                    File.Delete(Problem.Domain.Path + "problem.sat.debug");
                    bDoneIO = true;
                }
                catch (Exception e) { }
            }

            DateTime dtStart = DateTime.Now;
            List<List<Predicate>> lAssignments = new List<List<Predicate>>();

            foreach (Formula f in lFormulas)
                if (f != null)
                    bAllNull = false;
            if (bAllNull)//solved by unit propogation
            {
                lAssignments.Add(new List<Predicate>(lPartialAssignment));
                return lAssignments;
            }
            
            List<Predicate> lAssignment = lProblematicTag;
            while (cAttempts > 0)
            {
                if (lAssignment != null)
                {
                    CompoundFormula cfOr = new CompoundFormula("or");
                    foreach (Predicate pAssigned in lAssignment)
                        cfOr.AddOperand(pAssigned.Negate());
                    lFormulas.Add(cfOr);
                }
                bool bDone = false;
                HashSet<int> lParticipatingVariables = null;
                while (!bDone)
                {
                    try
                    {

                        StreamWriter sw = new StreamWriter(Problem.Domain.Path + "problem.sat");
                        lParticipatingVariables = WriteCNF(lFormulas, sw);
                        sw.Close();
                        bDone = true;
                    }
                    catch (Exception e) { }
                }


                foreach (Process pFF in Process.GetProcessesByName("MiniSat.exe"))
                {
                    if (pFF.ProcessName.ToLower().Contains("MiniSat.exe"))
                        pFF.Kill();
                }

                // additions/

                /*ProcessStartInfo psi = new ProcessStartInfo();
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                psi.WorkingDirectory = Problem.Domain.Path;
                psi.FileName = Problem.Domain.Path + @"minisat.exe";
                psi.Arguments = "problem.sat solution.sat";
                psi.UseShellExecute = false;
                Process pr = new Process();
                pr.StartInfo = psi;
                pr.Start();*/
               


                Process p = new Process();
                p.StartInfo.WorkingDirectory = Problem.Domain.Path;
                p.StartInfo.FileName = Program.BASE_PATH + "\\Planners\\minisat.exe";
                //p.StartInfo.WorkingDirectory = @"R:\IMAP\bin\Debug\Box-5";
                p.StartInfo.WorkingDirectory = Problem.Domain.Path;
                p.StartInfo.Arguments = "problem.sat solution.sat";
                
                //p.StartInfo.Arguments = Problem.Domain.Path + "problem.sat " + Problem.Domain.Path + "solution.sat";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = false;
                p.StartInfo.RedirectStandardError = true;
                
                //p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                m_sFFOutput = "";

                DateTime dtBeforeMiniSat = DateTime.Now;
                p.Start();

                //p.BeginOutputReadLine();
                /*
                msProblem.Position = 0;
                BinaryReader srModels = new BinaryReader(msProblem);

                while (srModels.PeekChar() >= 0)
                    p.StandardInput.BaseStream.WriteByte(srModels.ReadByte());
                p.StandardInput.BaseStream.WriteByte(0);
                
                char[] aChars = GetCNF(lFormulas);
                foreach (char c in aChars)
                {
                    p.StandardInput.Write(c);
                    if (c == (char)0)
                        break;
                }*/
                //WriteCNF(lFormulas, p.StandardInput);

                //iSize += p.StandardInput.BaseStream.Length / 1000;

                //p.StandardInput.Close();

                if (!p.WaitForExit(1000 * 60 * 2))//2 minutes max
                {
                    p.Kill();
                    return null;
                }
                tsTotalRunMiniSat += DateTime.Now - dtBeforeMiniSat;

                try
                {                 
                    //StreamReader sr = p.StandardOutput;

                    StreamReader sr = new StreamReader(Problem.Domain.Path + "solution.sat");
                    m_sFFOutput = sr.ReadToEnd();
                    sr.Close();

                    if (m_sFFOutput.Contains("UNSAT"))
                        break;
                    else
                    {
                        if (m_sFFOutput.StartsWith("SAT"))
                            m_sFFOutput = m_sFFOutput.Substring(4);

                        lAssignment = new List<Predicate>(lPartialAssignment);
                        foreach (string sVariable in m_sFFOutput.Split(' '))
                        {
                            bool bNegate = false;
                            int idx = int.Parse(sVariable);
                            if (idx < 0)
                            {
                                idx *= -1;
                                bNegate = true;
                            }
                            if (lParticipatingVariables.Contains(idx))
                            {
                                Predicate pAssigned = m_lSATVariables[idx - 1];
                                if (bNegate)
                                    pAssigned = pAssigned.Negate();
                                lAssignment.Add(pAssigned);
                            
                            }
                        }

                        lAssignments.Add(lAssignment);
                    }
                    cAttempts--;
                }
                catch (Exception e)
                {
                    Console.WriteLine("BUGBUG");
                }
            }
            tsTotalRunSatSolver += DateTime.Now - dtStart;
            cRuns++;

            //if (cRuns % 10 == 0)
            //    Console.WriteLine("\n" + cRuns + ")" + tsTotalRunSatSolver.TotalSeconds / cRuns + ", " + tsTotalRunMiniSat.TotalSeconds / cRuns + ", " + iSize / cRuns);

            return lAssignments;
        }


            /*
        public List<List<Predicate>> RunSatSolver(List<Formula> lFormulas, int cAttempts, List<Predicate> lProblematicTag)
        {

            ConstraintSystem s1 = ConstraintSystem.CreateSolver();
            SolverContext context = SolverContext.GetContext();
            Model model = context.CreateModel();

            Decision d1 = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "v1");
            Decision d2 = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "v2");
            Decision d3 = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "v3");
            Decision d4 = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "v4");

            model.AddDecisions(d1, d2, d3, d4);

            model.AddConstraint("c12", d1 != d2);
            model.AddConstraint("c13", d1 != d3);
            model.AddConstraint("c14", d1 != d4);
            model.AddConstraint("c23", d2 != d3);
            model.AddConstraint("c24", d2 != d4);
            model.AddConstraint("c34", d3 != d4);

            model.AddConstraint("c1234", d1 + d2 + d3 + d4 == 1);

            
            Solution solution2 = context.Solve();
            Console.WriteLine(d1);
            Console.WriteLine(d2);
            Console.WriteLine(d3);
            Console.WriteLine(d4);
            SolverContext context = SolverContext.GetContext();
            Model model = context.CreateModel();
            Dictionary<int,Decision> dDecisionVariables = new Dictionary<int,Decision>();
            int iForumla = 0;
            foreach (Formula f in lFormulas)
            {
                if (f == null)
                    continue;
                if (f is PredicateFormula)
                {
                    Predicate p = ((PredicateFormula)f).Predicate;
                    int idx = GetPredicateIndex(p);
                    Decision d = null;
                    if (!dDecisionVariables.TryGetValue(idx, out d))
                    {
                        d = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "V" + idx);
                        dDecisionVariables[idx] = d;
                        model.AddDecision(d);
                    }
                    model.AddConstraints("c" + iForumla, d == 1);
                }
                else
                {
                    CompoundFormula cf = (CompoundFormula)f;
                    if (cf.IsSimpleFormula())
                    {
                        Term t = GetClause(cf, model, dDecisionVariables);

                        model.AddConstraints("c" + iForumla, t);
                    }
                    else
                    {
                        CompoundFormula cfAnd = cf.ToCNF();
                        int iSubFormula = 0;
                        foreach (CompoundFormula cfSub in cfAnd.Operands)
                        {
                            Term t = GetClause(cfSub, model, dDecisionVariables);

                            model.AddConstraints("c" + iForumla + "." + iSubFormula, t);

                        }
                    }

                    iForumla++;
                }
            }

            //Solution sol = context.Solve(new ConstraintProgrammingDirective());
            Solution sol = context.Solve();
            List<List<Predicate>> lAssignments = new List<List<Predicate>>();
            while (sol.Quality != SolverQuality.Infeasible)
            {
                
                List<Predicate> lAssignment = new List<Predicate>();
                foreach (Decision d in dDecisionVariables.Values)
                {
                    bool bNegate = false;
                    int idx = int.Parse(d.Name.Substring(1));
                    int val = (int)d.ToDouble();
                    if (val == 0)
                    {
                        bNegate = true;
                    }
                    Predicate pAssigned = m_lSATVariables[idx - 1];
                    if (bNegate)
                        pAssigned = pAssigned.Negate();
                    lAssignment.Add(pAssigned);

                }
                lAssignments.Add(lAssignment);
                cAttempts--;
            }

            return lAssignments;
        }

*/
        /*
        public List<List<Predicate>> RunSatSolver(List<Formula> lFormulas, int cAttempts, List<Predicate> lProblematicTag)
        {
            DateTime dtStart = DateTime.Now;
            Context ctx = new Context();
            Solver s = ctx.MkSolver();
            Dictionary<int, BoolExpr> dDecisionVariables = new Dictionary<int, BoolExpr>();
            int iForumla = 0;

            List<List<int>> lClauses = GetCNFClauses(lFormulas);
            foreach (List<int> lClause in lClauses)
            {
                List<BoolExpr> lExp = new List<BoolExpr>();
                foreach (int iVar in lClause)
                {
                    if (iVar == 0)
                        continue;
                    int idx = iVar;
                    if (iVar < 0)
                        idx = iVar * -1;

                    BoolExpr d = null;
                    if (!dDecisionVariables.TryGetValue(idx, out d))
                    {
                        d = (BoolExpr)ctx.MkConst("V" + idx, ctx.MkBoolSort());
                        dDecisionVariables[idx] = d;
                    }

                    if (iVar < 0)
                        lExp.Add(ctx.MkNot(d));
                    else
                        lExp.Add(d);
                }
                BoolExpr eOr = ctx.MkOr(lExp.ToArray());
                s.Assert(eOr);
            }


            Status bSat = s.Check();
            List<List<Predicate>> lAssignments = new List<List<Predicate>>();
            while (bSat == Status.SATISFIABLE)
            {
                Model m = s.Model;               
                List<Predicate> lAssignment = new List<Predicate>();
                foreach (FuncDecl d in m.Decls)
                {
                    bool bNegate = false;
                    int idx = int.Parse(d.Name.ToString().Substring(1));
                    Z3_lbool val = ((BoolExpr)m.ConstInterp(d)).BoolValue;
                    if (val == Z3_lbool.Z3_L_FALSE)
                    {
                        bNegate = true;
                    }
                    Predicate pAssigned = m_lSATVariables[idx - 1];
                    if (bNegate)
                        pAssigned = pAssigned.Negate();
                    lAssignment.Add(pAssigned);

                }
                lAssignments.Add(lAssignment);
                cAttempts--;
            }
            tsTotalRunSatSolver += DateTime.Now - dtStart;
            cRuns++;

            if (cRuns % 10 == 0)
                Console.WriteLine("\n" + cRuns + ")" + tsTotalRunSatSolver.TotalSeconds / cRuns );

            return lAssignments;
        }
            
/*
        private Term GetClause(CompoundFormula cfSimple, Model model, Dictionary<int, Decision> dDecisionVariables)
        {
            HashSet<Predicate> lPredicates = cfSimple.GetAllPredicates();
            SumTermBuilder sum = new SumTermBuilder(lPredicates.Count);
            foreach (Predicate p in lPredicates)
            {
                int idx = GetPredicateIndex(p);
                Decision d = null;
                if (!dDecisionVariables.TryGetValue(idx, out d))
                {
                    d = new Decision(Microsoft.SolverFoundation.Services.Domain.Boolean, "V" + idx);
                    dDecisionVariables[idx] = d;
                    model.AddDecision(d);
                }

                if (p.Negation)
                    sum.Add(1 - d);
                else
                    sum.Add(d);


            }
            Term t = null;
            if (cfSimple.Operator == "or")
                t = sum.ToTerm() >= 1;
            else if (cfSimple.Operator == "oneof")
                t = sum.ToTerm() == 1;
            else
                Console.WriteLine("BUGBUG");

            return t;
        }

        private Dictionary<int, CspTerm> AddConstraints(List<Formula> lFormulas, ConstraintSystem s)
        {
            List<List<int>> lIntCluases = GetCNFClauses(lFormulas);
            Dictionary<int, CspTerm> dTerms = new Dictionary<int, CspTerm>();

            foreach (List<int> lClause in lIntCluases)
            {
                List<CspTerm> lTerms = new List<CspTerm>();
                foreach (int literal in lClause)
                {
                    if (literal != 0)
                    {
                        int var = Math.Abs(literal);
                        CspTerm tVariable = null;
                        if (!dTerms.TryGetValue(var, out tVariable))
                        {
                            tVariable = s.CreateBoolean(var);
                            dTerms[var] = tVariable;
                        }
                        if (literal > 0)
                        {
                            lTerms.Add(tVariable);
                        }
                        else
                        {
                            lTerms.Add(s.Neg(tVariable));
                        }
                    }
                }
                //CspTerm tOr = s.Or(lTerms.ToArray());
                CspTerm tOr = s.And(lTerms[0], lTerms[1]);
                s.AddConstraints(tOr);
            }

            return dTerms;
        
        }
*/

        private List<List<int>> GetCNFClauses(List<Formula> lFormulas)
        {

            List<List<int>> lIntCluases = new List<List<int>>();

            foreach (Formula f in lFormulas)
            {
                if (f == null)
                    continue;
                if (f is PredicateFormula)
                {
                    Predicate p = ((PredicateFormula)f).Predicate;
                    List<int> lClause = new List<int>();
                    int idx = GetPredicateIndex(p);
                    if (p.Negation)
                        idx *= -1;
                    lClause.Add(idx);
                    lClause.Add(0);
                    lIntCluases.Add(lClause);
                }
                else
                {
                    CompoundFormula cf = (CompoundFormula)f;

                    if (cf.SATSolverClauses.Count == 0)
                    {
                        if (cf.Operator == "or")
                        {
                            List<int> lClause = new List<int>();
                            foreach (PredicateFormula pf in cf.Operands)
                            {
                                Predicate p = pf.Predicate;
                                int idx = GetPredicateIndex(p);
                                if (p.Negation)
                                    idx *= -1;
                                lClause.Add(idx);
                            }
                            lClause.Add(0);
                            cf.SATSolverClauses.Add(lClause);
                        }
                        else if (cf.Operator == "oneof")
                        {
                            //using a simple conversion here - (oneof p1 p2 p3) = (and (or ~p1 ~p2) (or ~p1 ~p3) (or ~p2 ~p3) (or p1 p2 p3))
                            List<int> lClause = null;
                            if (cf.IsSimpleOneOf())
                            {
                                List<Predicate> lPredicates = new List<Predicate>(cf.GetAllPredicates());
                                for (int i = 0; i < lPredicates.Count - 1; i++)
                                {
                                    int idx1 = GetPredicateIndex(lPredicates[i]);
                                    if (!lPredicates[i].Negation)
                                        idx1 *= -1;
                                    for (int j = i + 1; j < lPredicates.Count; j++)
                                    {
                                        int idx2 = GetPredicateIndex(lPredicates[j]);
                                        if (!lPredicates[j].Negation)
                                            idx2 *= -1;
                                        lClause = new List<int>();
                                        lClause.Add(idx1);
                                        lClause.Add(idx2);
                                        lClause.Add(0);
                                        cf.SATSolverClauses.Add(lClause);
                                    }
                                }
                                lClause = new List<int>();
                                foreach (Predicate p in lPredicates)
                                {
                                    int idx = GetPredicateIndex(p);
                                    if (p.Negation)
                                        idx *= -1;
                                    lClause.Add(idx);
                                }
                                lClause.Add(0);
                                cf.SATSolverClauses.Add(lClause);
                            }
                            else
                            {
                                //using a simple conversion here - (oneof p1 (or p2 p3)) = (and (or ~p1 ~p2) (or ~p1 ~p3) (or p1 p2 p3))
                                int cOperands = cf.Operands.Count;
                                CompoundFormula cfOrAll = new CompoundFormula("or");
                                HashSet<int> hsAll = new HashSet<int>();
                                for (int i = 0; i < cOperands; i++)
                                {
                                    if (cf.Operands[i] is PredicateFormula || ((CompoundFormula)(cf.Operands[i])).IsSimpleDisjunction())
                                    {
                                        HashSet<Predicate> lFirstPredicates = cf.Operands[i].GetAllPredicates();
                                        for (int j = i + 1; j < cOperands; j++)
                                        {
                                            HashSet<Predicate> lSecondPredicates = cf.Operands[j].GetAllPredicates();
                                            foreach(Predicate pFirst in lFirstPredicates)
                                            {
                                                int idx1 = GetPredicateIndex(pFirst);
                                                if (pFirst.Negation)
                                                    hsAll.Add(idx1 * -1);
                                                else
                                                {
                                                    hsAll.Add(idx1);
                                                    idx1 *= -1;
                                                }
                                                foreach(Predicate pSecond in lSecondPredicates)
                                                {
                                                    int idx2 = GetPredicateIndex(pSecond);
                                                    if (pSecond.Negation)
                                                        hsAll.Add(idx2 * -1);
                                                    else
                                                    {
                                                        hsAll.Add(idx2);
                                                        idx2 *= -1;
                                                    }
                                                    lClause = new List<int>();
                                                    lClause.Add(idx1);
                                                    lClause.Add(idx2);
                                                    lClause.Add(0);
                                                    cf.SATSolverClauses.Add(lClause);
                                                }
                                            }
                                        }
                                    }
                                    else //not handling the case of neting "and" for now
                                        throw new NotImplementedException();
                                }
                                lClause = new List<int>();
                                foreach (int i in hsAll)
                                    lClause.Add(i);
                                lClause.Add(0);
                                cf.SATSolverClauses.Add(lClause);
                            }
                        }
                        else
                            throw new NotImplementedException();
                    }
                    lIntCluases.AddRange(cf.SATSolverClauses);
                }
            }
#if DEBUG
            /*StreamWriter swDebug = null;
            swDebug = new StreamWriter(Problem.Domain.Path + "problem.sat.debug");
            foreach (Formula f in lFormulas)
                swDebug.WriteLine(f);
            swDebug.Close();*/
#endif


           return lIntCluases;
        }


        private int GetPredicateIndex(Predicate p)
        {
            bool bNegate = p.Negation;
            if (bNegate)
                p = p.Negate();
            if (!m_dSATVariables.ContainsKey(p))
            {
                m_dSATVariables[p] = m_dSATVariables.Count + 1;
                m_lSATVariables.Add(p);
            }
            int idx = m_dSATVariables[p];
            return idx;
        }
        private string GetPredicateString(Predicate p)
        {
            int idx = GetPredicateIndex(p);
            if (p.Negation)
                return "-" + idx;
            return "" + idx;
        }

        private List<Predicate> m_lSATVariables = new List<Predicate>();
        private Dictionary<Predicate, int> m_dSATVariables = new Dictionary<Predicate, int>();

        private MemoryStream WriteCNF(List<Formula> lFormulas)
        {


            List<List<int>> lIntCluases = GetCNFClauses(lFormulas);
            MemoryStream ms = new MemoryStream();

            StreamWriter sw = new StreamWriter(ms);
            sw.WriteLine("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count);
            foreach (List<int> lClause in lIntCluases)
            {
                foreach (int i in lClause)
                    sw.Write(i + " ");
                sw.WriteLine();
            }

            sw.Flush();

            MemoryStream ms2 = new MemoryStream();
            ms.Position = 0;
            BinaryReader br = new BinaryReader(ms);
            BinaryWriter bw = new BinaryWriter(ms2);
            byte b = br.ReadByte();
            while (b >= 0)
            {
                bw.Write(b);
                if (br.BaseStream.Position == br.BaseStream.Length)
                {
                    break;
                }
                b = br.ReadByte();
            }
            bw.Write('\0');
            bw.Flush();
            //sw.Close();
            return ms2;
        }

        private HashSet<int> WriteCNF(List<Formula> lFormulas, StreamWriter sw)
        {
            List<List<int>> lIntCluases = GetCNFClauses(lFormulas);
            HashSet<int> lParticipatingVariables = new HashSet<int>();
            //MemoryStream ms = new MemoryStream();
            
            sw.WriteLine("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count);
            foreach (List<int> lClause in lIntCluases)
            {
                string s = "";
                foreach (int i in lClause)
                {
                    if(i != 0)
                        lParticipatingVariables.Add(Math.Abs(i));
                    s += i + " ";
                }
                sw.WriteLine(s);
                iSize += s.Length;
            }
            /*
            StringBuilder sb = new StringBuilder();
            //StringWriter sw1 = new StringWriter(sb);
            //sw1.WriteLine("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count);
            sb.Append("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count + "\n");
            foreach (List<int> lClause in lIntCluases)
            {
                string s = "";
                foreach (int i in lClause)
                    s += i + " ";
                //sw1.WriteLine(s);
                sb.Append(s + "\n");
                iSize += s.Length;
            }
             sw.WriteLine(sb.ToString());
           */
            //sw.Write(0);
            sw.Flush();
            return lParticipatingVariables;
        }

        private int CopyToCharArray(char[] a, int iStart, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                a[iStart + i] = s[i];
            }
            return iStart + s.Length;
        }
        private char[] GetCNF(List<Formula> lFormulas)
        {
            List<List<int>> lIntCluases = GetCNFClauses(lFormulas);
            int cChars = 0;
            foreach (List<int> lClause in lIntCluases)
            {
                cChars += lClause.Count;
            }
            char[] aChars = new char[cChars * 10];
            int iCurrentChar = 0;
            iCurrentChar = CopyToCharArray(aChars, iCurrentChar, "p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count);
            foreach (List<int> lClause in lIntCluases)
            {
                string s = "";
                foreach (int i in lClause)
                    s += i + " ";
                iCurrentChar = CopyToCharArray(aChars, iCurrentChar, s);
                iSize += s.Length;
            }
            /*
            StringBuilder sb = new StringBuilder();
            //StringWriter sw1 = new StringWriter(sb);
            //sw1.WriteLine("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count);
            sb.Append("p cnf " + m_dSATVariables.Count + " " + lIntCluases.Count + "\n");
            foreach (List<int> lClause in lIntCluases)
            {
                string s = "";
                foreach (int i in lClause)
                    s += i + " ";
                //sw1.WriteLine(s);
                sb.Append(s + "\n");
                iSize += s.Length;
            }
             sw.WriteLine(sb.ToString());
           */
            aChars[iCurrentChar] = (char)0;
            return aChars;
        }

        public PartiallySpecifiedState GetPartiallySpecifiedState()
        {
            PartiallySpecifiedState pss = new PartiallySpecifiedState(this);
            if(SDRPlanner.EnforceCNF)
                m_cfCNFHiddenState = (CompoundFormula)m_cfCNFHiddenState.ToCNF();
            return pss;
        }

        /*
        private CompoundFormula ToCNF(List<CompoundFormula> lHidden)
        {
            CompoundFormula cfAnd = new CompoundFormula("and");
            foreach (CompoundFormula cfHidden in lHidden)
            {
                cfAnd.AddOperand(cfHidden);
            }
            CompoundFormula cfCNF = cfAnd.ToCNF();
            return cfAnd;
        }
         * */

        public bool ConsistentWith(Predicate p)
        {

            List<Predicate> lKnown = new List<Predicate>(Observed);
            lKnown.Add(p);
            Formula fReduced = m_cfCNFHiddenState.Reduce(lKnown);
            if (fReduced.IsFalse(lKnown))
                return false;
            return true;
            /*
            List<Predicate> lKnown = new List<Predicate>();
            lKnown.Add(p);
            if (m_cfCNFHiddenState.IsFalse(lKnown))
                return false;
            return true;*/
        }

        int count_revisions = 0;
        public HashSet<int> ReviseInitialBelief(Formula fObserve, PartiallySpecifiedState pssLast)
        {
            DateTime dtBefore = DateTime.Now;
            Stack<PartiallySpecifiedState> sTrace = new Stack<PartiallySpecifiedState>();
            Stack<List<Formula>> sForumalsTrace = new Stack<List<Formula>>();
            PartiallySpecifiedState pssCurrent = pssLast, pssSuccessor = null;
            HashSet<int> hsModifiedClauses = new HashSet<int>();
            //Formula fToRegress = fObserve, fRegressed = null;
            bool bTrueRegression = false;
            int cSteps = 0;

            count_revisions++;
            List<Formula> lCurrentFormulas = new List<Formula>();
            List<Formula> lRegressedFormulas = new List<Formula>();

            lCurrentFormulas.Add(fObserve);

            //TimeSpan ts1 = new TimeSpan(0), ts2 = new TimeSpan(0);
            //DateTime dtStart = DateTime.Now;
            while (pssCurrent.Predecessor != null)
            {
                sTrace.Push(pssCurrent);
                sForumalsTrace.Push(lCurrentFormulas);
                lRegressedFormulas = new List<Formula>();
                HashSet<Predicate> hsNew = new HashSet<Predicate>();
                foreach (Formula fCurrent in lCurrentFormulas)
                {
                    if (fCurrent.IsTrue(pssCurrent.Observed))
                        continue;//used to be break but I think that if we got here then there is no point in continuing...
                    //is false doesn't properly work here
                    //Debug.Assert(fCurrent.IsFalse(pssCurrent.Observed), "Rgression of an observation returned false");
                    //pssCurrent.GeneratingAction.ClearConditionsChoices();
                    //pssCurrent.GeneratingAction.RemoveImpossibleOptions(pssCurrent.Observed); Need to this after the regression (below)
                    //DateTime dt = DateTime.Now;
                    Formula fRegressed = pssCurrent.RegressObservation(fCurrent);
                    //ts1 += DateTime.Now - dt;
                    if (fRegressed is CompoundFormula)
                    {
                        CompoundFormula cf = (CompoundFormula)fRegressed;
                        if (cf.Operator != "and")
                            lRegressedFormulas.Add(fRegressed);
                        else
                            foreach (Formula f in cf.Operands)
                                lRegressedFormulas.Add(f);

                    }
                    else
                        lRegressedFormulas.Add(fRegressed);

                    //dt = DateTime.Now;
                    //must be after the regression so as not to make everything already known
                    if (!SDRPlanner.OptimizeMemoryConsumption)
                        hsNew.UnionWith(pssCurrent.AddObserved(fCurrent));
                    //ts2 += DateTime.Now - dt;
                    //pssCurrent.AddObserved(fToRegress); //Not sure that this is valid!

                    if (!fRegressed.Equals(fCurrent))
                        //if (bTrueRegression || !fRegressed.Equals(fToRegress))
                        bTrueRegression = true;
                }
                if (hsNew.Count > 0 && pssSuccessor != null)
                    pssSuccessor.PropogateObservedPredicates(hsNew);
                pssSuccessor = pssCurrent;
                pssCurrent = pssCurrent.Predecessor;
                cSteps++;
                lCurrentFormulas = lRegressedFormulas;
            }

            Formula fFinal = null;
            if (lCurrentFormulas.Count == 0)
                return hsModifiedClauses;
            if (lCurrentFormulas.Count == 1)
                fFinal = lCurrentFormulas[0].Reduce(Observed);
            else
            {
                CompoundFormula cf = new CompoundFormula("and");
                foreach (Formula f in lCurrentFormulas)
                {
                    Formula fReduced = f.Reduce(Observed).Simplify();
                    Formula fCNF = null;
                    if (fReduced is CompoundFormula)
                        fCNF = ((CompoundFormula)fReduced).ToCNF();
                    else
                        fCNF = fReduced;
                    cf.AddOperand(fCNF);
                }
                fFinal = cf;

            }
            //Debug.WriteLine("Total time in regressobs " + ts1.TotalSeconds + " propogate " + ts2.TotalSeconds +
            //     " all " + (DateTime.Now - dtStart).TotalSeconds + " size " + fFinal.Size);

            if (fFinal.IsTrue(null))
                return hsModifiedClauses;
            DateTime dtAfterRegression = DateTime.Now;

            DateTime dtAfterReasoning = DateTime.Now;
            //Seems likely but I am unsure: if there was no real regression, then learned things can be applied to all states as is

            HashSet<Predicate> lLearned = null;
            if (BeliefState.UseEfficientFormulas)
                lLearned = AddReasoningFormulaEfficient(fFinal);
            else
                lLearned = AddReasoningFormula(fFinal, hsModifiedClauses);

            if (lLearned.Count > 0)
            {
                //HashSet<Predicate> lLearned = pssCurrent.ApplyReasoning(); not needed since we got the learned predicates from the belief update
                if (!SDRPlanner.ComputeCompletePlanTree)
                    pssCurrent.AddObserved(lLearned);
                dtAfterReasoning = DateTime.Now;
                if (bTrueRegression)
                {
                    //while (bUpdate && sTrace.Count > 0)
                    while (sTrace.Count > 0 && lLearned.Count > 0)
                    {
                        pssCurrent = sTrace.Pop();
                        //bUpdate = pssCurrent.PropogateObservedPredicates();
                        lLearned = pssCurrent.PropogateObservedPredicates(lLearned);
                    }
                    if (SDRPlanner.OptimizeMemoryConsumption && lLearned.Count > 0)
                    {
                        pssLast.AddObserved(lLearned);
                    }
                }
                else
                {
                    if (!SDRPlanner.OptimizeMemoryConsumption)
                    {
                        while (sTrace.Count > 0)
                        {
                            pssCurrent = sTrace.Pop();
                            pssCurrent.AddObserved(lLearned);
                        }
                    }
                    else
                        pssLast.AddObserved(lLearned);
                }

            }
            /*
            Console.WriteLine("Time for belief update: " +
                (DateTime.Now - dtBefore).TotalSeconds +
                " regression " + (dtAfterRegression - dtBefore).TotalSeconds +
                " reasoning " + (dtAfterReasoning - dtAfterRegression).TotalSeconds +
                " update " + (DateTime.Now - dtAfterReasoning).TotalSeconds);
            */


            return hsModifiedClauses;
        }

        public void SetProblematicTag(List<Predicate> lAssignment)
        {
            m_lProblematicTag = new List<Predicate>();
            foreach (Predicate p in lAssignment)
            {
                if (p is TimePredicate)
                {
                    TimePredicate tp = (TimePredicate)p;
                    if (tp.Time == 0)
                        m_lProblematicTag.Add(tp.Predicate);
                }
                //else
                //    throw new NotImplementedException();
            }
        }
    }
}
