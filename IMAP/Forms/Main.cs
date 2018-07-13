using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMAP.Forms
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();


            frmProblem problemWin = new frmProblem();
            problemWin.MdiParent = this;
            problemWin.Show();

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmProblem problemWin = new frmProblem();
            problemWin.MdiParent = this;
            problemWin.Show();
        }
    }
}
