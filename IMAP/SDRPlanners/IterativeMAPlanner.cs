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
        public SDRPlanner.Planners m_planner { get; }

        private AgentSelector agentSelector;

        private Dictionary<Constant, PlanResult> m_AgentsPlans = new Dictionary<Constant, PlanResult>();

        public IterativeMAPlanner(Domain d, Problem p, SDRPlanner.Planners planner)
        {
            Domain = d;
            Problem = p;
            m_planner = planner;
            agentSelector = new AgentSelector(d.GetAgents(), p.GetGoals());

        }

        public Dictionary<Constant, PlanResult> Plan()
        {
            // Set iteration number = 0;
            int iteration = 0;
            // Initialize SA agent Planner
            SingleAgentSDRPlanner saSDR = new SingleAgentSDRPlanner(Domain, Problem, m_planner);

            while (!agentSelector.Finished())
            {
                Constant currAgent = agentSelector.GetNextAgent();

                // Inc iteration num
                iteration += 1;

                // Get constraints from previous iterations
                //   collab action, sender 
                List<Tuple<Action,Constant>> prevCollabConstraints = agentSelector.GetCollabConstraints(currAgent);

                // Get goals completion time from previous iterations
                List<KeyValuePair<Predicate, int>> prevGoalsCompletionTime = agentSelector.GetPrevGoalsCompletionTime(currAgent, Problem);
                // Plan for current agent
                var collabReqForCurrentAgent = prevCollabConstraints.Select(x => x.Item1).ToList();
                PlanResult pr = saSDR.Plan(currAgent, null, prevGoalsCompletionTime, collabReqForCurrentAgent);
                if (pr.Valid)
                {
                    // 1. Align tree using joint actions
                    //  1.1 Extract constraints (Collaborative actions)
                    //         Agent   , Actions required
                    List<Action> collabUsed = pr.GetConstraintsGeneratedForSelf();

                    //  1.2 Check if sender of  can commit to the collaborative actions
                    PlanResult pr_validation = saSDR.Plan(currAgent, null, prevGoalsCompletionTime, collabUsed);

                    if (pr_validation.Valid)
                    {
                        pr = pr_validation;
                    }
                    else
                    {
                        // TODO postpone joint actions until valid
                        throw new NotImplementedException();
                    }
                    //
                    Dictionary<Constant, List<Action>> constraints = pr.GetNewConstraintsGeneratedForOtherAgents(prevCollabConstraints);
                    // 4. Save collaborative actions' constraints for other agents
                    // for each target agent
                    // TODO inser this part into one function (maybe AddCollabConstraints?)
                    agentSelector.RemoveConstraintsFromSendBy(currAgent);
                    foreach (var agentConstraints in constraints)
                    {
                        // for each action that other target agent needs to complete.
                        foreach (var a in agentConstraints.Value)
                        {
                            // add this action to his tasks.
                            agentSelector.AddCollabConstraints(agentConstraints.Key, a, currAgent);
                        }    
                    }


                    // 5. Save goal completion time, but ignore achieved predicates forced from other agents - he didnt caused that!
                    var goalTiming = pr.GetGoalsCompletionTime(Problem, prevCollabConstraints.Select(x=>x.Item1).ToList());
                    foreach (var item in goalTiming)
                    {
                        Constant backtrackToAgent = null;
                        agentSelector.AddGoalCompletionTime(iteration, currAgent, item.Key, item.Value, out backtrackToAgent);
                        
                        // TODO - only backtrack to the earliest agent
                        // set backtrack if needed
                        if (backtrackToAgent != null)
                            agentSelector.SetNextAgent(backtrackToAgent);
                    }
                                     
                    // Save plan details
                    if (!m_AgentsPlans.ContainsKey(currAgent))
                        m_AgentsPlans.Add(currAgent, pr);
                    else
                        m_AgentsPlans[currAgent] = pr;
                }
                else
                {
                    // failed to plan (without alignment)
                    Console.WriteLine("Agent " + currAgent.Name + " failed to plan");
                }
            }
            
            return m_AgentsPlans;
        }
    }
}
