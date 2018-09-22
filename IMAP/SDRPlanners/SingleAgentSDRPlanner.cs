using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMAP.Formulas;
using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;

namespace IMAP.SDRPlanners
{
    public class SingleAgentSDRPlanner
    {
        //Parameters
        private Constant m_ActiveAgent;
        private List<Predicate> m_ActiveGoals;

        private Domain m_GeneralDomain;
        private Problem m_GeneralProblem;

        private Domain m_AgentDomain;
        private Problem m_AgentProblem;

        private int m_maxTime;
        private List<Action> m_ReqCollabActions;
        private List<KeyValuePair<Predicate, int>> m_GoalsCompletionTime;
        private SDRPlanner.Planners m_planner;

        public SingleAgentSDRPlanner(Domain m_Domain, Problem m_Problem, SDRPlanner.Planners planner)
        { 
            this.m_GeneralDomain = m_Domain;
            this.m_GeneralProblem = m_Problem;
            this.m_maxTime = IterativeMAPlanner.MAX_TIME;
            this.m_planner = planner;
        }
        public PlanResult Plan(Constant activeAgent, List<Predicate> activeGoals, List<KeyValuePair<Predicate, int>> goalsCompletionTime, List<Action> reqActions)
        {
            m_AgentDomain = Parser.ParseDomain(m_GeneralDomain.FilePath, m_GeneralDomain.AgentCallsign);
            m_AgentProblem = Parser.ParseProblem(m_GeneralProblem.FilePath, m_AgentDomain);

            //m_AgentDomain = new Domain(m_GeneralDomain.Path);
            //m_AgentProblem = new Problem(m_GeneralProblem, m_AgentDomain);

            m_ActiveAgent = activeAgent;
            m_ActiveGoals = m_AgentProblem.GetGoals();
            m_GoalsCompletionTime = goalsCompletionTime;
            m_ReqCollabActions = reqActions;

            DateTime start = DateTime.Now;

            AddNoopAction();
            AddTimeConstraints();
            AddCollabActionReq();
            ConvertToSingleAgentProblem();
            AddPrevCompletionOfGoals();
            SetGoals();
            AddReasoningActions();
            AddCosts();

            SDRPlanner sdrPlanner = new SDRPlanner(m_AgentDomain, m_AgentProblem, m_planner);
            string s1 = m_AgentDomain.ToString();
            string s2 = m_AgentProblem.ToString();
            ConditionalPlanTreeNode Plan = sdrPlanner.OfflinePlanning();
            string s = m_AgentDomain.ToString();
            bool Valid = sdrPlanner.Valid;

            TimeSpan PlanningTime = DateTime.Now - start;

            PlanResult result = new PlanResult(activeAgent, Plan, PlanningTime, Valid, m_AgentDomain, m_AgentProblem);

            return result;
        }
        private void ConvertToSingleAgentProblem()
        {
            /*foreach (var otherAgent in agents)
            {
                if (otherAgent.Name != agent.Name)
                {
                    RemoveAgentFromDomain(domain, otherAgent);
                    //problem.RemoveConstant(otherAgent);                   
                }
            }*/

            // Currently A1 plays
            GroundedPredicate activeAgentPredicate = new GroundedPredicate("active-agent");
            activeAgentPredicate.AddConstant(new Constant(m_AgentDomain.AgentCallsign, m_ActiveAgent.Name));
            m_AgentProblem.AddKnown(activeAgentPredicate);


            ParameterizedPredicate activeAgentParamPredicate = new ParameterizedPredicate("active-agent");
            Parameter pIsAgent = new Parameter(m_AgentDomain.AgentCallsign, "?a");
            activeAgentParamPredicate.AddParameter(pIsAgent);
            m_AgentDomain.AddPredicate(activeAgentParamPredicate);
            
            ParameterizedPredicate activeAgentParamPredicateJoint = new ParameterizedPredicate("active-agent");
            Parameter pIsAgentJoint = new Parameter(m_AgentDomain.AgentCallsign, "?a1");
            activeAgentParamPredicateJoint.AddParameter(pIsAgentJoint);


            foreach (var action in m_AgentDomain.Actions)
            {
                Action originalAction = action.Clone();
                if (action.Preconditions.ContainsParameter(pIsAgent) || action.Preconditions.ContainsParameter(pIsAgentJoint))
                {
                    if (action.Preconditions.CountAgents(m_AgentDomain.AgentCallsign) > 1)
                    {
                        // Joint Action
                        ParameterizedPredicate paramPredicateAgentAt = new ParameterizedPredicate("agent-at");
                        paramPredicateAgentAt.AddParameter(new Parameter(m_AgentDomain.AgentCallsign, "?a2"));
                        paramPredicateAgentAt.AddParameter(new Parameter("pos", "?start"));

                        action.Preconditions.RemovePredicate(paramPredicateAgentAt);
                        action.Preconditions.AddPredicate(activeAgentParamPredicateJoint);
                    }
                    else
                    {
                        CompoundFormula newcf = new CompoundFormula("and");
                        newcf.SimpleAddOperand(action.Preconditions);
                        newcf.SimpleAddOperand(activeAgentParamPredicate);
                        action.Preconditions = newcf;
                    }
                }
                action.OriginalActionBeforeRemovingAgent = originalAction;
            }
        }
        private void AddNoopAction()
        {
            m_AgentDomain.AddNoopAction();
        }
        private void AddTimeConstraints()
        {
            m_AgentDomain.AddTime(m_maxTime);
            m_AgentProblem.AddTime(m_maxTime);
        }
        private void AddPrevCompletionOfGoals()
        {
            if (m_GoalsCompletionTime != null)
            {
                foreach (var completion in m_GoalsCompletionTime)
                {
                    m_AgentDomain.AddGoalCompletion(m_AgentProblem, completion.Key, completion.Value);

                }
            }
        }
        private void AddCollabActionReq()
        {
            if (m_ReqCollabActions != null)
            {
                if (m_ReqCollabActions.Count > 0)
                {
                    m_AgentDomain.AddPredicate("sub-goal", "?g", "aGoal");
                }


                int artGoals = 0;
                foreach (var reqAction in m_ReqCollabActions)
                {
                    string predicateName = "sub-goal" + (artGoals++);
                    GroundedPredicate gp = new GroundedPredicate("sub-goal");
                    gp.AddConstant(new Constant("aGoal", predicateName));

                    m_AgentDomain.AddConstant("aGoal", predicateName);

                    // Update this goal to the domain and problem;
                    m_ActiveGoals.Add(gp);

                    m_AgentProblem.AddKnown(gp.Negate());

                    // This action now achieves this goal
                    reqAction.AddEffect(gp);

                    m_AgentDomain.AddAction(reqAction);



                    Action counterAction = new Action("art-" + reqAction.Name);

                    Formula cgf = reqAction.Preconditions.GetUnknownPredicates(m_AgentDomain.m_lObservable);

                    counterAction.Preconditions = cgf.Negate(true);
                    counterAction.AddEffect(gp);

                    m_AgentDomain.AddAction(counterAction);
                }
            }
        }
        private void AddReasoningActions()
        {
            m_AgentDomain.AddReasoningActions(m_AgentProblem);
        }
        private void AddCosts()
        {
            if (SDRPlanner.AddActionCosts)
            {
                foreach (var a in m_AgentDomain.Actions)
                {
                    a.Cost = CostGenerator(a);
                }

                m_AgentDomain.Types.Add("cost");
                m_AgentDomain.Constants.Add(new Constant("cost", "total-cost"));

                m_AgentProblem.AddMetric("(:metric minimize (total-cost))");
            }
        }
        private int CostGenerator(Action a)
        {

            if (SDRPlanner.CostGenerator == null)
            {
                // must initialize cost generator first, if add action costs is TRUE.
                throw new Exception();
            }
            
            int cost = SDRPlanner.CostGenerator.GetCost(a);
            return cost;
        }
        private void SetGoals()
        {
            if (m_ActiveGoals != null)
            {
                List<Predicate> currentGoals = new List<Predicate>();
                foreach (var goal in m_ActiveGoals)
                {
                    currentGoals.Add(goal);
                }
                m_AgentProblem.SetGoals(currentGoals);
            }
        }
    }
}
