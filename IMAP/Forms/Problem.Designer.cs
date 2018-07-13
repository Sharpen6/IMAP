using System.IO;

namespace IMAP.Forms
{
    partial class frmProblem
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("");
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.btnSaveResults = new System.Windows.Forms.Button();
            this.tcResults = new System.Windows.Forms.TabControl();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.txtDomainPath = new System.Windows.Forms.TextBox();
            this.txtProblemPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.button3 = new System.Windows.Forms.Button();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.label15 = new System.Windows.Forms.Label();
            this.txtAgentCallsign = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatesNumber = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblProblemName = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblInitialStatesNumber = new System.Windows.Forms.Label();
            this.lblGoalsNumber = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblActionsNumber = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.rtxtDrawing = new System.Windows.Forms.RichTextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.cbPlanner = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.cbActiveAgents = new System.Windows.Forms.ComboBox();
            this.clbActiveGoals = new System.Windows.Forms.CheckedListBox();
            this.chkbAlignPlan = new System.Windows.Forms.CheckBox();
            this.nudMaxTime = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.clbPrevGoalTime = new System.Windows.Forms.CheckedListBox();
            this.label10 = new System.Windows.Forms.Label();
            this.clbActiveJointActions = new System.Windows.Forms.CheckedListBox();
            this.btnRunSingleAgentPlan = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.lvFinalPlans = new System.Windows.Forms.ListView();
            this.Agent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PlanningTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Makespan = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnCheckOverallPlan = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.lblMakespan = new System.Windows.Forms.Label();
            this.lblIterations = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.lblDepth = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.lblOverallTime = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.ofdSelectFile = new System.Windows.Forms.OpenFileDialog();
            this.lblValid = new System.Windows.Forms.Label();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.lbBenchmarks = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxTime)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel6, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.groupBox3, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox5, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1250, 720);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.AutoSize = true;
            this.groupBox4.Controls.Add(this.tableLayoutPanel5);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox4.Location = new System.Drawing.Point(3, 370);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(619, 395);
            this.groupBox4.TabIndex = 5;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Plans";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.AutoSize = true;
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Controls.Add(this.btnSaveResults, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.tcResults, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(613, 376);
            this.tableLayoutPanel5.TabIndex = 1;
            // 
            // btnSaveResults
            // 
            this.btnSaveResults.AutoSize = true;
            this.btnSaveResults.Location = new System.Drawing.Point(3, 349);
            this.btnSaveResults.Name = "btnSaveResults";
            this.btnSaveResults.Size = new System.Drawing.Size(80, 23);
            this.btnSaveResults.TabIndex = 16;
            this.btnSaveResults.Text = "Save Results";
            this.btnSaveResults.UseVisualStyleBackColor = true;
            // 
            // tcResults
            // 
            this.tcResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcResults.Location = new System.Drawing.Point(3, 3);
            this.tcResults.Name = "tcResults";
            this.tcResults.SelectedIndex = 0;
            this.tcResults.Size = new System.Drawing.Size(607, 340);
            this.tcResults.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(619, 231);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Path";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.txtDomainPath, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtProblemPath, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.button1, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.button2, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel7, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.groupBox6, 3, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(613, 212);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // txtDomainPath
            // 
            this.txtDomainPath.Location = new System.Drawing.Point(75, 3);
            this.txtDomainPath.Name = "txtDomainPath";
            this.txtDomainPath.Size = new System.Drawing.Size(376, 20);
            this.txtDomainPath.TabIndex = 0;
            // 
            // txtProblemPath
            // 
            this.txtProblemPath.Location = new System.Drawing.Point(75, 32);
            this.txtProblemPath.Name = "txtProblemPath";
            this.txtProblemPath.Size = new System.Drawing.Size(376, 20);
            this.txtProblemPath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Domain";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Problem";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(516, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(516, 32);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(23, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "...";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 2;
            this.tableLayoutPanel2.SetColumnSpan(this.tableLayoutPanel7, 4);
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40.46511F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 59.53489F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Controls.Add(this.button3, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.tableLayoutPanel8, 0, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(3, 109);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(607, 100);
            this.tableLayoutPanel7.TabIndex = 7;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(248, 3);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(356, 23);
            this.button3.TabIndex = 6;
            this.button3.Text = "Load";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.ColumnCount = 2;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Controls.Add(this.label15, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.txtAgentCallsign, 1, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(239, 94);
            this.tableLayoutPanel8.TabIndex = 7;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(3, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(77, 13);
            this.label15.TabIndex = 0;
            this.label15.Text = "Agent Callsign:";
            // 
            // txtAgentCallsign
            // 
            this.txtAgentCallsign.Location = new System.Drawing.Point(103, 3);
            this.txtAgentCallsign.Name = "txtAgentCallsign";
            this.txtAgentCallsign.Size = new System.Drawing.Size(133, 20);
            this.txtAgentCallsign.TabIndex = 1;
            this.txtAgentCallsign.Text = "agent";
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.AutoSize = true;
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 304F));
            this.tableLayoutPanel6.Controls.Add(this.groupBox2, 1, 0);
            this.tableLayoutPanel6.Controls.Add(this.rtxtDrawing, 0, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(3, 240);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel6.Size = new System.Drawing.Size(619, 124);
            this.tableLayoutPanel6.TabIndex = 7;
            // 
            // groupBox2
            // 
            this.groupBox2.AutoSize = true;
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(318, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(298, 118);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "General Details";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.Controls.Add(this.lblStatesNumber, 1, 4);
            this.tableLayoutPanel3.Controls.Add(this.label7, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.lblProblemName, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.lblInitialStatesNumber, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.lblGoalsNumber, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.lblActionsNumber, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.button4, 2, 4);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 5;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(292, 99);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // lblStatesNumber
            // 
            this.lblStatesNumber.AutoSize = true;
            this.lblStatesNumber.Location = new System.Drawing.Point(100, 68);
            this.lblStatesNumber.Name = "lblStatesNumber";
            this.lblStatesNumber.Size = new System.Drawing.Size(10, 13);
            this.lblStatesNumber.TabIndex = 9;
            this.lblStatesNumber.Text = "-";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 68);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(37, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "States";
            // 
            // lblProblemName
            // 
            this.lblProblemName.AutoSize = true;
            this.lblProblemName.Location = new System.Drawing.Point(100, 0);
            this.lblProblemName.Name = "lblProblemName";
            this.lblProblemName.Size = new System.Drawing.Size(10, 13);
            this.lblProblemName.TabIndex = 7;
            this.lblProblemName.Text = "-";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(35, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 51);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Initial States";
            // 
            // lblInitialStatesNumber
            // 
            this.lblInitialStatesNumber.AutoSize = true;
            this.lblInitialStatesNumber.Location = new System.Drawing.Point(100, 51);
            this.lblInitialStatesNumber.Name = "lblInitialStatesNumber";
            this.lblInitialStatesNumber.Size = new System.Drawing.Size(10, 13);
            this.lblInitialStatesNumber.TabIndex = 5;
            this.lblInitialStatesNumber.Text = "-";
            // 
            // lblGoalsNumber
            // 
            this.lblGoalsNumber.AutoSize = true;
            this.lblGoalsNumber.Location = new System.Drawing.Point(100, 34);
            this.lblGoalsNumber.Name = "lblGoalsNumber";
            this.lblGoalsNumber.Size = new System.Drawing.Size(10, 13);
            this.lblGoalsNumber.TabIndex = 4;
            this.lblGoalsNumber.Text = "-";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 34);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Goals";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 17);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Actions";
            // 
            // lblActionsNumber
            // 
            this.lblActionsNumber.AutoSize = true;
            this.lblActionsNumber.Location = new System.Drawing.Point(100, 17);
            this.lblActionsNumber.Name = "lblActionsNumber";
            this.lblActionsNumber.Size = new System.Drawing.Size(10, 13);
            this.lblActionsNumber.TabIndex = 3;
            this.lblActionsNumber.Text = "-";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(197, 71);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(71, 23);
            this.button4.TabIndex = 10;
            this.button4.Text = "Calc (long)";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // rtxtDrawing
            // 
            this.rtxtDrawing.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtxtDrawing.Font = new System.Drawing.Font("Miriam Fixed", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtxtDrawing.Location = new System.Drawing.Point(3, 3);
            this.rtxtDrawing.Name = "rtxtDrawing";
            this.rtxtDrawing.Size = new System.Drawing.Size(309, 118);
            this.rtxtDrawing.TabIndex = 6;
            this.rtxtDrawing.Text = "";
            // 
            // groupBox3
            // 
            this.groupBox3.AutoSize = true;
            this.groupBox3.Controls.Add(this.tableLayoutPanel4);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(628, 3);
            this.groupBox3.Name = "groupBox3";
            this.tableLayoutPanel1.SetRowSpan(this.groupBox3, 2);
            this.groupBox3.Size = new System.Drawing.Size(619, 361);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Execution Details:";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.AutoSize = true;
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.Controls.Add(this.cbPlanner, 1, 6);
            this.tableLayoutPanel4.Controls.Add(this.label14, 0, 6);
            this.tableLayoutPanel4.Controls.Add(this.label13, 0, 5);
            this.tableLayoutPanel4.Controls.Add(this.label12, 0, 4);
            this.tableLayoutPanel4.Controls.Add(this.label9, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.cbActiveAgents, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.clbActiveGoals, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.chkbAlignPlan, 1, 4);
            this.tableLayoutPanel4.Controls.Add(this.nudMaxTime, 1, 5);
            this.tableLayoutPanel4.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.clbPrevGoalTime, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.label10, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.clbActiveJointActions, 1, 3);
            this.tableLayoutPanel4.Controls.Add(this.btnRunSingleAgentPlan, 1, 7);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 8;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(613, 342);
            this.tableLayoutPanel4.TabIndex = 3;
            // 
            // cbPlanner
            // 
            this.cbPlanner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPlanner.FormattingEnabled = true;
            this.cbPlanner.Items.AddRange(new object[] {
            "a1",
            "a2",
            "a3"});
            this.cbPlanner.Location = new System.Drawing.Point(203, 241);
            this.cbPlanner.Name = "cbPlanner";
            this.cbPlanner.Size = new System.Drawing.Size(267, 21);
            this.cbPlanner.TabIndex = 14;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 238);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(46, 13);
            this.label14.TabIndex = 13;
            this.label14.Text = "Planner:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(3, 212);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(56, 13);
            this.label13.TabIndex = 11;
            this.label13.Text = "Max Time:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 192);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(57, 13);
            this.label12.TabIndex = 9;
            this.label12.Text = "Align Plan:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 27);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 13);
            this.label9.TabIndex = 2;
            this.label9.Text = "Active Goals:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "Active Agent:";
            // 
            // cbActiveAgents
            // 
            this.cbActiveAgents.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbActiveAgents.FormattingEnabled = true;
            this.cbActiveAgents.Items.AddRange(new object[] {
            "a1",
            "a2",
            "a3"});
            this.cbActiveAgents.Location = new System.Drawing.Point(203, 3);
            this.cbActiveAgents.Name = "cbActiveAgents";
            this.cbActiveAgents.Size = new System.Drawing.Size(267, 21);
            this.cbActiveAgents.TabIndex = 1;
            // 
            // clbActiveGoals
            // 
            this.clbActiveGoals.CheckOnClick = true;
            this.clbActiveGoals.FormattingEnabled = true;
            this.clbActiveGoals.Items.AddRange(new object[] {
            "g1",
            "g2",
            "g3"});
            this.clbActiveGoals.Location = new System.Drawing.Point(203, 30);
            this.clbActiveGoals.Name = "clbActiveGoals";
            this.clbActiveGoals.Size = new System.Drawing.Size(266, 49);
            this.clbActiveGoals.TabIndex = 4;
            // 
            // chkbAlignPlan
            // 
            this.chkbAlignPlan.AutoSize = true;
            this.chkbAlignPlan.Checked = true;
            this.chkbAlignPlan.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkbAlignPlan.Location = new System.Drawing.Point(203, 195);
            this.chkbAlignPlan.Name = "chkbAlignPlan";
            this.chkbAlignPlan.Size = new System.Drawing.Size(15, 14);
            this.chkbAlignPlan.TabIndex = 10;
            this.chkbAlignPlan.UseVisualStyleBackColor = true;
            // 
            // nudMaxTime
            // 
            this.nudMaxTime.Location = new System.Drawing.Point(203, 215);
            this.nudMaxTime.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
            this.nudMaxTime.Name = "nudMaxTime";
            this.nudMaxTime.Size = new System.Drawing.Size(266, 20);
            this.nudMaxTime.TabIndex = 12;
            this.nudMaxTime.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(3, 82);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(138, 13);
            this.label11.TabIndex = 7;
            this.label11.Text = "Prec Goal Completion Time:";
            // 
            // clbPrevGoalTime
            // 
            this.clbPrevGoalTime.CheckOnClick = true;
            this.clbPrevGoalTime.FormattingEnabled = true;
            this.clbPrevGoalTime.Items.AddRange(new object[] {
            "ja1",
            "ja2"});
            this.clbPrevGoalTime.Location = new System.Drawing.Point(203, 85);
            this.clbPrevGoalTime.Name = "clbPrevGoalTime";
            this.clbPrevGoalTime.Size = new System.Drawing.Size(266, 49);
            this.clbPrevGoalTime.TabIndex = 8;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 137);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(120, 13);
            this.label10.TabIndex = 5;
            this.label10.Text = "Prec Collab Actions req:";
            // 
            // clbActiveJointActions
            // 
            this.clbActiveJointActions.CheckOnClick = true;
            this.clbActiveJointActions.FormattingEnabled = true;
            this.clbActiveJointActions.Items.AddRange(new object[] {
            "ja1",
            "ja2"});
            this.clbActiveJointActions.Location = new System.Drawing.Point(203, 140);
            this.clbActiveJointActions.Name = "clbActiveJointActions";
            this.clbActiveJointActions.Size = new System.Drawing.Size(266, 49);
            this.clbActiveJointActions.TabIndex = 6;
            // 
            // btnRunSingleAgentPlan
            // 
            this.btnRunSingleAgentPlan.AutoSize = true;
            this.btnRunSingleAgentPlan.Location = new System.Drawing.Point(203, 268);
            this.btnRunSingleAgentPlan.Name = "btnRunSingleAgentPlan";
            this.btnRunSingleAgentPlan.Size = new System.Drawing.Size(267, 23);
            this.btnRunSingleAgentPlan.TabIndex = 15;
            this.btnRunSingleAgentPlan.Text = "Execute";
            this.btnRunSingleAgentPlan.UseVisualStyleBackColor = true;
            this.btnRunSingleAgentPlan.Click += new System.EventHandler(this.btnRunSingleAgentPlan_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.tableLayoutPanel9);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox5.Location = new System.Drawing.Point(628, 370);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(619, 395);
            this.groupBox5.TabIndex = 8;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Final Plan";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.AutoSize = true;
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.Controls.Add(this.btnCheckOverallPlan, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.label16, 0, 2);
            this.tableLayoutPanel9.Controls.Add(this.label17, 0, 3);
            this.tableLayoutPanel9.Controls.Add(this.lblMakespan, 1, 3);
            this.tableLayoutPanel9.Controls.Add(this.lblIterations, 1, 5);
            this.tableLayoutPanel9.Controls.Add(this.label18, 0, 5);
            this.tableLayoutPanel9.Controls.Add(this.lblDepth, 1, 4);
            this.tableLayoutPanel9.Controls.Add(this.label19, 0, 4);
            this.tableLayoutPanel9.Controls.Add(this.lblOverallTime, 1, 6);
            this.tableLayoutPanel9.Controls.Add(this.label20, 0, 6);
            this.tableLayoutPanel9.Controls.Add(this.lvFinalPlans, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.button5, 1, 1);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 7;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(613, 376);
            this.tableLayoutPanel9.TabIndex = 1;
            // 
            // lvFinalPlans
            // 
            this.lvFinalPlans.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Agent,
            this.PlanningTime,
            this.Makespan});
            this.tableLayoutPanel9.SetColumnSpan(this.lvFinalPlans, 2);
            this.lvFinalPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFinalPlans.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem2});
            this.lvFinalPlans.Location = new System.Drawing.Point(3, 3);
            this.lvFinalPlans.Name = "lvFinalPlans";
            this.lvFinalPlans.Size = new System.Drawing.Size(607, 241);
            this.lvFinalPlans.TabIndex = 0;
            this.lvFinalPlans.UseCompatibleStateImageBehavior = false;
            this.lvFinalPlans.View = System.Windows.Forms.View.Details;
            // 
            // Agent
            // 
            this.Agent.Text = "Agent";
            this.Agent.Width = 100;
            // 
            // PlanningTime
            // 
            this.PlanningTime.Text = "Planning Time";
            this.PlanningTime.Width = 100;
            // 
            // Makespan
            // 
            this.Makespan.Text = "Makespan";
            this.Makespan.Width = 100;
            // 
            // btnCheckOverallPlan
            // 
            this.btnCheckOverallPlan.Location = new System.Drawing.Point(3, 250);
            this.btnCheckOverallPlan.Name = "btnCheckOverallPlan";
            this.btnCheckOverallPlan.Size = new System.Drawing.Size(75, 23);
            this.btnCheckOverallPlan.TabIndex = 1;
            this.btnCheckOverallPlan.Text = "Check Plan";
            this.btnCheckOverallPlan.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(3, 276);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(30, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Valid";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(3, 296);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(57, 13);
            this.label17.TabIndex = 4;
            this.label17.Text = "Makespan";
            // 
            // lblMakespan
            // 
            this.lblMakespan.AutoSize = true;
            this.lblMakespan.Location = new System.Drawing.Point(103, 296);
            this.lblMakespan.Name = "lblMakespan";
            this.lblMakespan.Size = new System.Drawing.Size(10, 13);
            this.lblMakespan.TabIndex = 5;
            this.lblMakespan.Text = "-";
            // 
            // lblIterations
            // 
            this.lblIterations.AutoSize = true;
            this.lblIterations.Location = new System.Drawing.Point(103, 336);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(10, 13);
            this.lblIterations.TabIndex = 9;
            this.lblIterations.Text = "-";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(3, 336);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(50, 13);
            this.label18.TabIndex = 8;
            this.label18.Text = "Iterations";
            // 
            // lblDepth
            // 
            this.lblDepth.AutoSize = true;
            this.lblDepth.Location = new System.Drawing.Point(103, 316);
            this.lblDepth.Name = "lblDepth";
            this.lblDepth.Size = new System.Drawing.Size(10, 13);
            this.lblDepth.TabIndex = 7;
            this.lblDepth.Text = "-";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(3, 316);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(58, 13);
            this.label19.TabIndex = 6;
            this.label19.Text = "Avg.Depth";
            // 
            // lblOverallTime
            // 
            this.lblOverallTime.AutoSize = true;
            this.lblOverallTime.Location = new System.Drawing.Point(103, 356);
            this.lblOverallTime.Name = "lblOverallTime";
            this.lblOverallTime.Size = new System.Drawing.Size(10, 13);
            this.lblOverallTime.TabIndex = 11;
            this.lblOverallTime.Text = "-";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(3, 356);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(74, 13);
            this.label20.TabIndex = 10;
            this.label20.Text = "Planning Time";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(103, 250);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(75, 23);
            this.button5.TabIndex = 12;
            this.button5.Text = "Clear";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // ofdSelectFile
            // 
            this.ofdSelectFile.Filter = "PDDL files|*.pddl|All files|*.*";
            // 
            // lblValid
            // 
            this.lblValid.AutoSize = true;
            this.lblValid.Location = new System.Drawing.Point(221, 132);
            this.lblValid.Name = "lblValid";
            this.lblValid.Size = new System.Drawing.Size(10, 13);
            this.lblValid.TabIndex = 3;
            this.lblValid.Text = "-";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.lbBenchmarks);
            this.groupBox6.Location = new System.Drawing.Point(545, 3);
            this.groupBox6.Name = "groupBox6";
            this.tableLayoutPanel2.SetRowSpan(this.groupBox6, 2);
            this.groupBox6.Size = new System.Drawing.Size(65, 100);
            this.groupBox6.TabIndex = 8;
            this.groupBox6.TabStop = false;
            // 
            // lbBenchmarks
            // 
            this.lbBenchmarks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbBenchmarks.FormattingEnabled = true;
            this.lbBenchmarks.Location = new System.Drawing.Point(3, 16);
            this.lbBenchmarks.Name = "lbBenchmarks";
            this.lbBenchmarks.Size = new System.Drawing.Size(59, 81);
            this.lbBenchmarks.TabIndex = 0;
            this.lbBenchmarks.SelectedIndexChanged += new System.EventHandler(this.lbBenchmarks_SelectedIndexChanged);
            // 
            // frmProblem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1250, 720);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "frmProblem";
            this.Text = "Problem";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel8.PerformLayout();
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxTime)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TextBox txtDomainPath;
        private System.Windows.Forms.TextBox txtProblemPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.OpenFileDialog ofdSelectFile;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label lblInitialStatesNumber;
        private System.Windows.Forms.Label lblGoalsNumber;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblActionsNumber;
        private System.Windows.Forms.Label lblStatesNumber;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblProblemName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cbActiveAgents;
        private System.Windows.Forms.ComboBox cbPlanner;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckedListBox clbActiveGoals;
        private System.Windows.Forms.CheckBox chkbAlignPlan;
        private System.Windows.Forms.NumericUpDown nudMaxTime;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckedListBox clbPrevGoalTime;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckedListBox clbActiveJointActions;
        private System.Windows.Forms.Button btnRunSingleAgentPlan;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnSaveResults;
        private System.Windows.Forms.TabControl tcResults;
        private System.Windows.Forms.RichTextBox rtxtDrawing;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtAgentCallsign;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.Button btnCheckOverallPlan;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label lblValid;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label lblMakespan;
        private System.Windows.Forms.Label lblIterations;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label lblDepth;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label lblOverallTime;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ListView lvFinalPlans;
        private System.Windows.Forms.ColumnHeader Agent;
        private System.Windows.Forms.ColumnHeader PlanningTime;
        private System.Windows.Forms.ColumnHeader Makespan;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.ListBox lbBenchmarks;
    }
}