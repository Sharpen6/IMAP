using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Costs
{
    public abstract class AbstractCostGenerator
    {
        public abstract int GetCost(Action a);
        public int GetWaitForGoalCost()
        {
            return 0;
        }
        public int GetNoOpCost()
        {
            return 1;
        }
        public int GetArtCost()
        {
            return 1;
        }
    }
}
