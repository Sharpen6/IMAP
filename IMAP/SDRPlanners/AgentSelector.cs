using System;
using System.Collections.Generic;
using IMAP.General;
using IMAP.Predicates;

namespace IMAP.SDRPlanners
{
    public class AgentSelector
    {
        private List<Constant> agents;
        private List<Predicate> goals;
        private int currentlySelected = 0;

        private List<GoalCompletionAtIteration> GoalsCompletionTime = new List<GoalCompletionAtIteration>();
        private List<CollaborationRequest> CollborationRequests = new List<CollaborationRequest>();

        public AgentSelector()
        {
        }

        public AgentSelector(List<Constant> agents, List<Predicate> goals)
        {
            this.agents = agents;
            this.goals = goals;
        }

        public bool Finished()
        {
            return agents.Count == currentlySelected;
        }

        /// <summary>
        /// Return the next agent that needs to be called 
        /// </summary>
        /// <returns></returns>
        public Constant GetNextAgent()
        {
            if (currentlySelected < agents.Count)
            {
                Constant agent = agents[currentlySelected];
                currentlySelected += 1;
                return agent;
            }

            // Index out of bounds
            return null; 
        }

        public void SetNextAgent(Constant agent)
        {
            int agentIndex = agents.FindIndex(x => x == agent);
            int currentlySelectedAgentIndex = currentlySelected - 1;
            // remove knowledge aquired by agents skipped back
            for (int i = currentlySelectedAgentIndex - 1; i >= agentIndex; i--)
            {
                Constant remAgent = agents[i];
                // remove goal completion time
                foreach (var goalComp in GoalsCompletionTime.FindAll(x => x.Agent == remAgent))
                {
                    goalComp.Invalid = true;
                }

                // TODO - what about constraints send by them?
            }

            currentlySelected = agentIndex;
        }

        public List<KeyValuePair<Predicate, int>> GetPrevGoalsCompletionTime(Constant agent, Problem problem)
        {
            List<KeyValuePair<Predicate, int>> alreadyCompleted = new List<KeyValuePair<Predicate, int>>();
            List <Predicate> goals = problem.GetGoals();

            foreach (var goal in goals)
            {
                GoalCompletionAtIteration history = GoalsCompletionTime.FindLast(x => x.Goal == goal && x.Agent != agent && !x.Invalid);

                if (history != null)
                {
                    alreadyCompleted.Add(new KeyValuePair<Predicate, int>(history.Goal, history.MinTime));
                }
            }
            return alreadyCompleted;
            
        }

        internal void AddCollabConstraints(Constant targetAgent, Action collabAction, Constant senderAgent)
        {
            CollaborationRequest cr = new CollaborationRequest();
            cr.CollaborationRequired = collabAction;
            cr.Sender = senderAgent;
            cr.Receiver = targetAgent;
           
            CollborationRequests.Add(cr);
        }

        public List<Tuple<Action,Constant>> GetCollabConstraints(Constant agent)
        {
            List<CollaborationRequest> crs = CollborationRequests.FindAll(x => x.Receiver.Name == agent.Name && x.Sender.Name != agent.Name);
            if (crs == null) return null;
            List<Tuple<Action, Constant>> res = new List<Tuple<Action, Constant>>();
            foreach (var colabReq in crs)
            {
                res.Add(new Tuple<Action, Constant>(colabReq.CollaborationRequired, colabReq.Sender));
            }
            return res;
        }

        internal void RemoveConstraintsFromSendBy(Constant currAgent)
        {
            CollborationRequests.RemoveAll(x => x.Sender == currAgent);
        }

        public void AddGoalCompletionTime(int iteration, Constant agent, Predicate key, int value, out Constant backtrackToAgent)
        {
            GoalCompletionAtIteration goalCompletion = new GoalCompletionAtIteration();
            goalCompletion.Iteration = iteration;
            goalCompletion.Agent = agent;
            goalCompletion.Goal = key;
            goalCompletion.MinTime = value;

            // 
            backtrackToAgent = null;
            // If this goal overrides an existing completion by another agent, return to this agent at the next iteration..
            var overridenCompletion = GoalsCompletionTime.FindLast(x => x.Goal == key && !x.Invalid);
            if (overridenCompletion != null)
            {
                if (value < overridenCompletion.MinTime)
                {
                    // in this case, the agent improved the previous time.
                    Constant slowerAgent = overridenCompletion.Agent;

                    // Slower agent got beaten by the current agent, let the slower agent to replan again.
                    // This time, hopefully he would let this goal go next time he replans..
                    backtrackToAgent = slowerAgent;
                    GoalsCompletionTime.Add(goalCompletion);
                }
            }
            else
            {
                GoalsCompletionTime.Add(goalCompletion);
            }
            
        }

        private class GoalCompletionAtIteration
        {
            public int Iteration { get; set; }
            public Constant Agent { get; set; }
            public Predicate Goal { get; set; }
            public int MinTime { get; set; }
            public bool Invalid { get; set; }

            public override string ToString()
            {
                return "i:" + Iteration + ", a= " + Agent + ", g= " + Goal + " at " + MinTime + ", Invalid? " + Invalid;
            }
        }

        private class CollaborationRequest
        {
            public Constant Sender { get; set; }
            public Constant Receiver { get; set; }
            public Action CollaborationRequired { get; set; }
        }

    }
}