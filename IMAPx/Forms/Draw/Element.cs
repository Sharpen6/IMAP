using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Forms.Draw
{
    internal abstract class Element
    {
        string name;

        public Element(string name)
        {
            this.name = name;
        }
        public override string ToString()
        {
            return name;
        }
    }
}
