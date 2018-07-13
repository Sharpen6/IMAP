using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Costs
{
    public class BoxDebugCostGenerator : AbstractCostGenerator
    {
        public override int GetCost(IMAP.Action a)
        {

            string name = a.GetOperationName();
            int tmp = 0;
            if (name.StartsWith("R") && int.TryParse(name.Substring(name.IndexOf('R') + 1), out tmp) ||
                name.StartsWith("art-"))
            {
                return 1;
            }
            else
            {
                switch (name)
                {

                    // no op <  2 * move (better to stay then to move back and forth)
                    case "wait-goal":
                        return 0;
                        break;
                    case "observe-box":
                        return 2;
                        break;
                    case "push":
                        return 3;
                        break;
                    case "joint-push":
                        return 3;
                        break;
                    case "move":
                        return 2;
                        break;
                    case "no-op":
                        return 3;
                        break;
                    
                    /*case "wait-goal":
                        return 0;
                        break;
                    case "observe-box":
                        return 5;
                        break;
                    case "push":
                        return 30;
                        break;
                    case "joint-push":
                        return 60;
                        break;
                    case "move":
                        return 1;
                        break;
                    case "no-op":
                        return 1;
                        break;

                    default:
                        return 1;
                        throw new Exception();
                        break;*/
                }
                return 1;
            }
        }
    }
}
