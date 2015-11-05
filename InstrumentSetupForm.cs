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
    public partial class InstrumentSetupForm : Form
    {
        Address addr = new Address();
        public InstrumentSetupForm(Address addr)
        {
            InitializeComponent();
            if(addr!=null)
            {
                this.addr = addr;
                this.textBox1.Text = this.addr.SA;
                this.textBox2.Text = this.addr.SG;
                this.textBox3.Text = this.addr.SG2;
                this.textBox4.Text = this.addr.RFBOX;
                this.textBox5.Text = this.addr.RFBOX2;
                this.textBox6.Text = this.addr.IS1;
                this.textBox7.Text = this.addr.IS2;
                this.textBox8.Text = this.addr.RUMASTER;
                this.textBox9.Text = this.addr.DC5767A;

            }
        }

        private void InstrumentSetupForm_Load(object sender, EventArgs e)
        {
            
        }
        private void button_cancel_Click(object sender, EventArgs e)
        {

            this.Close();
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            if(this.textBox1.Text!="")
            {
                this.addr.SA = this.textBox1.Text;
            }
            if (this.textBox2.Text != "")
            {
                this.addr.SG = this.textBox2.Text;
            }
            if (this.textBox3.Text != "")
            {
                this.addr.SG2 = this.textBox3.Text;
            }
            if (this.textBox4.Text != "")
            {
                this.addr.RFBOX = this.textBox4.Text;
            }
            if (this.textBox5.Text != "")
            {
                this.addr.RFBOX2 = this.textBox5.Text;
            }
            if (this.textBox6.Text != "")
            {
                this.addr.IS1 = this.textBox6.Text;
            }
            if (this.textBox7.Text != "")
            {
                this.addr.IS2 = this.textBox7.Text;
            }
            if (this.textBox8.Text != "")
            {
                this.addr.RUMASTER = this.textBox8.Text;
            }
            if (this.textBox9.Text != "")
            {
                this.addr.DC5767A = this.textBox9.Text;
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
