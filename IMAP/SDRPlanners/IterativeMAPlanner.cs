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

        public Dictionary<Constant, PlanResult> Plan()
        {
            // Initialize SA agent Planner
            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(Domain, Problem, SDRPlanner.Planners.FF);

            // Get the first agent
            Constant agent = agentSelector.GetNextAgent();

            while (agent!=null)
            {
                // Get constraints from previous iterations
                List<Action> prevCollabConstraints = agentSelector.GetCollabConstraints(agent);

                // Plan for current agent
                PlanResult pr = saSDR.Plan(agent, null, null, prevCollabConstraints);
                if (pr.Valid)
                {
                    // 1. Align tree using joint actions
                    Dictionary<Action, int> JointActionsTimes = pr.GetUsedJointActionsLastTiming(pr.m_agentDomain);
                    // 2. Extract constraints
                    //         Agent   , Actions required
                    Dictionary<Constant, List<Action>> constraints = GetConstraintsForNextAgents(pr);

                    // 3. Check if sender of  can commit to the collaborative actions
                    PlanResult pr_validation = saSDR.Plan(agent, null, null, constraints[agent]);

                    if (pr_validation.Valid)
                    {
                        pr = pr_validation;
                    }
                    else
                    {
                        // postpone joint actions until valid
                        throw new NotImplementedException();
                    }

                    // 4. Save collab constraints for other agents
                    foreach (var agentConstraints in constraints)
                    { 
                        agentSelector.AddCollabConstraints(agentConstraints.Key, agentConstraints.Value, agent);
                    }

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
                    agent = agentSelector.GetNextAgent();
                }
            }
            
            return m_AgentsPlans;
        }

        public Dictionary<Constant, List<Action>> GetConstraintsForNextAgents(PlanResult pr)
        {
            // Extract constraints for next agents
            Dictionary<Action, int> JointActionsTimes = pr.GetUsedJointActionsLastTiming(pr.m_agentDomain);
            // 
            Constant agent = pr.m_planningAgent;

            Dictionary<Constant, List<Action>> collabActionsForAgents = new Dictionary<Constant, List<Action>>();

            foreach (var joinAction in JointActionsTimes)
            {
                // The secondary actor which have to do this action, the action
                Tuple<Constant, Action> actionForOther = pr.m_agentDomain.GetCorellativeActionForOtherAgents(joinAction, agent);
                Tuple<Constant, Action> actionForSelf = pr.m_agentDomain.GetCorellativeActionForOtherAgents(joinAction, agent);

                if (!collabActionsForAgents.ContainsKey(actionForOther.Item1))
                    collabActionsForAgents.Add(actionForOther.Item1, new List<Action>());
                collabActionsForAgents[actionForOther.Item1].Add(actionForOther.Item2);

                if (!collabActionsForAgents.ContainsKey(agent))
                    collabActionsForAgents.Add(agent, new List<Action>());
                collabActionsForAgents[agent].Add(joinAction.Key);
            }
            return collabActionsForAgents;
        }

        private void PostpondPlanByJointActionsTimes(PlanResult pr, Dictionary<Action, int> jointActionsTimes)
        {
            throw new NotImplementedException();
        }
    }
}
