using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;
using IMAP.Predicates;
using IMAP.Formulas;

namespace IMAP.General
{
    public class Parser
    {
        /// <summary>
        /// CTR
        /// </summary>
        public Parser()
        {

        }
        public static void ParseDomainAndProblem(string sFileName, out Domain d, out Problem p)
        {
            string sPath = sFileName.Substring(0, sFileName.LastIndexOf(@"\") + 1);
            StreamReader sr = new StreamReader(sFileName);

            Stack<string> s = ToStack(sr);
            CompoundExpression eDomain = (CompoundExpression)ToExpression(s);
            CompoundExpression eProblem = (CompoundExpression)ToExpression(s);
            sr.Close();
            d = ParseDomain(eDomain, sPath, "");
            p = ParseProblem(eProblem,d, "");
        }
        public static Domain ParseDomain(string sDomainFile, string sAgentCallsign)
        {
            string sPath = sDomainFile.Substring(0, sDomainFile.LastIndexOf(@"\") + 1);
            StreamReader sr = new StreamReader(sDomainFile);
            CompoundExpression exp = (CompoundExpression)ToExpression(sr);
            sr.Close();

            Domain d = ParseDomain(exp, sPath, sDomainFile);
            d.AgentCallsign = sAgentCallsign;
            return d;
        }
        private static Domain ParseDomain(CompoundExpression exp, string sPath, string sDomainPath)
        {
            Domain d = null;
            foreach (Expression e in exp.SubExpressions)
            {
                if (e is CompoundExpression)
                {
                    CompoundExpression eSub = (CompoundExpression)e;
                    if (eSub.Type == "domain")
                        d = new Domain(eSub.SubExpressions.First().ToString(), sPath);
                    else if (eSub.Type == ":requirements")
                    {
                    }
                    else if (eSub.Type == ":types")
                    {
                        foreach (Expression eType in ((CompoundExpression)eSub).SubExpressions)
                            d.Types.Add(eType.ToString());
                    }
                    else if (eSub.Type == ":constants")
                    {
                        ReadConstants(eSub, d);
                    }
                    else if (eSub.Type == ":functions")
                    {
                        ReadFunctions(eSub, d);
                    }
                    else if (eSub.Type == ":predicates")
                    {
                        ReadPredicates(eSub, d);
                    }
                    else if (eSub.Type == ":action")
                    {
                        d.AddAction(ReadAction(eSub, d));
                    }
                }
            }
            d.FilePath = sDomainPath;
            return d;
        }
        public static Problem ParseProblem(string sProblemFile, Domain d)
        {
            StreamReader sr = new StreamReader(sProblemFile);
            CompoundExpression exp = (CompoundExpression)ToExpression(sr);
            sr.Close();
            return ParseProblem(exp, d, sProblemFile);
        }
        private static Problem ParseProblem(CompoundExpression exp, Domain d, string sProblemPath)
        {
            Problem p = null;
            CompoundExpression eSub = null;
            foreach (Expression e in exp.SubExpressions)
            {
                eSub = (CompoundExpression)e;
                if (eSub.Type == "problem")
                {
                    p = new Problem(eSub.SubExpressions.First().ToString(), d);
                }
                if (eSub.Type == ":domain")
                {
                    if (eSub.SubExpressions.First().ToString() != d.Name)
                        throw new InvalidDataException("Domain and problem files don't match!");
                }
                if (eSub.Type == ":objects")
                {
                    ReadConstants(eSub, d);
                }
                if (eSub.Type == ":init")
                {
                    CompoundExpression eAnd = (CompoundExpression)eSub.SubExpressions.First();
                    if (eAnd.Type == "and")
                        ReadInitState(p, d, eAnd);
                    else
                        ReadInitState(p, d, eSub);
                    //throw new InvalidDataException("Expecting 'and', got " + eAnd.Type);
                }
                if (eSub.Type == ":goal")
                    ReadGoal(p, d, eSub.SubExpressions[0]);
                if (eSub.Type == ":metric")
                    ReadMetric(p, d, eSub);
            }
            //p.AddReasoningActions(); not needed as long as we use FF to do the planning for us
            d.ComputeAlwaysKnown();
            p.CompleteKnownState();


            List<Predicate> lConstantPredicates = new List<Predicate>();
            foreach (Predicate pKnown in p.Known)
            {
                if (d.AlwaysConstant(pKnown))
                    lConstantPredicates.Add(pKnown);
            }
            d.RemoveUniversalQuantifiers(lConstantPredicates);
            p.RemoveUniversalQuantifiers();
            p.FilePath = sProblemPath;
            return p;
        }
        private static void ReadFunctions(CompoundExpression eFunctions, Domain d)
        {
            foreach (Expression eSub in eFunctions.SubExpressions)
            {
                if (eSub.ToString() != ":functions")
                {
                    CompoundExpression eFunction = (CompoundExpression)eSub;
                    //BUGBUG - only reading non parametrized functions for now
                    d.AddFunction("(" + eFunction.Type + ")");
                }
                
            }
        }
        private static void ReadConstants(CompoundExpression exp, Domain d)
        {
            string sType = "?", sExp = "";
            List<string> lUndefined = new List<string>();
            for( int iExpression  = 0 ; iExpression < exp.SubExpressions.Count ; iExpression++)
            {
                sExp = exp.SubExpressions[iExpression].ToString().Trim();
                if ( sExp == "-")
                {
                    sType = exp.SubExpressions[iExpression + 1].ToString();
                    iExpression++;
                    foreach (string sName in lUndefined)
                    {
                        Constant newC = new Constant(sType, sName);
                        if (!d.Constants.Contains(newC))
                            d.AddConstant(newC);
                    }
                    lUndefined.Clear();
                }
                else if( !sExp.StartsWith(":"))
                {
                    lUndefined.Add(sExp);
                }
            }
            if (lUndefined.Count > 0)
            {
                //supporting objects with undefined types as type "OBJ"
                foreach(string sName in lUndefined)
                    d.AddConstant(new Constant("OBJ", sName));
                //throw new NotImplementedException();
            }
           
        }
        private static void ReadPredicates(CompoundExpression exp, Domain d)
        {
            foreach (Expression e in exp.SubExpressions)
            {
                Predicate p = ReadPredicate((CompoundExpression)e, d);
                d.AddPredicate(p);
            }
        }
        private static Predicate ReadPredicate(CompoundExpression exp, Domain d)
        {
            ParameterizedPredicate pp = new ParameterizedPredicate(exp.Type);
            int iExpression = 0;
            Parameter p = null;
            string sName = "";
            List<Parameter> lUntypedParameters = new List<Parameter>();
            for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression++)
            {
                sName = exp.SubExpressions[iExpression].ToString();
                if (sName == "-")
                {
                    string sType = exp.SubExpressions[iExpression + 1].ToString();
                    foreach (Parameter pUntyped in lUntypedParameters)
                        pUntyped.Type = sType;
                    lUntypedParameters.Clear();
                    iExpression++;//skip the "-" and the type
                }
                else
                {
                    p = new Parameter("", sName);
                    lUntypedParameters.Add(p);
                    pp.AddParameter(p);
                }
            }
            if (d.Types.Count == 1)
            {
                foreach (Parameter pUntyped in lUntypedParameters)
                    pUntyped.Type = d.Types[0];
            }
            return pp;
        }
        private static GroundedPredicate ReadGroundedPredicate(CompoundExpression exp, Domain d)
        {
            GroundedPredicate gp = new GroundedPredicate(exp.Type);
            int iExpression = 0;
            Constant c = null;
            string sName = "";
            for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression++)
            {
                sName = exp.SubExpressions[iExpression].ToString();
                c = d.GetConstant(sName);
                gp.AddConstant(c);
            }
            return gp;
        }
        private static Action ReadAction(CompoundExpression exp, Domain d)
        {
            string sName = exp.SubExpressions[0].ToString();
            Action pa = null; 
            int iExpression = 0;
            for (iExpression = 1; iExpression < exp.SubExpressions.Count; iExpression++)
            {
                if (exp.SubExpressions[iExpression].ToString() == ":parameters")
                {
                    CompoundExpression ceParams = (CompoundExpression)exp.SubExpressions[iExpression + 1];
                    if (ceParams.Type != "N/A")
                    {
                        pa = new ParametrizedAction(sName);
                        ReadParameters((CompoundExpression)exp.SubExpressions[iExpression + 1], (ParametrizedAction)pa);
                    }
                    iExpression++;
                }
                else if (exp.SubExpressions[iExpression].ToString() == ":effect")
                {
                    if (pa == null)
                        pa = new Action(sName);
                    ReadEffect((CompoundExpression)exp.SubExpressions[iExpression + 1], pa, d, pa is ParametrizedAction);
                    iExpression++;
                }
                else if (exp.SubExpressions[iExpression].ToString() == ":precondition")
                {
                    if (pa == null)
                        pa = new Action(sName);
                    ReadPrecondition((CompoundExpression)exp.SubExpressions[iExpression + 1], pa, d, pa is ParametrizedAction);
                    iExpression++;
                }
                else if (exp.SubExpressions[iExpression].ToString() == ":observe")
                {
                    if (pa == null)
                        pa = new Action(sName);
                    ReadObserve((CompoundExpression)exp.SubExpressions[iExpression + 1], pa, d, pa is ParametrizedAction);
                    iExpression++;
                }
            }
            return pa;
        }
        private static void ReadParameters(CompoundExpression exp, ParametrizedAction pa)
        {
           // unfortunately, expressions have a weird non standard structure with no type -(? i - pos ? j - pos )
          //  so we must have a special case 
            List<string> lTokens = exp.ToTokenList();
            List<string> lNames = new List<string>();
            string sType = "";
            int iCurrent = 0;
            while (iCurrent < lTokens.Count)
            {
                if (lTokens[iCurrent] == "-")
                {
                    sType = lTokens[iCurrent + 1];
                    foreach (string sName in lNames)
                        pa.AddParameter(new Parameter(sType, sName));
                    lNames = new List<string>();
                    sType = "";
                    iCurrent += 2;
                }
                else
                {
                    lNames.Add(lTokens[iCurrent]);
                    iCurrent++;
                }
            }
            if (lNames.Count != 0) //allowing no types specified
            {
                foreach (string sName in lNames)
                    pa.AddParameter(new Parameter("OBJ", sName));
            }

        }
        private static Formula ReadFormula(CompoundExpression exp, Dictionary<string, string> dParameterNameToType, bool bParamterized, Domain d)
        {
            bool bPredicate = true;
            //Console.WriteLine(exp);
            if (d!= null && d.IsFunctionExpression(exp.Type))
            {
                Predicate p = ReadFunctionExpression(exp, dParameterNameToType, d);
                return new PredicateFormula(p);
            }
            else if (IsUniversalQuantifier(exp))
            {
                CompoundExpression eParameter = (CompoundExpression)exp.SubExpressions[0];
                CompoundExpression eBody = (CompoundExpression)exp.SubExpressions[1];
                string sParameter = eParameter.Type;
                string sType = eParameter.SubExpressions[1].ToString();
                dParameterNameToType[sParameter] = sType;
                ParametrizedFormula cfQuantified = new ParametrizedFormula(exp.Type);
                cfQuantified.Parameters[sParameter] = sType;
                Formula fBody = ReadFormula(eBody, dParameterNameToType, true, d);
                cfQuantified.AddOperand(fBody);
                return cfQuantified;
            }
            else if (exp.Type == "probabilistic")
            {
                ProbabilisticFormula pf = new ProbabilisticFormula();
                int iExpression = 0;
                for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression+=2)
                {
                    //if (exp.SubExpressions[iExpression] is StringExpression)
                    //    throw new InvalidDataException();
                    string sProb = exp.SubExpressions[iExpression].ToString();
                    double dProb = 0.0;
                    if (sProb.Contains("/"))
                    {
                        string[] a = sProb.Split('/');
                        dProb = double.Parse(a[0]) / double.Parse(a[1]);
                    }
                    else
                    {
                        dProb = double.Parse(sProb);
                    }
                    Formula f = ReadFormula((CompoundExpression)exp.SubExpressions[iExpression + 1], dParameterNameToType, bParamterized, d);
                    pf.AddOption(f, dProb);
                }
                return pf;
            }
            else
            {
                foreach (Expression eSub in exp.SubExpressions)
                {
                    if (eSub is CompoundExpression)
                    {
                        bPredicate = false;
                        break;
                    }
                }
                if (bPredicate)
                    return ReadPredicate(exp, dParameterNameToType, bParamterized, d);
                else
                {
                    CompoundFormula cf = new CompoundFormula(exp.Type);
                    int iExpression = 0;
                    for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression++)
                    {
                        if (exp.SubExpressions[iExpression] is StringExpression)
                            throw new InvalidDataException();
                        Formula f = ReadFormula((CompoundExpression)exp.SubExpressions[iExpression], dParameterNameToType, bParamterized, d);
                        cf.SimpleAddOperand(f);
                    }
                    if (cf.Operator == "not" && cf.Operands[0] is PredicateFormula)
                    {
                        PredicateFormula fNegate = new PredicateFormula(((PredicateFormula)cf.Operands[0]).Predicate.Negate());
                        return fNegate;
                    }
                    return cf;
                }
            }
        }
        private static bool IsUniversalQuantifier(CompoundExpression exp)
        {
            return exp.Type.ToLower() == "forall" || exp.Type.ToLower() == "exists";
        }
        private static Formula ReadGroundedFormula(CompoundExpression exp, Domain d)
        {
            bool bPredicate = true;
            if (IsUniversalQuantifier(exp))
            {
                CompoundExpression eParameter = (CompoundExpression)exp.SubExpressions[0];
                CompoundExpression eBody = (CompoundExpression)exp.SubExpressions[1];
                string sParameter = eParameter.Type;
                string sType = eParameter.SubExpressions[1].ToString();
                Dictionary<string, string> dParameterNameToType = new Dictionary<string, string>();
                dParameterNameToType[sParameter] = sType;
                ParametrizedFormula cfQuantified = new ParametrizedFormula(exp.Type);
                cfQuantified.Parameters[sParameter] = sType;
                Formula fBody = ReadFormula(eBody, dParameterNameToType, true, d);
                cfQuantified.AddOperand(fBody);
                return cfQuantified;
            }
            foreach (Expression eSub in exp.SubExpressions)
            {
                if (eSub is CompoundExpression)
                {
                    bPredicate = false;
                    break;
                }
            }
            if (bPredicate)
                return new PredicateFormula(ReadGroundedPredicate(exp, d));
            else
            {
                CompoundFormula cf = new CompoundFormula(exp.Type);
                int iExpression = 0;
                for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression++)
                {
                    Formula f = ReadGroundedFormula((CompoundExpression)exp.SubExpressions[iExpression], d);
                    cf.AddOperand(f);
                }
                if (cf.Operator == "not" && cf.Operands[0] is PredicateFormula)
                {
                    return new PredicateFormula(((PredicateFormula)cf.Operands[0]).Predicate.Negate());
                }
                return cf;
            }
        }
        private static Formula ReadPredicate(CompoundExpression exp, Dictionary<string, string> dParameterNameToType, bool bParametrized, Domain d)
        {
            Predicate p = null;
            int iExpression = 0;
            string sName = "";

            if (bParametrized)
                p = new ParameterizedPredicate(exp.Type);
            else
                p = new GroundedPredicate(exp.Type);
            bool bAllConstants = true;
            for (iExpression = 0; iExpression < exp.SubExpressions.Count; iExpression++)
            {
                sName = exp.SubExpressions[iExpression].ToString();
                if (bParametrized)
                {
                    Argument a = null;
                    if (sName.StartsWith("?"))
                    {
                       // if (!dParameterNameToType.ContainsKey(sName))
                       //     dParameterNameToType.Add(sName,)
                        a = new Parameter(dParameterNameToType[sName], sName);
                        bAllConstants = false;
                    }
                    else
                    {
                        if (!d.ConstantNameToType.ContainsKey(sName))
                        {
                            d.ConstantNameToType.Add(sName, dParameterNameToType[sName]);
                            //throw new Exception("Predicate " + sName + " undefined");// SAGI co
                        }
                         a = new Constant(d.ConstantNameToType[sName], sName);
                    }
                    ((ParameterizedPredicate)p).AddParameter(a);
                }
                else
                {
                    try
                    {
                        Constant c = new Constant(d.ConstantNameToType[sName], sName);
                        ((GroundedPredicate)p).AddConstant(c);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                    }
                }
            }
            if (bParametrized)
                if (!MatchParametersToPredicateDeclaration((ParameterizedPredicate)p, d))
                    throw new NotImplementedException();

            if (bParametrized && bAllConstants)
            {
                GroundedPredicate gp = new GroundedPredicate(p.Name);
                foreach (Constant c in ((ParameterizedPredicate)p).Parameters)
                    gp.AddConstant(c);
                p = gp;
            }


            PredicateFormula vf = new PredicateFormula(p);
            return vf;
        }
        private static bool MatchParametersToPredicateDeclaration(ParameterizedPredicate pp, Domain d)
        {
            foreach (Predicate pDefinition in d.Predicates)
            {
                if (pDefinition.Name == pp.Name)
                {
                    if (pDefinition is ParameterizedPredicate)
                    {
                        ParameterizedPredicate ppDefinition = (ParameterizedPredicate)pDefinition;
                        if (pp.Parameters.Count() != ppDefinition.Parameters.Count())
                            return false;
                        for (int i = 0; i < pp.Parameters.Count(); i++)
                        {
                            if (ppDefinition.Parameters.ElementAt(i).Type == "")
                                ppDefinition.Parameters.ElementAt(i).Type = pp.Parameters.ElementAt(i).Type;
                            else if (ppDefinition.Parameters.ElementAt(i).Type != pp.Parameters.ElementAt(i).Type)
                                return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        private static void ReadEffect(CompoundExpression exp, Action pa, Domain d, bool bParametrized)
        {
            string sOperator = exp.Type;
            Formula f = null;
            if (pa is ParametrizedAction)
                f = ReadFormula(exp, ((ParametrizedAction)pa).ParameterNameToType, bParametrized, d);
            else
                f = ReadFormula(exp, d.ConstantNameToType, bParametrized, d);
            pa.SetEffects(f);
            
        }
        private static void ReadPrecondition(CompoundExpression exp, Action pa, Domain d, bool bParametrized)
        {
            string sOperator = exp.Type;
            Formula f = null;
            if (pa is ParametrizedAction)
                f = ReadFormula(exp, ((ParametrizedAction)pa).ParameterNameToType, bParametrized, d);
            else
                f = ReadFormula(exp, d.ConstantNameToType, bParametrized, d);
            pa.Preconditions = f;
        }
        private static void ReadObserve(CompoundExpression exp, Action pa, Domain d, bool bParametrized)
        {
            string sOperator = exp.Type;
            Formula f = null;
            if (pa is ParametrizedAction)
                f = ReadFormula(exp, ((ParametrizedAction)pa).ParameterNameToType, bParametrized, d);
            else
                f = ReadFormula(exp, d.ConstantNameToType, bParametrized, d);
            pa.Observe = f;
        }
        public static string ListToString(IList l)
        {
            if (l.Count == 0)
                return "";
            string s = "";
            for (int i = 0; i < l.Count - 1; i++)
            {
                s += l[i].ToString() + " ";
            }
            s += l[l.Count - 1] + "";
            return s;
        }
        public static string ListToString(IList l, int cTabs)
        {
            if (l.Count == 0)
                return "";
            string s = "";
            for (int i = 0; i < l.Count - 1; i++)
            {
                if (l[i] is CompoundFormula)
                    s += ((CompoundFormula)l[i]).ToString(cTabs);
                else
                    s += l[i].ToString() + " ";
            }
            if (l[l.Count - 1] is CompoundFormula)
                s += ((CompoundFormula)l[l.Count - 1]).ToString(cTabs) + "";
            else
                s += l[l.Count - 1] + "";
            return s;
        }
        public static string ListToStringII(IList l)
        {
            if (l.Count == 0)
                return "()";
            string s = "(";
            for (int i = 0; i < l.Count - 1; i++)
            {
                s += l[i].ToString() + " ";
            }
            s += l[l.Count - 1] + ")";
            return s;
        }
        private static void ReadMetric(Problem p, Domain d, CompoundExpression eSub)
        {
            p.AddMetric(eSub.ToString());
        }
        private static void ReadGoal(Problem p, Domain d, Expression eGoal)
        {
            Formula fGoal = ReadGroundedFormula((CompoundExpression)eGoal, d);
            p.Goal = fGoal;
        }
        private static void ReadInitState(Problem p, Domain d, CompoundExpression eInitState)
        {

            foreach (Expression e in eInitState.SubExpressions)
            {
                CompoundExpression eSub = (CompoundExpression)e;
                if(d.IsFunctionExpression(eSub.Type))
                { 
                    p.AddKnown(ReadFunctionExpression(eSub, null, d));
                }
                else if (d.ContainsPredicate(eSub.Type))
                {
                    p.AddKnown(ReadGroundedPredicate(eSub, d));
                }
                else
                {
                    if (eSub.Type != "unknown")
                    {
                        Formula f = ReadGroundedFormula(eSub, d);
                        if (f is CompoundFormula)
                            p.AddHidden((CompoundFormula)f);
                        if (f is PredicateFormula)//this happens in (not (p)) statments
                            p.AddKnown(((PredicateFormula)f).Predicate);
                    }
                    else
                    {
                        //causing a problem - add operand does some basic reasoning - adding p and ~p results in true for or statements.
                        //skipping unknown for now...
                        
                        Predicate pUnknown = ReadGroundedPredicate((CompoundExpression)eSub.SubExpressions[0], d);
                        CompoundFormula cfOr = new CompoundFormula("or");
                        cfOr.SimpleAddOperand(pUnknown);
                        cfOr.SimpleAddOperand(pUnknown.Negate());
                        p.AddHidden(cfOr);
                        
                    }
                }
            }
            p.ComputeRelevanceClosure();
        }
        public static Predicate ReadFunctionExpression(CompoundExpression exp, Dictionary<string, string> dParameterNameToType, Domain d)
        {
            Constant c = null;
            string sName = exp.SubExpressions[0].ToString();

            if (exp.Type == "=")
            {
                GroundedPredicate gp = new GroundedPredicate("=");
                gp.AddConstant(new Constant("cost", "(total-cost)"));
                gp.AddConstant(new Constant("", "0"));
                return gp;



                /*
                string sParam1 = exp.SubExpressions[0].ToString();
                string sParam2 = exp.SubExpressions[1].ToString();
                if (!dParameterNameToType.ContainsKey(sParam1))
                    throw new ArgumentException("First argument of = must be a parameter");
                ParameterizedPredicate pp = new ParameterizedPredicate("=");
                pp.AddParameter(new Parameter(dParameterNameToType[sParam1], sParam1));
                if (dParameterNameToType.ContainsKey(sParam2))
                    pp.AddParameter(new Parameter(dParameterNameToType[sParam2], sParam2));
                else
                    pp.AddParameter(new Constant(d.ConstantNameToType[sParam2], sParam2));
                return pp;*/

                /*
                 * Sag: I dont know what was the meaning of this code, but it was meant not to work. If = is given,
                 * dParameterNameToType will be null and then if (!dParameterNameToType.ContainsKey(sParam1)) will throw null exception.
                 * */
            }


            GroundedPredicate p = new GroundedPredicate(exp.Type);
            double dValue = 0.0;
            if (d.Functions.Contains(sName))
                c = new Constant("Function", sName);
            else
                throw new ArgumentException("First argument of increase or decrease must be a function");
            p.AddConstant(c);

            sName = exp.SubExpressions[1].ToString();
            if (double.TryParse(sName, out dValue))
                c = new Constant("Number", sName);
            else
                throw new ArgumentException("Second argument of increase or decrease must be a number");
            p.AddConstant(c);
            return p;
        }
        private static Stack<string> ToStack(StreamReader sr)
        {
            Stack<string> lStack = new Stack<string>();
            char[] aDelimiters = { ' ', '\n', '(', ')' };
            Stack<string> sTokens = new Stack<string>();
            string sToken = "";
            while (!sr.EndOfStream)
            {
                string sLine = sr.ReadLine();
                sLine = sLine.Trim().ToLower();
                if (sLine.StartsWith(";;") && sLine.Contains("cell for initial open"))
                    sLine = sLine.Substring(2);
                //if(sLine.Contains("move0"))
                //    Debug.WriteLine("BUGBUG");
                if (sLine.Contains(";"))
                    sLine = sLine.Substring(0, sLine.IndexOf(";")).Trim();
                sLine += " ";
                foreach (char c in sLine)
                {
                    if (aDelimiters.Contains(c))
                    {
                        sToken = sToken.Trim();
                        sTokens.Push(sToken);
                        sTokens.Push(c + "");
                        sToken = "";
                    }
                    else
                    {
                        sToken += c;
                    }
                }
            }
            sToken = sToken.Trim();
            if (sToken.Length > 0)
                sTokens.Push(sToken);
            Stack<string> sReveresed = new Stack<string>();
            while (sTokens.Count > 0)
            {
                sToken = sTokens.Pop().Trim();
                if (sToken.Length > 0)
                    sReveresed.Push(sToken);
            }
            return sReveresed;
        }
        private static Stack<string> ToStackII(StreamReader sr)
        {
            Stack<string> lStack = new Stack<string>();
            string sAll = ReadToEnd(sr);
            char[] aDelimiters = { ' ', '\n', '(', ')' };
            Stack<string> sTokens = new Stack<string>();
            string sToken = "";
            foreach (char c in sAll)
            {
                if (aDelimiters.Contains(c))
                {
                    sToken = sToken.Trim();
                    sTokens.Push(sToken);
                    sTokens.Push(c + "");
                    sToken = "";
                }
                else
                {
                    sToken += c;
                }
            }
            sToken = sToken.Trim();
            if (sToken.Length > 0)
                sTokens.Push(sToken);
            Stack<string> sReveresed = new Stack<string>();
            while (sTokens.Count > 0)
            {
                sToken = sTokens.Pop().Trim();
                if (sToken.Length > 0)
                    sReveresed.Push(sToken);
            }
            return sReveresed;
        }
        private static string ReadToEnd(StreamReader sr)
        {
            string sAll = "";
            while (!sr.EndOfStream)
            {
                string sLine = sr.ReadLine();
                if (sLine.Contains(";"))
                    sLine = sLine.Substring(0, sLine.IndexOf(";")).Trim();
                if (sLine.Length > 0)
                    sAll += sLine + " ";
            }
            return sAll;
        }
        private static Expression ToExpression(StreamReader sr)
        {
            Stack<string> s = ToStack(sr);
            Expression e = ToExpression(s);
            return e;
        }
        private static Expression ToExpression(Stack<string> sStack)
        {
            string sToken = sStack.Pop();
            if (sToken == "(")
            {
                bool bDone = false;
                CompoundExpression exp = new CompoundExpression();
                sToken = sStack.Pop();
                if (sToken == ")")
                {
                    exp.Type = "N/A";
                    bDone = true;
                }
                else
                    exp.Type = sToken;
                while (!bDone)
                {
                    if (sStack.Count == 0)
                        throw new InvalidDataException("Exp " + exp.Type + " was not closed");
                    sToken = sStack.Pop();
                    if (sToken == ")")
                        bDone = true;
                    else
                    {
                        sStack.Push(sToken);
                        exp.SubExpressions.Add(ToExpression(sStack));
                    }

                }
                return exp;
            }
            else
            {
                return new StringExpression(sToken);
            }
        }
        public static CompoundFormula ParseFormula(string sFile, Domain d)
        {
            string sPath = sFile.Substring(0, sFile.LastIndexOf(@"\") + 1);
            StreamReader sr = new StreamReader(sFile);
            CompoundExpression exp = (CompoundExpression)ToExpression(sr);
            sr.Close();
            Formula cf = ReadFormula(exp, null, false, d);
            return (CompoundFormula)cf;
        }
    }
}
