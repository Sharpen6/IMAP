using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Forms.Draw
{
    class Tile
    {
        List<Element> elements = new List<Element>();
        internal void AddItem(Element agent)
        {
            elements.Add(agent);
        }

        public override string ToString()
        {
            string output = "";
            foreach (var item in elements)
            {
                output += " " + item;
            }
            if (elements.Count > 0)
            {
                output = output.Substring(1);
            }
            return output;
        }
    }
}
