﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP
{
    class KnowledgeAction : Action
    {
        public bool ReasoningAction { get{ return Original == null; } }
        public Action Original { get; private set; }
        public KnowledgeAction(Action aOriginal)
            : base(aOriginal.Name)
        {
            Original = aOriginal;
        }
        public KnowledgeAction(string sName)
            : base(sName)
        {
            Original = null;
        }
    }
}
