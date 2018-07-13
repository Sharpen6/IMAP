using IMAP.General;
using IMAP.Predicates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.PlanTree
{
    public class DomainExtensiveInfo
    {
        public Domain m_dDomain { get; private set; }
        public Problem m_dProblem { get; private set; }
        public Dictionary<IMAP.Action, int> AgentsReqForAction { get; set; }
        public string ProblemDesc { get; internal set; }
        public DomainExtensiveInfo(Domain domain, Problem problem)
        {
            m_dDomain = domain;
            m_dProblem = problem;


            ScanActions();
        }

        private void ScanActions()
        {
            AgentsReqForAction = new Dictionary<IMAP.Action, int>();
            foreach (var action in m_dDomain.Actions)
            {
                int agentsNum = 0;
                if (action.Preconditions != null)
                {
                    int preconditionsNumAgents = action.Preconditions.CountAgents(m_dDomain.AgentCallsign);
                    agentsNum = Math.Max(agentsNum, preconditionsNumAgents);
                }
                if (action.Effects != null)
                {
                    int effectNumAgents = action.Effects.CountAgents(m_dDomain.AgentCallsign);
                    agentsNum = Math.Max(agentsNum, effectNumAgents);
                }

                AgentsReqForAction.Add(action, agentsNum);
            }
        }

        internal List<string> GetSingleActions()
        {
            List<string> selectedActions = new List<string>();
            foreach (var item in AgentsReqForAction)
            {
                if (item.Value == 1)
                    selectedActions.Add(item.Key.Name);
            }
            return selectedActions;
        }

        internal List<string> GetDifficulty()
        {
            List<string> waypoints = new List<string>();
            foreach (var item in m_dDomain.Constants)
            {
                if (item.Name.StartsWith("waypoint"))
                    waypoints.Add(item.Name);
            }
            return waypoints;
        }

        public List<Constant> GetAgents()
        {
            return m_dDomain.GetAgents();
        }

        internal List<string> GetJointActions()
        {
            List<string> selectedActions = new List<string>();
            foreach (var item in AgentsReqForAction)
            {
                if (item.Value > 1)
                    selectedActions.Add(item.Key.Name);
            }
            return selectedActions;
        }

        internal int GetActionNumberOfAgents(string p)
        {
            foreach (var item in AgentsReqForAction)
            {
                if (item.Key.Name.StartsWith(p))
                {
                    return item.Value;
                }
            }
            return 0;
        }

        internal List<Predicate> GetGoals()
        {
            return m_dProblem.GetGoals();
        }

        internal List<Predicate> GetCollabGoals()
        {
            List<Predicate> ans = new List<Predicate>();
            foreach (var goal in GetGoals())
            {
                bool isCollab = false;
                //if (goal.ToString().Contains("box-at b1 p2-2"))
                //if (goal.ToString().Contains("box-at b1 p2-2") || goal.ToString().Contains("box-at b2 p3-2") || goal.ToString().Contains("box-at b3 p4-2"))
                //if (goal.ToString().Contains("communicated_rock_data"))
                //    isCollab = true;


                if (isCollab)
                    ans.Add(goal);


            }
            return ans;
        }
    }
}
