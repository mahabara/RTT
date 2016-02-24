using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTT
{
    public partial class DebugForm : Form
    {
        public string visastatus = "visa32";
        public DebugForm(bool status)
        {
            InitializeComponent();
            if (status)
                //visastatus = "visa32";
                radiovisa32.Select();
            else
                //visastatus = "visacom";
                radiovisacom.Select();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void radiovisacom_CheckedChanged(object sender, EventArgs e)
        {
            this.visastatus = radiovisacom.Text;
        }

        private void radiovisa32_CheckedChanged(object sender, EventArgs e)
        {
            this.visastatus = radiovisa32.Text;
        }
    }
}
