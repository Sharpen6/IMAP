﻿using IMAP.PlanTree;
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

        private Domain m_generalDomain;
        private Problem m_generalProblem;

        private List<KeyValuePair<Predicate, int>> goalsCompletionTime;
        private List<Action> reqActions;

        private static Dictionary<string, string> CorrelativeActions = new Dictionary<string, string>();

        public PlanResult(Constant agent, ConditionalPlanTreeNode plan, TimeSpan planningTime, bool valid,
            List<KeyValuePair<Predicate, int>> goalsCompletionTime, List<Action> reqActions, Domain d, Problem p,
            Domain general_d, Problem general_p)
        {
            m_planningAgent = agent;
            Plan = plan;
            PlanningTime = planningTime;
            Valid = valid;
            m_agentDomain = d;
            m_agentProblem = p;
            m_generalDomain = general_d;
            m_generalProblem = general_p;
            // The plan generated under the following variables..
            this.goalsCompletionTime = goalsCompletionTime;
            this.reqActions = reqActions;
        }

        public Dictionary<Predicate, int> GetGoalsCompletionTime(Problem problem, List<Action> prevCollabConstraints)
        {
            if (Plan == null)
                return null;
            List<Predicate> goals = problem.GetGoals();
            Dictionary<Predicate, int> goalTiming = new Dictionary<Predicate, int>();
            //List<Action> jointActions = reqActions;
            List<Action> jointActions = new List<Action>();
            Plan.GetGoalsTiming(goals, jointActions, ref goalTiming);

            return goalTiming;
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

        public Dictionary<Constant, List<Action>> GetNewConstraintsGeneratedForOtherAgents(List<Tuple<Action, Constant>> prevCollabConstraints)
        {
            Dictionary<Constant, List<Action>> filteredConstraints = new Dictionary<Constant, List<Action>>();
            Dictionary<Constant, List<Action>> allConstraints = GetConstraintsForNextAgents(prevCollabConstraints);
            foreach (var constraint in allConstraints)
            {
                // Skip constraints from current agent
                if (constraint.Key != m_planningAgent)
                {
                    filteredConstraints.Add(constraint.Key, new List<Action>());

                    List<Action> constraintsForAgent = constraint.Value;
                    foreach (var consAction in constraintsForAgent)
                    {
                        if (CorrelativeActions.ContainsKey(consAction.Name))
                        {
                            // If action found - it means that it got generated by other agent, dont return it back.
                            string corrAction = CorrelativeActions[consAction.Name];
                            if (prevCollabConstraints.Count(x=>x.Item1.Name == corrAction) == 0)
                            {
                                filteredConstraints[constraint.Key].Add(consAction);
                            }
                        }
                        else
                        {
                            // action not found - it is a new joint action - send it forward..

                            filteredConstraints[constraint.Key].Add(consAction);
                        }  
                    }
                }
            }
            return filteredConstraints;
        }

        public List<Action> GetConstraintsGeneratedForSelf()
        {
            // TODO : validate this method.
            Dictionary<Constant, List<Action>> agentsActions = GetConstraintsForNextAgents();
            // Return: Constraints:
            // 1. Requested by previous agents from this agent
            // 2. New constraints generated from joint actions used by this agent.         
            if (agentsActions.ContainsKey(m_planningAgent))
                return agentsActions[m_planningAgent];
            else
                return new List<Action>();
        }

        public Dictionary<Constant, List<Action>> GetConstraintsForNextAgents(List<Tuple<Action, Constant>> prevCollabConstraints = null)
        {
            if (prevCollabConstraints == null)
                prevCollabConstraints = new List<Tuple<Action, Constant>>();

            // Extract constraints for next agents
            Dictionary<Action, int> JointActionsTimes = GetUsedJointActionsLastTiming(m_agentDomain);
            // 
            Constant agent = m_planningAgent;

            Dictionary<Constant, List<Action>> collabActionsForAgents = new Dictionary<Constant, List<Action>>();
           
            foreach (var jointAction in JointActionsTimes)
            {
                // if this joint action was sent by another agent, dont forward it at all.
                if (prevCollabConstraints.Count(x=>x.Item1.Name == jointAction.Key.Name) > 0)
                {
                    continue;
                }

                if (!collabActionsForAgents.ContainsKey(agent))
                    collabActionsForAgents.Add(agent, new List<Action>());

                collabActionsForAgents[agent].Add(jointAction.Key.Clone());

                Action jointActionOtherAgent = jointAction.Key.Clone();
                string otherAgentName = jointAction.Key.Preconditions.GetAgents(m_agentDomain.AgentCallsign).Where(x=>x!=agent.Name).First();
                Constant otherAgentObject = m_agentDomain.GetAgents().Where(x => x.Name == otherAgentName).First();
                jointActionOtherAgent.ChangeAgent(agent, otherAgentObject);


                if (!collabActionsForAgents.ContainsKey(otherAgentObject))
                    collabActionsForAgents.Add(otherAgentObject, new List<Action>());
                collabActionsForAgents[otherAgentObject].Add(jointActionOtherAgent);

                /*

                // The secondary actor which have to do this action, the action
                Tuple<Constant, Action> actionForOther = m_agentDomain.GetCorellativeActionForOtherAgents(jointAction, agent, m_generalDomain);

                // Save to static db of correlative actions.
                if (!CorrelativeActions.ContainsKey(jointAction.Key.Name))
                    CorrelativeActions.Add(jointAction.Key.Name, actionForOther.Item2.Name);
                if (!CorrelativeActions.ContainsKey(actionForOther.Item2.Name))
                    CorrelativeActions.Add(actionForOther.Item2.Name, jointAction.Key.Name);

                if (!collabActionsForAgents.ContainsKey(actionForOther.Item1))
                    collabActionsForAgents.Add(actionForOther.Item1, new List<Action>());
                collabActionsForAgents[actionForOther.Item1].Add(actionForOther.Item2);

                if (!collabActionsForAgents.ContainsKey(agent))
                    collabActionsForAgents.Add(agent, new List<Action>());
                collabActionsForAgents[agent].Add(jointAction.Key);*/
            }
            return collabActionsForAgents;
        }
    }
}
