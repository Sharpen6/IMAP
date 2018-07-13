using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.PlanTree
{
    class PlanTreePrinter
    {
        public static string Print(ConditionalPlanTreeNode root)
        {
            StringBuilder sb = new StringBuilder();
            RecPrint(root, 0, "", ref sb);
            return sb.ToString();
        }
        private static void RecPrint(ConditionalPlanTreeNode node, int depth, string path, ref StringBuilder sb)
        {
            if (node == null)
                return;
            if (node.Action != null)
            { 
                sb.AppendLine(path + node.Action.Name.ToString());
            }
            if (node.SingleChild != null)
            {
                string tPath = path + "\t";
                RecPrint(node.SingleChild, depth + 1, tPath, ref sb);
            }
            if (node.FalseObservationChild != null)
            {
                string tPath = path + "\t(f)";
                RecPrint(node.FalseObservationChild, depth + 1, tPath, ref sb);
            }
            if (node.TrueObservationChild != null)
            {
                string tPath = path + "\t(t)";
                RecPrint(node.TrueObservationChild, depth + 1, tPath, ref sb);
            }
        }
    }
}
