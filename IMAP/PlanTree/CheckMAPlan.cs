using IMAP.Formulas;
using IMAP.General;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.PlanTree
{
    public class CheckMAPlan
    {
        public static bool IsValid2(Domain d, Problem p, Dictionary<Constant, PlanResult> plans)
        {
            bool valid = true;
            var AllInitialStates = p.GetInitialStates();
            // for each possible initial state - 
            foreach (var initialState in AllInitialStates)
            {
                bool isPlanValidForInitialSetting = CheckPlansOnSingleInitialState(initialState, plans, p.GetGoals());
                // if plans are not valid for any branch, set to fail.
                if (isPlanValidForInitialSetting == false)
                {
                    valid = false;
                }
            }
            return valid;
        }

        private static bool CheckPlansOnSingleInitialState(PartiallySpecifiedState initialState, Dictionary<Constant, PlanResult> plans, List<Predicate> goals)
        {
            List<Action> actionsList = GenerateJointActionsListForSingleInitialState(initialState, plans);
            List<Predicate> collectedPredicate = new List<Predicate>();
            PartiallySpecifiedState currentState = initialState.Clone();
            foreach (var action in actionsList)
            {
                currentState.ApplyOffline(action, out Formula obs, out PartiallySpecifiedState psTrue, out PartiallySpecifiedState psFalse);
                currentState = psTrue;
            }
            if (currentState.IsGoalState())
                return true;
            else
                return false;
        }

        private static List<Action> GenerateJointActionsListForSingleInitialState(PartiallySpecifiedState initialState, Dictionary<Constant, PlanResult> plans)
        {
            foreach (var agentPlan in plans)
            {
                Constant agent = agentPlan.Key;
                PlanResult planResult = agentPlan.Value;
                ConditionalPlanTreeNode cptn = planResult.Plan;
                List<Action> actions = new List<Action>();
                cptn.GetActionUsed(ref actions);
                return actions;
            }
            return null;
        }

        // 
        public static bool IsValid(Dictionary<Constant, PlanResult> plans)
        {
            List<PreconditionsAndEffectsAtTime> peatCollection = new List<PreconditionsAndEffectsAtTime>();
            int latestTimeOperationObserved = 0;
            // Collect all the preconditions and effects from all the agent's plans at each timestamp
            foreach (var plan in plans)
            {
                Constant agent = plan.Key;
                ConditionalPlanTreeNode cptn = plan.Value.Plan;
                // get all actions
                List<Action> actionsUsed = new List<Action>();
                cptn.GetActionUsed(ref actionsUsed);

                foreach (var a in actionsUsed)
                {
                    int actionTime = a.GetTime();
                    Formula effects = a.Effects;
                    Formula preconditions = a.Preconditions;

                    PreconditionsAndEffectsAtTime peat = new PreconditionsAndEffectsAtTime();
                    peat.agent = agent;
                    peat.Time = actionTime;
                    peat.Effects = effects;
                    peat.Preconditions = preconditions;
                    peatCollection.Add(peat);
                    //Update latest operation time
                    latestTimeOperationObserved = Math.Max(latestTimeOperationObserved, actionTime);
                }
            }

            // Create aggregated collection of effects over time
            Dictionary<int, Formula> effectsCollection = new Dictionary<int, Formula>();
            Dictionary<int, Formula> preconditionsCollection = new Dictionary<int, Formula>();
            for (int i = 0; i <= latestTimeOperationObserved; i++)
            {
                // Get all effects at time i
                CompoundFormula combinedEffects = new CompoundFormula("and");
                foreach (var effFormula in peatCollection.FindAll(x => x.Time == i).Select(x => x.Effects).ToList())
                    combinedEffects.AddOperand(effFormula);
                effectsCollection.Add(i, combinedEffects);
                // Get all the preconditions at time i
                CompoundFormula combinedPreconditions = new CompoundFormula("and");
                foreach (var effFormula in peatCollection.FindAll(x => x.Time == i).Select(x => x.Preconditions).ToList())
                    combinedPreconditions.AddOperand(effFormula);
                preconditionsCollection.Add(i, combinedPreconditions);
            }



            return true;
        }

        private class PreconditionsAndEffectsAtTime
        {
            public Constant agent { get; set; }
            public int Time { get; set; }
            public Formula Preconditions { get; set; }
            public Formula Effects { get; set; }
        }
    }
}
