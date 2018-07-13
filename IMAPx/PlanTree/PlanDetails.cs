using IMAP.Predicates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.PlanTree
{
    public class PlanDetails
    {
        public ConditionalPlanTreeNode Plan { get; private set; }
        public Dictionary<Action, int> JointActionsTimes { get; set; }
        public Dictionary<Predicate, int> GoalsTiming { get; internal set; }
        public int MakeSpan
        {
            get
            {
                return Plan.MaxOperatingTime();
            }
        }
        public List<int> LeafsDepth
        {
            get
            {
                List<int> leafsDepth = new List<int>();
                Plan.ScanLeafDepth(ref leafsDepth);
                return leafsDepth;
            }
        }
        public TimeSpan PlanningTime { get; set; }
        public bool Valid { get; set; }
        public Constant ActiveAgent { get; set; }
        public Dictionary<Action, Action> OriginalActionsMapping { get; internal set; }

        public PlanDetails(ConditionalPlanTreeNode conditionalPlanTreeNode)
        {
            Plan = conditionalPlanTreeNode;
        }

        /*public PlanDetails()
        {
            JointActionsTimes = new Dictionary<Action, int>();
            GoalsTiming = new Dictionary<Predicate, int>();
        }*/
        /*internal bool Improves(PlanDetails prevPD)
        {
            return GetImprovements(prevPD).Count > 0;
        }*/
        /*internal Dictionary<Predicate, int> GetImprovements(PlanDetails planDetails)
        {
            Dictionary<Predicate, int> improvements = new Dictionary<Predicate, int>();
            Dictionary<Predicate, int> futureCompletions = planDetails.GoalsTiming;

            foreach (var myGoalTime in GoalsTiming)
            {
                if (futureCompletions.ContainsKey(myGoalTime.Key))
                {
                    if (futureCompletions[myGoalTime.Key] > myGoalTime.Value)
                        improvements.Add(myGoalTime.Key, myGoalTime.Value);
                }
            }
            return improvements;
        }*/
        /*internal void AddAchievements(PlanDetails planDetails)
        {
            foreach (var item in planDetails.JointActionsTimes)
            {
                if (JointActionsTimes.ContainsKey(item.Key))
                {
                    JointActionsTimes[item.Key] = item.Value;
                }
                else
                {
                    JointActionsTimes.Add(item.Key, item.Value);
                }
            }

            foreach (var item in planDetails.GoalsTiming)
            {
                if (GoalsTiming.ContainsKey(item.Key))
                {
                    GoalsTiming[item.Key] = item.Value;
                }
                else
                {
                    GoalsTiming.Add(item.Key, item.Value);
                }
            }
        }
        internal void PrintGoalTimes()
        {
            string ans = "";
            foreach (var item in GoalsTiming)
            {
                ans += item.Key + ":" + item.Value + ",";
            }
            Console.WriteLine(ans);
        }*/
    }
}
