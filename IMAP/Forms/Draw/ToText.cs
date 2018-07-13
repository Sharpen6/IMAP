using IMAP.Formulas;
using IMAP.General;
using IMAP.Predicates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Forms.Draw
{
    class ToText
    {
        public string PrintProblem(Domain domain, Problem problem)
        {
            Tile[,] map = GetMapDetails(domain, problem);

            int rowLength = map.GetLength(0);
            int colLength = map.GetLength(1);

            int maxLength = 0;
            for (int i = 0; i < colLength; i++)
            {
                for (int j = 0; j < rowLength; j++)
                {
                    if (map[j, i].ToString().Length > maxLength)
                        maxLength = map[j, i].ToString().Length;
                }
            }
            maxLength += 1;
            string output = "";
            bool firstLine = true;
            int lineWidth = 0;
            for (int i = 0; i < colLength; i++)
            {
                for (int j = 0; j < rowLength; j++)
                {
                    output += map[j, i].ToString().PadLeft(maxLength) + " |";
                }
                if (firstLine)
                {
                    lineWidth = output.Length;
                    firstLine = false;
                }
                output += "\r\n" + "".PadLeft(lineWidth, '-') + "\r\n";
            }
            return output;
        }
        private Tile[,] GetMapDetails(Domain domain, Problem problem)
        {
            int width = 0;
            int hight = 0;

            foreach (var item in domain.Constants)
            {
                if (item.Name.StartsWith("p"))
                {
                    string[] pos = item.Name.Substring(1).Split('-');
                    int curr_width = int.Parse(pos[0]);
                    int curr_hight = int.Parse(pos[1]);

                    if (curr_hight > hight)
                        hight = curr_hight;
                    if (curr_width > width)
                        width = curr_width;
                }
            }

            Tile[,] map = new Tile[width, hight];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < hight; j++)
                {
                    map[i, j] = new Tile();
                }
            }

            foreach (var item in domain.Constants)
            {
                if (item.Name.StartsWith("a"))
                {
                    foreach (var item2 in problem.Known)
                    {
                        if (item2.Name == "agent-at")
                        {
                            if (item2 is GroundedPredicate)
                            {
                                if (((GroundedPredicate)item2).Negation == false)
                                    if (((GroundedPredicate)item2).Constants[0].Name == item.Name)
                                    {
                                        string[] pos = ((GroundedPredicate)item2).Constants[1].Name.Substring(1).Split('-');
                                        int x = int.Parse(pos[0]);
                                        int y = int.Parse(pos[1]);
                                        map[x - 1, y - 1].AddItem(new Agent(item.Name));
                                        break;
                                    }
                            }
                        }
                    }
                }
                if (item.Name.StartsWith("b"))
                {
                    bool isHeavy = false;
                    foreach (var item2 in problem.Known)
                    {
                        if (item2.Name == "heavy")
                        {
                            if (item2 is GroundedPredicate)
                            {
                                if (((GroundedPredicate)item2).Constants.First().Name == item.Name)
                                {
                                    if (!item2.Negation)
                                        isHeavy = true;
                                }
                            }
                        }
                    }
                    foreach (var item2 in problem.Known)
                    {
                        if (item2.Name == "box-at")
                        {
                            if (item2 is GroundedPredicate)
                            {
                                if (((GroundedPredicate)item2).Negation == false)
                                    if (((GroundedPredicate)item2).Constants[0].Name == item.Name)
                                    {
                                        string[] pos = ((GroundedPredicate)item2).Constants[1].Name.Substring(1).Split('-');
                                        int x = int.Parse(pos[0]);
                                        int y = int.Parse(pos[1]);
                                        map[x - 1, y - 1].AddItem(new Box(item.Name, isHeavy));
                                        break;
                                    }
                            }
                        }
                    }
                }
            }

            foreach (var item2 in problem.Hidden)
            {
                CompoundFormula cf = item2;
                if (cf.Operator == "oneof")
                {
                    foreach (var op in cf.Operands)
                    {

                        string[] pos = ((GroundedPredicate)((PredicateFormula)op).Predicate).Constants[1].Name.Substring(1).Split('-');
                        int x = int.Parse(pos[0]);
                        int y = int.Parse(pos[1]);
                        string boxID = ((GroundedPredicate)((PredicateFormula)op).Predicate).Constants[0].Name;
                        bool isHeavy = false;
                        foreach (var item3 in problem.Known)
                        {
                            if (item3.Name == "heavy")
                            {
                                if (item3 is GroundedPredicate)
                                {
                                    if (((GroundedPredicate)item3).Constants.First().Name == boxID)
                                    {
                                        if (!item3.Negation)
                                            isHeavy = true;
                                    }
                                }
                            }
                        }
                        map[x - 1, y - 1].AddItem(new Box(boxID, isHeavy));
                    }
                }
            }
            return map;
        }   
    }
}
