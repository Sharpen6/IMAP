using IMAP.Costs;
using IMAP.Forms.Draw;
using IMAP.Formulas;
using IMAP.General;
using IMAP.PlanTree;
using IMAP.Predicates;
using IMAP.SDRPlanners;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMAP.Forms
{
    public partial class frmProblem : Form
    {
        string m_DomainPath = null;
        string m_ProblemPath = null;
        string m_AgentCallsign = "";
        public frmProblem()
        {
            InitializeComponent();

            InitTextFields();
            InitBenchmarksList();
        }

        private class Folder
        {
            public string path { get; set; }
            public Folder(string path)
            {
                this.path = path;
            }
            public override string ToString()
            {
                return path.Replace(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar, "");
            }
        }
        private void InitBenchmarksList()
        {
            lbBenchmarks.Items.Clear();
            List<Folder> lbItems = new List<Folder>();
            foreach (string path in Directory.GetDirectories(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\"))
            {       
                lbItems.Add(new Folder(path));
            }
            lbBenchmarks.DataSource = lbItems;           
        }

        private void InitTextFields()
        {
            txtDomainPath.Text = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\S1\d.pddl";
            txtProblemPath.Text = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\TestFiles\S1\p.pddl";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ofdSelectFile.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofdSelectFile.FileName))
                {
                    txtDomainPath.Text = ofdSelectFile.FileName;
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (ofdSelectFile.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(ofdSelectFile.FileName))
                {
                    txtProblemPath.Text = ofdSelectFile.FileName;
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string domainPath = txtDomainPath.Text;
            string problemPath = txtProblemPath.Text;

            if (File.Exists(domainPath) && File.Exists(problemPath) && txtAgentCallsign.Text!="")
            {
                m_DomainPath = txtDomainPath.Text;
                m_ProblemPath = txtProblemPath.Text;
                m_AgentCallsign = txtAgentCallsign.Text;

                DisplayOverallDetails();

                ofdSelectFile.InitialDirectory = Directory.GetParent(domainPath).FullName;
            }
            else
            {
                ShowWarning("Domain / Problem files were not found.");
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            Domain m_dTmpDomain = Parser.ParseDomain(m_DomainPath, txtAgentCallsign.Text);
            Problem m_pTmpProblem = Parser.ParseProblem(m_ProblemPath, m_dTmpDomain);

            List<PartiallySpecifiedState> initialStates = GetInitialStates(m_pTmpProblem);
            List<PartiallySpecifiedState> numberOfStates = SearchAllStates(m_dTmpDomain, initialStates);
            lblStatesNumber.Text = numberOfStates.Count.ToString();
        }
        private void DisplayOverallDetails()
        {
            Domain d = Parser.ParseDomain(m_DomainPath, m_AgentCallsign);
            Problem p = Parser.ParseProblem(m_ProblemPath, d);

            if (d != null && p != null)
            {
                Text = d.Name;
                List<PartiallySpecifiedState> initialStates = GetInitialStates(p);
                lblActionsNumber.Text = d.Actions.Count.ToString();
                lblGoalsNumber.Text = p.Goal.CollectPredicates.Count.ToString();
                lblInitialStatesNumber.Text = initialStates.Count.ToString();
                lblProblemName.Text = d.Name;

                if (d.Name.StartsWith("box"))
                    ReadBoxProblem();

                ReadAgents(d, p);
                ReadGoals(d, p);
                ReadJointActions();
                ReadPrevGoalComp();
                ReadPlanners();
                chkbAlignPlan.Checked = true;
                nudMaxTime.Value = 100;
            }
            else
            {
                ShowWarning("Cannot display problem details.");
            }
        }

        private void ReadBoxProblem()
        {
            Domain d = Parser.ParseDomain(m_DomainPath, m_AgentCallsign);
            Problem p = Parser.ParseProblem(m_ProblemPath, d);

            ToText tt = new ToText();
            string drawing = tt.PrintProblem(d, p);
            rtxtDrawing.Text = drawing;
        }

        private void ReadPlanners()
        {
            cbPlanner.Items.Clear();
            foreach (var item in Enum.GetValues(typeof(SDRPlanner.Planners)))
            {
                cbPlanner.Items.Add(item);
            }
            cbPlanner.SelectedIndex = 0;
        }

        private void ReadPrevGoalComp()
        {
            clbPrevGoalTime.Items.Clear();
        }

        private void ReadJointActions()
        {
            clbActiveJointActions.Items.Clear();
        }

        private void ReadGoals(Domain d, Problem p)
        {
            List<Predicate> Goals = p.Goal.CollectPredicates;
            clbActiveGoals.Items.Clear();
            clbActiveGoals.Items.AddRange(Goals.ToArray());
            for (int i = 0; i < clbActiveGoals.Items.Count; i++)
            {
                clbActiveGoals.SetItemChecked(i, true);
            }           
        }
        private void ReadAgents(Domain d, Problem p)
        {
            List<Constant> agents = d.GetAgents();
            cbActiveAgents.Items.Clear();
            cbActiveAgents.Items.AddRange(agents.ToArray());
            cbActiveAgents.SelectedIndex = 0;
        }
        private List<PartiallySpecifiedState> GetInitialStates(Problem problem)
        {
            List<PartiallySpecifiedState> lpssInitialPossibleStates = new List<PartiallySpecifiedState>();
            BeliefState bsInitial = problem.GetInitialBelief();//, bsCurrent = bsInitial, bsNext = null;
            PartiallySpecifiedState pssInitial = bsInitial.GetPartiallySpecifiedState();

            lpssInitialPossibleStates.Add(pssInitial);

            foreach (var hiddenItems in problem.Hidden)
            {
                if (hiddenItems is CompoundFormula)
                {
                    List<PartiallySpecifiedState> stateAdditions = new List<PartiallySpecifiedState>();
                    foreach (var item in hiddenItems.Operands)
                    {
                        foreach (var pssCurrentCheckedState in lpssInitialPossibleStates)
                        {
                            PartiallySpecifiedState pssNew = pssCurrentCheckedState.Clone();
                            pssNew.AddObserved(item);
                            stateAdditions.Add(pssNew);
                        }
                    }
                    lpssInitialPossibleStates = stateAdditions;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return lpssInitialPossibleStates;
        }
        private List<PartiallySpecifiedState> SearchAllStates(Domain domain, List<PartiallySpecifiedState> lInitialStates)
        {
            Console.WriteLine("Scanning all possible states, using BFS algorithm");
            Formula fObserved = null;
            PartiallySpecifiedState fTrueObserved = null;
            PartiallySpecifiedState fFalseObserved = null;
            Stack<PartiallySpecifiedState> OpenListStack = new Stack<PartiallySpecifiedState>(lInitialStates);
            Dictionary<string, PartiallySpecifiedState> OpenList = new Dictionary<string, PartiallySpecifiedState>();
            foreach (PartiallySpecifiedState pss in OpenListStack)
            {
                OpenList.Add(pss.ToString(), pss);
            }

            Dictionary<string, PartiallySpecifiedState> ClosedList = new Dictionary<string, PartiallySpecifiedState>();
            List<IMAP.Action> groundedActions = domain.GroundAllActions(domain.Actions, domain.GroundAllPredicates(null), false, false);


            int iterationCounter = 0;
            while (OpenListStack.Count != 0)
            {
                if (iterationCounter++ % 100 == 0)
                    Console.WriteLine("Seaching: (OL=" + OpenListStack.Count + "/CL=" + ClosedList.Count + ")");
                PartiallySpecifiedState selectedState = OpenListStack.Pop();
                OpenList.Remove(selectedState.ToString());

                ClosedList.Add(selectedState.ToString(), selectedState);

                foreach (var a in groundedActions)
                {
                    selectedState.ApplyOffline(a, out fObserved, out fTrueObserved, out fFalseObserved);

                    if (fTrueObserved != null && fTrueObserved.GetPositivePredicates().Count != selectedState.GetPositivePredicates().Count)
                        continue;
                    if (fTrueObserved != null)
                    {
                        if (ClosedList.ContainsKey(fTrueObserved.ToString()) || OpenList.ContainsKey(fTrueObserved.ToString()))
                            continue;

                        OpenListStack.Push(fTrueObserved);
                        OpenList.Add(fTrueObserved.ToString(), fTrueObserved);
                    }
                }
            }
            return ClosedList.Values.ToList();
        }
        private void ShowWarning(string sErrorMessage)
        {
            MessageBox.Show(sErrorMessage);
        }

        private void btnRunSingleAgentPlan_Click(object sender, EventArgs e)
        {
            // Active agent
            Constant activeAgent = (Constant)cbActiveAgents.SelectedItem;

            // Active goals
            List<Predicate> activeGoals = new List<Predicate>();
            for (int i = 0; i < clbActiveGoals.Items.Count; i++)
            {
                if (clbActiveGoals.GetItemChecked(i))
                {
                    activeGoals.Add((Predicate)clbActiveGoals.Items[i]);
                }
            }

            // Required Actions to be preformed
            List<Action> reqCollabActions = new List<Action>();
            for (int i = 0; i < clbActiveJointActions.Items.Count; i++)
            {
                if (clbActiveJointActions.GetItemChecked(i))
                {
                    reqCollabActions.Add((Action)clbActiveJointActions.Items[i]);
                }
            }

            // Active Prev achieved goals
            List<KeyValuePair<Predicate, int>> prevAchievedGoals = new List<KeyValuePair<Predicate, int>>();
            for (int i = 0; i < clbPrevGoalTime.Items.Count; i++)
            {
                if (clbPrevGoalTime.GetItemChecked(i))
                {
                    prevAchievedGoals.Add((KeyValuePair<Predicate, int>)clbPrevGoalTime.Items[i]);
                }
            }

            Domain reducedDomain = Parser.ParseDomain(m_DomainPath, m_AgentCallsign);
            Problem reducedProblem = Parser.ParseProblem(m_ProblemPath, reducedDomain);

            string sPath = Directory.GetParent(reducedDomain.Path).FullName + "\\";

            SingleAgentSDRPlanner m_saSDRPlanner = new SingleAgentSDRPlanner(reducedDomain,
                                                                            reducedProblem,
                                                                            (int)nudMaxTime.Value,
                                                                            (SDRPlanner.Planners)cbPlanner.SelectedItem);

            PlanResult planResult = m_saSDRPlanner.Plan(activeAgent,activeGoals, prevAchievedGoals, reqCollabActions);

            ConditionalPlanTreeNode root = planResult.Plan;
            PlanDetails pd = root.ScanDetails(reducedDomain, reducedProblem);
            pd.PlanningTime = planResult.PlanningTime;
            pd.Valid = planResult.Valid;
            pd.ActiveAgent = activeAgent;
            AddResult(pd);
        }


        private void AddResult(PlanDetails pd)
        {
            
            ucExecuteResults er = new ucExecuteResults(pd);
            er.SelectedGoalAchievementTime += Er_SelectedGoalAchievementTime;
            er.SelectedJointActionReq += Er_SelectedJointActionReq;
            er.CloseTab += Er_CloseTab;
            er.AddtoFinal += Er_AddtoFinal;
            TabPage tp = new TabPage();
            tp.Text = "agent " + pd.ActiveAgent.ToString();
            tp.Controls.Add(er);
            tcResults.TabPages.Add(tp);
        }



        private void Er_AddtoFinal(object sender, EventArgs e)
        {
            ucExecuteResults uce = (ucExecuteResults)sender;
            ListViewItem lvi = new ListViewItem();
            lvi.Text = uce.PlanDetails.ActiveAgent.Name;
            lvi.SubItems.Add((uce.PlanDetails.PlanningTime.TotalMilliseconds / (double) 1000).ToString(".##"));
            lvi.SubItems.Add(uce.PlanDetails.MakeSpan.ToString());
            lvFinalPlans.Items.Add(lvi);
        }

        private void Er_CloseTab(object sender, EventArgs e)
        {
            for (int i = 0; i < tcResults.TabPages.Count; i++)
            {
                if (tcResults.TabPages[i].Controls.Contains((Control)sender))
                {
                    tcResults.TabPages.RemoveAt(i);
                    return;
                }
            }
        }
        private void Er_SelectedJointActionReq(object sender, EventArgs e)
        {
            Tuple<Action, Constant, bool> item = (Tuple<Action, Constant, bool>)sender;

            Action UpdatedActionToNextAgent = item.Item1.Clone();
            Constant PastAgent = (Constant)item.Item2;
            Constant ActiveAgent = (Constant)cbActiveAgents.SelectedItem;
            UpdatedActionToNextAgent.ChangeAgent(PastAgent, ActiveAgent);


            if (item.Item3)
            {
                // select
                if (!clbActiveJointActions.Items.Contains(item.Item1))
                {
                    clbActiveJointActions.Items.Add(UpdatedActionToNextAgent);
                }
            }
            else
            {
                // deselect
                if (clbActiveJointActions.Items.Contains(item.Item1))
                {
                    clbActiveJointActions.Items.Remove(UpdatedActionToNextAgent);
                }
            }
        }
        private void Er_SelectedGoalAchievementTime(object sender, EventArgs e)
        {
            Tuple<KeyValuePair<Predicate, int>, bool> item = (Tuple<KeyValuePair<Predicate, int>, bool>)sender;
            if (item.Item2)
            {
                // select
                if (!clbPrevGoalTime.Items.Contains(item.Item1))
                {
                    clbPrevGoalTime.Items.Add(item.Item1);
                }
            }
            else
            {
                // deselect
                if (clbPrevGoalTime.Items.Contains(item.Item1))
                {
                    clbPrevGoalTime.Items.Remove(item.Item1);
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            lvFinalPlans.Items.Clear();
        }

        private void lbBenchmarks_SelectedIndexChanged(object sender, EventArgs e)
        {
            Folder f = (Folder)((ListBox)sender).SelectedItem;
            string domainPath = f.path + "\\" + "d.pddl";
            string problemPath = f.path + "\\" + "p.pddl";
            if (File.Exists(domainPath) && File.Exists(problemPath))
            {
                txtDomainPath.Text = domainPath;
                txtProblemPath.Text = problemPath;
            }
            button3_Click(sender, e);
        }
    }
}
