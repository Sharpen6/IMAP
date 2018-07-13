using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP.Predicates
{
    public class Constant : Argument
    {
        public Constant(string sType, string sName)
            : base(sType, sName)
        {

        }
        public Constant(int iType, string sName)
            : base(iType, sName)
        {

        }
    }
}
