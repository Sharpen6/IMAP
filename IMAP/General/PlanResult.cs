﻿using IMAP.PlanTree;
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
        public ConditionalPlanTreeNode Plan { get; set; }
        public TimeSpan PlanningTime { get; set; }
        public bool Valid { get; set; }
        public PlanResult(ConditionalPlanTreeNode plan, TimeSpan planningTime, bool valid)
        {
            Plan = plan;
            PlanningTime = planningTime;
            Valid = valid;
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


            Dictionary<Action /*type of the action without time*/, int /*max time observed*/ > actionsTime = new Dictionary<Action, int>();
            foreach (Action a in UsedJointActions)
            {
                Action aWithoutTime = a.RemoveTime();

                // The list already contains the action (without time)?
                if (actionsTime.Count(x=>x.Key.Name == aWithoutTime.Name) > 0)
                {
                    KeyValuePair<Action,int> existingAction = actionsTime.Where(x => x.Key.Name == aWithoutTime.Name).First();

                    if (existingAction.Value < a.GetTime())
                    {
                        actionsTime[existingAction.Key] = a.GetTime();
                    }
                }
                else
                {
                    actionsTime.Add(aWithoutTime, a.GetTime());
                }
            }
            return actionsTime;
        }
    }
}