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
    class SingleAgentSDRPlanner
    {
        //Parameters
        private Constant m_activeAgent;
        private List<Predicate> m_activeGoals;
        private Domain m_Domain;
        private Problem m_Problem;
        private int m_maxTime;
        private List<Action> m_ReqCollabActions;
        private List<KeyValuePair<Predicate, int>> m_prevAchievedGoals;
        private SDRPlanner.Planners m_planner;
        SDRPlanner m_sdrPlanner;
        // Generated
        public TimeSpan PlanningTime { get; private set; }
        public ConditionalPlanTreeNode Plan { get; private set; }
        public Boolean Valid { get; private set; }
        public Dictionary<Action, Action> OriginalActionsMapping { get; private set; }
        public SingleAgentSDRPlanner(Constant activeAgent, List<Predicate> activeGoals, 
            List<KeyValuePair<Predicate, int>> prevAchievedGoals, List<Action> reqActions,
            Domain m_Domain, Problem m_Problem, int maxTime, SDRPlanner.Planners planner)
        {
            this.m_activeAgent = activeAgent;
            this.m_activeGoals = activeGoals;
            this.m_prevAchievedGoals = prevAchievedGoals;
            this.m_Domain = m_Domain;
            this.m_Problem = m_Problem;
            this.m_maxTime = maxTime;
            this.m_planner = planner;
            this.m_ReqCollabActions = reqActions;

            OriginalActionsMapping = new Dictionary<Action, Action>();

            m_sdrPlanner = new SDRPlanner(m_Domain, m_Problem);
            SDRPlanner.Planner = m_planner;
        }
        public void Run()
        {
            DateTime start = DateTime.Now;
            AddNoopAction();
            AddTimeConstraints();
            AddCollabActionReq();
            ReduceToSingleAgent();
            AddPrevCompletionOfGoals();
            SetGoals();
            AddReasoningActions();
            AddCosts();

            Plan = m_sdrPlanner.OfflinePlanning();
            Valid = m_sdrPlanner.Valid;
            PlanningTime = DateTime.Now - start;
        }
        private void ReduceToSingleAgent()
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
            activeAgentPredicate.AddConstant(new Constant(m_Domain.AgentCallsign, m_activeAgent.Name));
            m_Problem.AddKnown(activeAgentPredicate);


            ParameterizedPredicate activeAgentParamPredicate = new ParameterizedPredicate("active-agent");
            Parameter pIsAgent = new Parameter(m_Domain.AgentCallsign, "?a");
            activeAgentParamPredicate.AddParameter(pIsAgent);
            m_Domain.AddPredicate(activeAgentParamPredicate);
            
            ParameterizedPredicate activeAgentParamPredicateJoint = new ParameterizedPredicate("active-agent");
            Parameter pIsAgentJoint = new Parameter(m_Domain.AgentCallsign, "?a1");
            activeAgentParamPredicateJoint.AddParameter(pIsAgentJoint);


            foreach (var action in m_Domain.Actions)
            {
                if (action.Preconditions.ContainsParameter(pIsAgent) || action.Preconditions.ContainsParameter(pIsAgentJoint))
                {
                    if (action.Preconditions.CountAgents(m_Domain.AgentCallsign) > 1)
                    {
                        Action originalAction = action.Clone();
                        // Joint Action
                        ParameterizedPredicate paramPredicateAgentAt = new ParameterizedPredicate("agent-at");
                        paramPredicateAgentAt.AddParameter(new Parameter(m_Domain.AgentCallsign, "?a2"));
                        paramPredicateAgentAt.AddParameter(new Parameter("pos", "?start"));

                        action.Preconditions.RemovePredicate(paramPredicateAgentAt);
                        action.Preconditions.AddPredicate(activeAgentParamPredicateJoint);

                        OriginalActionsMapping.Add(action, originalAction);
                    }
                    else
                    {
                        CompoundFormula newcf = new CompoundFormula("and");
                        newcf.SimpleAddOperand(action.Preconditions);
                        newcf.SimpleAddOperand(activeAgentParamPredicate);
                        action.Preconditions = newcf;
                    }
                }
            }
        }
        private void AddNoopAction()
        {
            m_Domain.AddNoopAction();
        }
        private void AddTimeConstraints()
        {
            m_Domain.AddTime(m_maxTime);
            m_Problem.AddTime(m_maxTime);
        }
        private void AddPrevCompletionOfGoals()
        {
            foreach (var completion in m_prevAchievedGoals)
            {
                m_Domain.AddGoalCompletion(m_Problem, completion.Key, completion.Value);

            }
        }
        private void AddCollabActionReq()
        {
            if (m_ReqCollabActions.Count > 0)
            {
                m_Domain.AddPredicate("sub-goal", "?g", "aGoal");
            }

            int artGoals = 0;
            foreach (var reqAction in m_ReqCollabActions)
            {
                string predicateName = "sub-goal" + (artGoals++);
                GroundedPredicate gp = new GroundedPredicate("sub-goal");
                gp.AddConstant(new Constant("aGoal", predicateName));

                m_Domain.AddConstant("aGoal", predicateName);

                // Update this goal to the domain and problem;
                m_activeGoals.Add(gp);

                m_Problem.AddKnown(gp.Negate());

                // This action now achieves this goal
                reqAction.AddEffect(gp);

                m_Domain.AddAction(reqAction);



                Action counterAction = new Action("art-" + reqAction.Name);

                Formula cgf = reqAction.Preconditions.GetUnknownPredicates(m_Domain.m_lObservable);

                counterAction.Preconditions = cgf.Negate(true);
                counterAction.AddEffect(gp);

                m_Domain.AddAction(counterAction);
            }
        }

        private void AddReasoningActions()
        {
            m_Domain.AddReasoningActions(m_Problem);
        }

        private void AddCosts()
        {
            if (SDRPlanner.AddActionCosts)
            {
                foreach (var a in m_Domain.Actions)
                {
                    a.Cost = CostGenerator(a);
                }

                m_Domain.Types.Add("cost");
                m_Domain.Constants.Add(new Constant("cost", "total-cost"));

                m_Problem.AddMetric("(:metric minimize (total-cost))");
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
            List<Predicate> currentGoals = new List<Predicate>();
            foreach (var goal in m_activeGoals)
            {
                currentGoals.Add(goal);
            }
            m_Problem.SetGoals(currentGoals);
        }
    }
}
