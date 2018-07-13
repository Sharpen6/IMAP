using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP
{
    class StringExpression : Expression
    {
        public string Value { get; private set; }
        public StringExpression(string sValue)
        {
            Value = sValue;
        }
        public override string ToString()
        {
            return Value;
        }
    }
}
