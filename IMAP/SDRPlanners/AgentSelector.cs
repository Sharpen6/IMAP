using System;
using System.Collections.Generic;
using IMAP.Predicates;

namespace IMAP.SDRPlanners
{
    public class AgentSelector
    {
        private List<Constant> agents;
        private int currentlySelected = 0;

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

            return agents[currentlySelected ++ ];
        }
    }
}