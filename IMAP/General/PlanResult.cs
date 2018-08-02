using IMAP.PlanTree;
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
    }
}
