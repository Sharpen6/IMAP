﻿using System;
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
            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(Domain, Problem, 200, SDRPlanner.Planners.FF);

            // Get the first agent
            Constant agent = agentSelector.GetNextAgent();

            while (agent!=null)
            {
                PlanResult pr = saSDR.Plan(agent, null, null, null);
                if (pr.Valid)
                {

                    // Extract constraints for next agents
                    Dictionary<string, int> JointActionsTimes = pr.GetUsedJointActionsLastTiming(Domain);

                    // 1. 
                    var constraints = GetConstraintsForNextAgents(JointActionsTimes, agent);
                    // 2. 
                    PostpondPlanForByJointActionsTimes(JointActionsTimes, pr);

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

        private object GetConstraintsForNextAgents(Dictionary<string, int> jointActionsTimes, Constant agent)
        {
            throw new NotImplementedException();
        }

        private void PostpondPlanForByJointActionsTimes(Dictionary<string, int> jointActionsTimes, PlanResult pr)
        {
            throw new NotImplementedException();
        }
    }
}
