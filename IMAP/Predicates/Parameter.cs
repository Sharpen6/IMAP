using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP.Predicates
{
    public class Parameter : Argument
    {
        public Parameter(string sType, string sName)
            : base(sType, sName)
        {
        }
        public Parameter(int iType, string sName)
            : base(iType, sName)
        {
        }
        public string FullString()
        {
            //return "(" + Name + " " + Type + ")";
            if (Type != "")
                return Name + " - " + Type;
            
            return Name;
        }
        public override string ToString()
        {
            //return "(" + Name + " " + Type + ")";
            return Name;
        }
    }
}
