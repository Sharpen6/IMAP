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

        private List<Constant> NeedsToPlan;

        public AgentSelector()
        {
        }

        public AgentSelector(List<Constant> agents, List<Predicate> goals)
        {
            this.agents = agents;
            this.goals = goals;

            NeedsToPlan = new List<Constant>(agents);
        }

        public bool Finished()
        {
            return NeedsToPlan.Count == 0;
        }

        /// <summary>
        /// When first called, the first agent is returned
        /// </summary>
        /// <returns></returns>
        public Constant GetNextAgent()
        {
            if (NeedsToPlan.Count > 0)
            {
                Constant agent = NeedsToPlan[0];
                NeedsToPlan.RemoveAt(0);
                return agent;
            }
            return null;
        }

        public List<KeyValuePair<Predicate, int>> GetPrevGoalsCompletionTime(Constant agent, Problem problem)
        {
            List<KeyValuePair<Predicate, int>> alreadyCompleted = new List<KeyValuePair<Predicate, int>>();
            List <Predicate> goals = problem.GetGoals();

            foreach (var goal in goals)
            {
                GoalCompletionAtIteration history = GoalsCompletionTime.FindLast(x => x.Goal == goal && x.Agent != agent);

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
            List<CollaborationRequest> crs = CollborationRequests.FindAll(x => x.Receiver.Equals(agent));
            if (crs == null) return null;
            List<Tuple<Action, Constant>> res = new List<Tuple<Action, Constant>>();
            foreach (var colabReq in crs)
            {
                res.Add(new Tuple<Action, Constant>(colabReq.CollaborationRequired, colabReq.Sender));
            }
            return res;
        }

        public void AddGoalCompletionTime(int iteration, Constant agent, Predicate key, int value)
        {
            GoalCompletionAtIteration goalCompletion = new GoalCompletionAtIteration();
            goalCompletion.Iteration = iteration;
            goalCompletion.Agent = agent;
            goalCompletion.Goal = key;
            goalCompletion.MinTime = value;

            // If this goal overrides an existing completion by another agent, return to this agent at the next iteration..
            var overridenCompletion = GoalsCompletionTime.FindLast(x => x.Goal == key);
            if (overridenCompletion != null)
            {
                if (value < overridenCompletion.MinTime)
                {
                    // in this case, the agent improved the previous time.
                    Constant slowerAgent = overridenCompletion.Agent;

                    // slower agent got beaten by the current agent, let the slower agent to replan, this time. 
                    // hopefully he would let this goal go next time he replans..
                    NeedsToPlan.Insert(0, slowerAgent);
                }
                else
                {
                    if (overridenCompletion.Agent == agent)
                    {

                    }
                    else
                    {
                        // in this case, the agent used an action that gets the goal instead of using just the get goal action..
                        throw new Exception();
                    }
                }
            }
            GoalsCompletionTime.Add(goalCompletion);
        }

        private class GoalCompletionAtIteration
        {
            public int Iteration { get; set; }
            public Constant Agent { get; set; }
            public Predicate Goal { get; set; }
            public int MinTime { get; set; }

            public override string ToString()
            {
                return "i:" + Iteration + ", a= " + Agent + ", g= " + Goal + " at " + MinTime;
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