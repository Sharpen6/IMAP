using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IMAP.Predicates;
using IMAP.PlanTree;

namespace IMAP.Forms
{
    public partial class ucExecuteResults : UserControl
    {
        public PlanDetails PlanDetails { get; set; }

        public event EventHandler SelectedGoalAchievementTime;
        public event EventHandler SelectedJointActionReq;

        public event EventHandler CloseTab;
        public event EventHandler AddtoFinal;
        public ucExecuteResults()
        {
            InitializeComponent();
        }
        public ucExecuteResults(PlanDetails pd) : this()
        {
            PlanDetails = pd;

            rtxtPlan.Text = PlanTreePrinter.Print(pd.Plan); ;
            lblAvgDepth.Text = pd.LeafsDepth.Average().ToString();
            lblMakespan.Text = pd.MakeSpan.ToString();
            lblValid.Text = pd.Valid.ToString();
            lblPlanningTime.Text = (pd.PlanningTime.TotalMilliseconds / (double)1000).ToString(".##") + "s";


            UpdateCollabActions(pd.JointActionsTimes);
            UpdateGoalAchievementsTime(pd.GoalsTiming);

        }

        private void UpdateGoalAchievementsTime(Dictionary<Predicate, int> goalsTiming)
        {
            clbGoalsAch.Items.Clear();
            foreach (var item in goalsTiming)
            {
                clbGoalsAch.Items.Add(item);
            }
        }

        private void UpdateCollabActions(Dictionary<Action, int> jointActionsTimes)
        {
            clbCollabActions.Items.Clear();
            foreach (var item in jointActionsTimes)
            {
                clbCollabActions.Items.Add(item.Key);
            }
        }

        private void clbGoalsAch_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            CheckedListBox selectedItem = (CheckedListBox)sender;
            KeyValuePair<Predicate, int> item = (KeyValuePair<Predicate, int>)selectedItem.SelectedItem;
            bool isSelected = e.NewValue == CheckState.Unchecked ? false : true;

            SelectedGoalAchievementTime(new Tuple<KeyValuePair<Predicate, int>, bool>(item, isSelected), e);
        }
        private void clbCollabActions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            CheckedListBox selectedItem = (CheckedListBox)sender;
            Action item = (Action)selectedItem.SelectedItem;
            bool isSelected = e.NewValue == CheckState.Unchecked ? false : true;

            SelectedJointActionReq(new Tuple<Action, Constant, bool>(item, PlanDetails.ActiveAgent, isSelected), e);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            CloseTab(this, e);
        }

        private void btnAddToFinal_Click(object sender, EventArgs e)
        {
            AddtoFinal(this, e);
        }
    }
}