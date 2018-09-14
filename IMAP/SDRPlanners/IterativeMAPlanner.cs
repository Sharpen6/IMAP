using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMAP.General;
using IMAP.Predicates;

namespace IMAP.SDRPlanners
{
    public class IterativeMAPlanner
    {
        public static int MAX_TIME = 200;

        public Domain Domain { get; set; }
        public Problem Problem { get; set; }

        private AgentSelector agentSelector;

        private Dictionary<Constant, PlanResult> m_AgentsPlans = new Dictionary<Constant, PlanResult>();

        public IterativeMAPlanner(Domain d, Problem p)
        {
            Domain = d;
            Problem = p;
            agentSelector = new AgentSelector(d.GetAgents());
        }

        public PlanResult Plan()
        {
            // Initialize SA agent Planner
            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(Domain, Problem, SDRPlanner.Planners.FF);

            // Get the first agent
            Constant agent = agentSelector.GetNextAgent();

            while (agent!=null)
            {
                PlanResult pr = saSDR.Plan(agent, null, null, null);
                if (pr.Valid)
                {
                    // 1. Align tree using joint actions
                    Dictionary<Action, int> JointActionsTimes = pr.GetUsedJointActionsLastTiming(pr.m_agentDomain);
                    // 2. Extract constraints
                    //         Agent   , Actions required
                    Dictionary<Constant, List<Action>> constraints = GetConstraintsForNextAgents(pr);
                    //PostpondPlanByJointActionsTimes(pr, JointActionsTimes);
                    // Save plan details
                    if (!m_AgentsPlans.ContainsKey(agent))
                        m_AgentsPlans.Add(agent, pr);
                    else
                        m_AgentsPlans[agent] = pr;


                    // Advance to the next agent
                    agent = agentSelector.GetNextAgent();
                }
                else
                {

                }
            }
            
            return null;
        }

        public Dictionary<Constant, List<Action>> GetConstraintsForNextAgents(PlanResult pr)
        {
            Dictionary<Constant, List<Action>> res = new Dictionary<Constant, List<Action>>();
            // Extract constraints for next agents
            Dictionary<Action, int> JointActionsTimes = pr.GetUsedJointActionsLastTiming(pr.m_agentDomain);
            // 
            Constant agent = pr.m_planningAgent;
            foreach (var joinAction in JointActionsTimes)
            {
                // The secondary actor which have to do this action, the action
                Tuple< Constant, Action > action = pr.m_agentDomain.GetCorellativeActionForOtherAgents(joinAction, agent);
                
                if (!res.ContainsKey(action.Item1))
                    res.Add(action.Item1, new List<Action>());
                res[action.Item1].Add(action.Item2);
            }
            return res;
        }

        private void PostpondPlanByJointActionsTimes(PlanResult pr, Dictionary<Action, int> jointActionsTimes)
        {
            throw new NotImplementedException();
        }
    }
}
