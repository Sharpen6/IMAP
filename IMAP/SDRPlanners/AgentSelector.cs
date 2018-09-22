using System;
using System.Collections.Generic;
using IMAP.Predicates;

namespace IMAP.SDRPlanners
{
    public class AgentSelector
    {
        private List<Constant> agents;
        private int currentlySelected = 0;

        private List<CollaborationRequest> CollborationRequests = new List<CollaborationRequest>();

        private class CollaborationRequest
        {
            public Constant sender { get; set; }
            public Constant Receiver { get; set; }
            public List<Action> CollaborationRequired { get; set; }
        }

        public AgentSelector()
        {
        }

        public AgentSelector(List<Constant> agents)
        {
            this.agents = agents;
        }

        /// <summary>
        /// When first called, the first agent is returned
        /// </summary>
        /// <returns></returns>
        public Constant GetNextAgent()
        {
            if (currentlySelected > agents.Count - 1)
                return null;

            Constant selectedAgent = agents[currentlySelected];
            currentlySelected += 1;
            return selectedAgent;
        }

        internal void AddCollabConstraints(Constant targetAgent, List<Action> collabActions, Constant senderAgent)
        {
            CollaborationRequest cr = new CollaborationRequest();
            cr.CollaborationRequired = collabActions;
            cr.sender = senderAgent;
            cr.Receiver = targetAgent;

            CollborationRequests.Add(cr);
        }

        internal List<Action> GetCollabConstraints(Constant agent)
        {
            CollaborationRequest cr = CollborationRequests.Find(x => x.Receiver.Equals(agent));
            if (cr == null) return null;
            return cr.CollaborationRequired;
        }
    }
}