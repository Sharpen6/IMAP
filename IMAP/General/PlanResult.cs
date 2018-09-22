using IMAP.PlanTree;
using IMAP.Predicates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.General
{
    public class PlanResult
    {
        public Constant m_planningAgent { get; set; }
        public ConditionalPlanTreeNode Plan { get; set; }
        public TimeSpan PlanningTime { get; set; }
        public bool Valid { get; set; }
        public Domain m_agentDomain { get; set; }
        public Problem m_agentProblem { get; set; }

        public PlanResult(Constant agent, ConditionalPlanTreeNode plan, TimeSpan planningTime, bool valid, Domain d, Problem p)
        {
            m_planningAgent = agent;
            Plan = plan;
            PlanningTime = planningTime;
            Valid = valid;
            m_agentDomain = d;
            m_agentProblem = p;
        }

        public List<KeyValuePair<Predicate, int>> GetGoalsCompletionTime()
        {
            if (Plan == null)
                return null;
            List<Predicate> goals = m_agentProblem.GetGoals();
            Dictionary<Predicate, int> goalTimeing = new Dictionary<Predicate, int>();
            Plan.GetGoalsTiming(goals, ref goalTimeing);

            return null;
        }
        public Dictionary<Action, int> GetUsedJointActionsLastTiming(Domain d)
        {
            // get contraints from plan
            if (Plan == null)
                return null;

            List<Action> UsedActions = new List<Action>();
            Plan.GetActionUsed(ref UsedActions);

            // Get only the joint actions that should be merged
            List<Action> UsedJointActions = new List<Action>();
            foreach (Action a in UsedActions)
            {
                if (a.Preconditions.CountAgents(d.AgentCallsign) > 1)
                {
                    UsedJointActions.Add(a);
                }
            }
            Dictionary<Action /*type of the action without time*/, Action /*type of the action with time*/ > actionsTimesMapping = new Dictionary<Action, Action>();
            foreach (Action a in UsedJointActions)
            {
                // The list already contains the action

                Action aWithoutTime = a.RemoveTime();
                if (actionsTimesMapping.Count(x=>x.Key.Name == aWithoutTime.Name) > 0)
                {
                    var aWithTimeAlreadyExists = actionsTimesMapping.Where(x => x.Key.Name == aWithoutTime.Name).First();

                    if (aWithTimeAlreadyExists.Value.GetTime() < a.GetTime())
                    {
                        actionsTimesMapping[aWithoutTime] = a;
                    }
                }
                else
                {
                    actionsTimesMapping.Add(a.RemoveTime(), a);
                }
            }

            Dictionary<Action /*type of the action without time*/, int /*time*/ > actionsTime = new Dictionary<Action, int>();
            foreach (var action in actionsTimesMapping)
            {
                actionsTime.Add(action.Value, action.Value.GetTime());
            }

   
            return actionsTime;
        }
    }
}
