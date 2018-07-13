using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMAP.SDRPlanners;
using IMAP.Formulas;
using IMAP.SDRPlanners;
using IMAP.Predicates;
using IMAP.General;

namespace IMAP.PlanTree
{
    public class ConditionalPlanTreeNode
    {
        public int ID { get; set; }
        public Action Action { get; set; }
        public ConditionalPlanTreeNode SingleChild { get; set; }
        public ConditionalPlanTreeNode FalseObservationChild { get; set; }
        public ConditionalPlanTreeNode TrueObservationChild { get; set; }
        private static int CountNodes = 0;
        public ConditionalPlanTreeNode()
        {
            ID = CountNodes++;
        }
        private string ToString(string sIndent, HashSet<int> lHistory)
        {
            if (lHistory.Contains(ID))
                return ")connect to " + ID;
            //HashSet<int> lNewHistory = new HashSet<int>(lHistory);
            lHistory.Add(ID);
            if (Action == null)
                return ")goal";
            string s = sIndent + ID + ") " + Action.Name + "\n";
            if (SingleChild != null)
                s += SingleChild.ToString(sIndent, lHistory);
            else
            {
                s += "branching...\n";
                if (FalseObservationChild != null)
                    s += FalseObservationChild.ToString(sIndent + "\t", lHistory);
                else
                    s += "Can't be false";
                s += "\n";
                if (TrueObservationChild != null)
                    s += TrueObservationChild.ToString(sIndent + "\t", lHistory);
                else
                    s += "Can't be true";
            }
            return s;
        }
        public override string ToString()
        {
            return ToString("", new HashSet<int>());
        }
        public static void Reset()
        {
            CountNodes = 0;
        }
        
        private Dictionary<Action, int> GetLatestTimingOfJointAction(Dictionary<Action, int> dictionary)
        {
            Dictionary<string, Dictionary<Action, int>> tempRes = new Dictionary<string, Dictionary<Action, int>>();
            foreach (var item in dictionary)
            {
                string cleanName = item.Key.Name.Substring(0, item.Key.Name.IndexOf("_t"));
                if (!tempRes.ContainsKey(cleanName))
                    tempRes.Add(cleanName, new Dictionary<Action, int>());

                if (!tempRes[cleanName].ContainsKey(item.Key))
                    tempRes[cleanName].Add(item.Key, item.Value);
            }
            Dictionary<Action, int> res = new Dictionary<Action, int>();
            foreach (var item in tempRes)
            {
                var tt = item.Value.OrderByDescending(x => x.Value).First();
                res.Add(tt.Key, tt.Value);
            }
            return res;
        }
        public Dictionary<int, List<Formula>> ScanPreconditions(Dictionary<int, List<Formula>> CollectedPreconditions)
        {
            if (Action != null)
            {
                int time = Action.GetTime();
                Formula currPreconditions = Action.Preconditions;

                if (!CollectedPreconditions.ContainsKey(time))
                    CollectedPreconditions.Add(time, new List<Formula>());

                CollectedPreconditions[time].Add(currPreconditions);
            }
            if (SingleChild != null)
            {
                SingleChild.ScanPreconditions(CollectedPreconditions);
            }
            if (FalseObservationChild != null)
            {
                FalseObservationChild.ScanPreconditions(CollectedPreconditions);
            }
            if (TrueObservationChild != null)
            {
                TrueObservationChild.ScanPreconditions(CollectedPreconditions);
            }
            return CollectedPreconditions;
        }
        public Dictionary<int, List<Formula>> ScanEffects(Dictionary<int,List<Formula>> CollectedEffects)
        {
            if (Action != null)
            {
                int time = Action.GetTime();
                Formula currEffects = Action.Effects;

                if (!CollectedEffects.ContainsKey(time))
                    CollectedEffects.Add(time, new List<Formula>());

                CollectedEffects[time].Add(currEffects);
            }
            if (SingleChild != null)
            {
                SingleChild.ScanEffects(CollectedEffects);
            }
            if (FalseObservationChild != null)
            {
                FalseObservationChild.ScanEffects(CollectedEffects);
            }
            if (TrueObservationChild != null)
            {
                TrueObservationChild.ScanEffects(CollectedEffects);
            }
            return CollectedEffects;
        }
        private Dictionary<Predicate, int> GetLatestTimingOfPredicate(Dictionary<Predicate, int> dictionary)
        {
            Dictionary<string, Dictionary<Predicate, int>> tempRes = new Dictionary<string, Dictionary<Predicate, int>>();
            foreach (var item in dictionary)
            {
                string cleanName = item.Key.ToString();
                if (!tempRes.ContainsKey(cleanName))
                    tempRes.Add(cleanName, new Dictionary<Predicate, int>());

                if (!tempRes[cleanName].ContainsKey(item.Key))
                    tempRes[cleanName].Add(item.Key, item.Value);
            }
            Dictionary<Predicate, int> res = new Dictionary<Predicate, int>();
            foreach (var item in tempRes)
            {
                var tt = item.Value.OrderByDescending(x => x.Value).First();
                res.Add(tt.Key, tt.Value);
            }
            return res;
        }
        private Dictionary<Formula, int> GetLatestTimingOfFormula(Dictionary<Formula, int> dictionary)
        {
            Dictionary<string, Dictionary<Formula, int>> tempRes = new Dictionary<string, Dictionary<Formula, int>>();
            foreach (var item in dictionary)
            {
                string cleanName = item.Key.ToString();
                if (!tempRes.ContainsKey(cleanName))
                    tempRes.Add(cleanName, new Dictionary<Formula, int>());

                if (!tempRes[cleanName].ContainsKey(item.Key))
                    tempRes[cleanName].Add(item.Key, item.Value);
            }
            Dictionary<Formula, int> res = new Dictionary<Formula, int>();
            foreach (var item in tempRes)
            {
                var tt = item.Value.OrderByDescending(x => x.Value).First();
                res.Add(tt.Key, tt.Value);
            }
            return res;
        }
        private Dictionary<Formula, int> GetEarliestTimingOfFormula(Dictionary<Formula, int> dictionary)
        {
            Dictionary<string, Dictionary<Formula, int>> tempRes = new Dictionary<string, Dictionary<Formula, int>>();
            foreach (var item in dictionary)
            {
                string cleanName = item.Key.ToString();
                if (!tempRes.ContainsKey(cleanName))
                    tempRes.Add(cleanName, new Dictionary<Formula, int>());

                if (!tempRes[cleanName].ContainsKey(item.Key))
                    tempRes[cleanName].Add(item.Key, item.Value);
            }
            Dictionary<Formula, int> res = new Dictionary<Formula, int>();
            foreach (var item in tempRes)
            {
                var tt = item.Value.OrderBy(x => x.Value).First();
                res.Add(tt.Key, tt.Value);
            }
            return res;
        }
        private Dictionary<Predicate, int> GetEarliestTimingOfPredicate(Dictionary<Predicate, int> dictionary)
        {
            Dictionary<string, Dictionary<Predicate, int>> tempRes = new Dictionary<string, Dictionary<Predicate, int>>();
            foreach (var item in dictionary)
            {
                string cleanName = item.Key.ToString();
                if (!tempRes.ContainsKey(cleanName))
                    tempRes.Add(cleanName, new Dictionary<Predicate, int>());

                if (!tempRes[cleanName].ContainsKey(item.Key))
                    tempRes[cleanName].Add(item.Key, item.Value);
            }
            Dictionary<Predicate, int> res = new Dictionary<Predicate, int>();
            foreach (var item in tempRes)
            {
                var tt = item.Value.OrderBy(x => x.Value).First();
                res.Add(tt.Key, tt.Value);
            }
            return res;
        }
        public void ScanLeafDepth(ref List<int> leafsDepth)
        {
            if (SingleChild != null)
            {
                if (SingleChild.Action == null)
                    leafsDepth.Add(Action.GetTime());
                else
                    SingleChild.ScanLeafDepth(ref leafsDepth);
            }
            if (FalseObservationChild != null)
            {
                if (FalseObservationChild.Action == null)
                    leafsDepth.Add(Action.GetTime());
                else           
                    FalseObservationChild.ScanLeafDepth(ref leafsDepth);
            }
            if (TrueObservationChild != null)
            {
                if (TrueObservationChild.Action == null)
                    leafsDepth.Add(Action.GetTime());
                else
                    TrueObservationChild.ScanLeafDepth(ref leafsDepth);

            }
        }
        public int MaxOperatingTime()
        {
            int maxTime = 0;
            if (Action != null)
            {
                int time = Action.GetTime();
                if (maxTime < time)
                    maxTime = time;
            }
            if (SingleChild != null)
            {
                int time = SingleChild.MaxOperatingTime();
                if (maxTime < time)
                    maxTime = time;
            }
            if (FalseObservationChild != null)
            {
                int time = FalseObservationChild.MaxOperatingTime();
                if (maxTime < time)
                    maxTime = time;
            }
            if (TrueObservationChild != null)
            {
                int time = TrueObservationChild.MaxOperatingTime();
                if (maxTime < time)
                    maxTime = time;
            }
            return maxTime;
        }
        private List<IMAP.Action> ScanEffectsForConst(Predicate goal)
        {
            List<IMAP.Action> ans = new List<IMAP.Action>();
            if (Action != null)
            {
                string actionName = Action.Name.Split('_')[0];
                if (!actionName.StartsWith("wait-goal"))
                {
                    if (Action.Effects != null)
                    {
                        Formula fEffects = Action.Effects;
                        if (fEffects is CompoundFormula)
                        {
                            CompoundFormula cf = (CompoundFormula)fEffects;
                            foreach (Formula formula in cf.Operands)
                            {
                                if (formula is PredicateFormula)
                                {
                                    PredicateFormula pf = (PredicateFormula)formula;
                                    if (pf.Predicate.ToString() == goal.ToString())
                                    {
                                        ans.Add(Action);
                                    }

                                }
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
            }
            if (SingleChild != null)
            {
                ans.AddRange(SingleChild.ScanEffectsForConst(goal));
            }
            if (FalseObservationChild != null)
            {
                ans.AddRange(FalseObservationChild.ScanEffectsForConst(goal));
            }
            if (TrueObservationChild != null)
            {
                ans.AddRange(TrueObservationChild.ScanEffectsForConst(goal));
            }
            return ans;
        }
        

        public ConditionalPlanTreeNode EnforceSpecificActionTimes(Dictionary<Action, int> jointActionsTimes)
        {
            ConditionalPlanTreeNode currentNode = null;
            if (Action != null)
            {
                Action newAction = Action.RemoveTime();
                string sActionName = newAction.Name;
            }
            return currentNode;
        }

        protected Dictionary<Predicate, int> CalcEarlyGoalAchievingTiming(List<Predicate> goals, List<string> jointActions)//,  bool takeJoint = false)
        {
            //Dictionary<Predicate, int> LatestGoalTime = new Dictionary<Predicate, int>();
            //Dictionary<Formula, int> EarliestGoalTime = new Dictionary<Formula, int>();
            Dictionary<Predicate, int> EarliestGoalTime = new Dictionary<Predicate, int>();
            foreach (Predicate goal in goals)
            {
                List<IMAP.Action> timestamps = ScanEffectsForConst(goal);
                if (timestamps.Count == 0)
                    continue;
                int latest = int.MaxValue;
                Action earliestAction = null;
                foreach (var item in timestamps)
                {
                    if (item.GetTime() < latest)
                    {
                        latest = item.GetTime();
                        earliestAction = item;
                    }
                }

                //Action goalTaker = earliestAction.Clone();
                //goalTaker.Effects.RemoveTime();
                //goalTaker.RemoveAgent();
                //EarliestGoalTime.Add(goalTaker.Effects, latest);

                string actionName = earliestAction.GetOperationName();
                /*if (takeJoint)
                {
                    if (jointActions != null && jointActions.Contains(actionName))
                        EarliestGoalTime.Add(goal, latest);
                }
                else
                {*/
                    //if (!jointActions.Contains(actionName))
                        EarliestGoalTime.Add(goal, latest);
                //}
            }

            return EarliestGoalTime;
        }
        private Dictionary<Action,int> CollectJointActionsWithTime(List<string> jointActions)
        {
            Dictionary<Action, int> ans = new Dictionary<Action, int>();
            if (Action != null)
            {
                foreach (var action in jointActions)
                {
                    if (Action.Name.StartsWith(action))
                    {
                        if (!ans.ContainsKey(Action))
                            ans.Add(Action, Action.GetTime());
                        if (ans[Action] > Action.GetTime())
                            ans[Action] = Action.GetTime();
                    }
                }
            }
            if (SingleChild != null)
            {
                foreach (var item in SingleChild.CollectJointActionsWithTime(jointActions))
                {
                    if (!ans.ContainsKey(item.Key))
                        ans.Add(item.Key, item.Value);
                    if (ans[item.Key] > item.Value)
                        ans[item.Key] = item.Value;
                }
            }
            if (FalseObservationChild != null)
            {
                foreach (var item in FalseObservationChild.CollectJointActionsWithTime(jointActions))
                {
                    if (!ans.ContainsKey(item.Key))
                        ans.Add(item.Key, item.Value);
                    if (ans[item.Key] > item.Value)
                        ans[item.Key] = item.Value;
                }
            }
            if (TrueObservationChild != null)
            {
                foreach (var item in TrueObservationChild.CollectJointActionsWithTime(jointActions))
                {
                    if (!ans.ContainsKey(item.Key))
                        ans.Add(item.Key, item.Value);
                    if (ans[item.Key] > item.Value)
                        ans[item.Key] = item.Value;
                }
            }
            return ans;
        }

        public PlanDetails ScanDetails(Domain domain, Problem problem)
        {
            DomainExtensiveInfo dei = new DomainExtensiveInfo(domain, problem);


            PlanDetails pd = new PlanDetails(this);


            List<string> jointActions = dei.GetJointActions();
            Dictionary<Action, int> jointActionsTimes = CollectJointActionsWithTime(jointActions);
            pd.JointActionsTimes = GetLatestTimingOfJointAction(jointActionsTimes);


            Dictionary<Predicate, int> goalTimings = CalcEarlyGoalAchievingTiming(dei.GetGoals(), jointActions);
            pd.GoalsTiming = GetEarliestTimingOfPredicate(goalTimings);

            //Dictionary<Predicate, int> goalTimingsWithJoint = CalcEarlyGoalAchievingTiming(domainExtensiveInfo.GetGoals(), jointActions,  true);
            //pd.JointGoalsTiming = GetEarliestTimingOfPredicate(goalTimingsWithJoint);

            return pd;

        }


        /*protected Dictionary<Formula, int> CalcLateGoalAchievingTiming(List<Predicate> goals)
        {
            //Dictionary<Predicate, int> LatestGoalTime = new Dictionary<Predicate, int>();
            Dictionary<Formula, int> LatestGoalTime = new Dictionary<Formula, int>();
            foreach (Predicate goal in goals)
            {
                List<IMAP.Action> timestamps = ScanEffectsForConst(goal);
                if (timestamps.Count == 0)
                    continue;
                int latest = 0;
                Action latestAction = null;
                foreach (var item in timestamps)
                {
                    if (item.GetTime() > latest)
                    {
                        latest = item.GetTime();
                        latestAction = item;
                    }
                }

                Action goalTaker = latestAction.Clone();
                goalTaker.Effects.RemoveTime();
                goalTaker.RemoveConstant();
                LatestGoalTime.Add(goalTaker.Effects, latest);
                //LatestGoalTime.Add(goal, latest);

            }
            return LatestGoalTime;
        }*/
        /*List<PredicateFormula> lpfs = new List<PredicateFormula>();
        foreach (string jointAction in lJointActions)
        {
            ParameterizedPredicate pp = new ParameterizedPredicate("current-time");
            pp.AddParameter(new Argument("", "t" + (iMaxJointActionDepth)));

            PredicateFormula pf = new PredicateFormula(pp);
            lpfs.Add(pf);
        }
        return lpfs;
    }
    
        /*internal void MaxJointActionDistance(List<string> lJointActions, int iCurrentDepth, ref int iMaxDistance)
        {
            if (Action != null)
            {
                bool containsJointAction = false;
                foreach (var action in lJointActions)
                {
                    if (Action.Name.Contains(action))
                        containsJointAction = true;
                }
                if (containsJointAction && (iMaxDistance < iCurrentDepth))
                    iMaxDistance = iCurrentDepth;
            }

            if (SingleChild!=null)
            {
                SingleChild.MaxJointActionDistance(lJointActions, iCurrentDepth + 1, ref iMaxDistance);
            }
            else
            {
                if (FalseObservationChild != null)
                    FalseObservationChild.MaxJointActionDistance(lJointActions, iCurrentDepth, ref iMaxDistance);
                if (TrueObservationChild != null)
                    TrueObservationChild.MaxJointActionDistance(lJointActions, iCurrentDepth, ref iMaxDistance);
            }
        }*/
        /*internal void DelayJointActions(List<string> lJointActions, int iCurrentDepth, int iMaxDistance)
        {
            if (SingleChild != null)
            {
                if (SingleChild.Action != null)
                {
                    bool containsJointAction = false;
                    foreach (var action in lJointActions)
                    {
                        if (SingleChild.Action.Name.Contains(action))
                            containsJointAction = true;
                    }
                    if (containsJointAction && (iCurrentDepth < iMaxDistance - 1))
                    {
                        ConditionalPlanTreeNode stayNode = new ConditionalPlanTreeNode();
                        stayNode.Action = new Action("Stay");
                        stayNode.SingleChild = SingleChild;
                        SingleChild = stayNode;
                    }
                }

                SingleChild.DelayJointActions(lJointActions, iCurrentDepth + 1, iMaxDistance);
            }

            if (FalseObservationChild != null)
            {
                if (FalseObservationChild.Action != null)
                {
                    bool containsJointAction = false;
                    foreach (var action in lJointActions)
                    {
                        if (FalseObservationChild.Action.Name.Contains(action))
                            containsJointAction = true;
                    }
                    if (containsJointAction && (iCurrentDepth < iMaxDistance - 1))
                    {
                        ConditionalPlanTreeNode stayNode = new ConditionalPlanTreeNode();
                        stayNode.Action = new Action("Stay");
                        stayNode.SingleChild = FalseObservationChild;
                        FalseObservationChild = stayNode;
                    }
                }

                FalseObservationChild.DelayJointActions(lJointActions, iCurrentDepth + 1, iMaxDistance);
            }
            if (TrueObservationChild != null)
            {
                if (TrueObservationChild.Action != null)
                {
                    bool containsJointAction = false;
                    foreach (var action in lJointActions)
                    {
                        if (TrueObservationChild.Action.Name.Contains(action))
                            containsJointAction = true;
                    }
                    if (containsJointAction && (iCurrentDepth < iMaxDistance - 1))
                    {
                        ConditionalPlanTreeNode stayNode = new ConditionalPlanTreeNode();
                        stayNode.Action = new Action("Stay");
                        stayNode.SingleChild = TrueObservationChild;
                        TrueObservationChild = stayNode;
                    }
                }

                TrueObservationChild.DelayJointActions(lJointActions, iCurrentDepth + 1, iMaxDistance);
            }
        }*/
    }
    }
