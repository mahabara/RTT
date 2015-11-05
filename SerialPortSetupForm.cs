using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace RTT
{
    public partial class SerialPortSetupForm : Form
    {
        public string port_rru = "";
        public string baudrate_rru = "";
        public string parity_rru = "";
        public string stopbits_rru = "";
        public string databits_rru = "";

        public string port_2 = "";
        public string baudrate_2 = "";
        public string parity_2 = "";
        public string stopbits_2 = "";
        public string databits_2 = "";

        //private DataTable ZLDT = new DataTable();
        public SerialPortSetupForm(Links link)
        {
            InitializeComponent();
            if(link!=null)
            {
                if (link.port1_hasset)
                {
                    this.port_rru = link.port_rru;
                    if (this.comboBox1.Items.Contains(this.port_rru))
                        this.comboBox1.SelectedText = this.port_rru;
                    this.baudrate_rru = link.baudrate_rru;
                    this.parity_rru = link.parity_rru;
                    this.databits_rru = link.databits_rru;
                    this.stopbits_rru = link.stopbits_rru;
                }

                if (link.port2_hasset)
                {
                    this.port_2 = link.port_2;
                    this.baudrate_2 = link.baudrate_2;
                    this.parity_2 = link.parity_2;
                    this.databits_2 = link.databits_2;
                    this.stopbits_2 = link.stopbits_2;
                }
            }
            
            
            
            

            
            

        }

        

        

        
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SerialPortSetupForm_Load(object sender, EventArgs e)
        {
            string[] portlist = SerialPort.GetPortNames();

            for (int i = 0; i < portlist.Length; i++)

            {

                this.comboBox1.Items.Add(portlist[i]);
                this.comboBox10.Items.Add(portlist[i]);

            }

            for(int i=0; i!= Constant.BAUD_RATES.Length; i++)
            {
                this.comboBox2.Items.Add(Constant.BAUD_RATES[i]);
                this.comboBox9.Items.Add(Constant.BAUD_RATES[i]);
            }

            for (int i = 0; i != Constant.PARITY.Length; i++)
            {
                this.comboBox3.Items.Add(Constant.PARITY[i]);
                this.comboBox8.Items.Add(Constant.PARITY[i]);
            }

            for (int i = 0; i != Constant.DATA_BITS.Length; i++)
            {
                this.comboBox4.Items.Add(Constant.DATA_BITS[i]);
                this.comboBox7.Items.Add(Constant.DATA_BITS[i]);
            }

            for (int i = 0; i != Constant.STOP_BITS.Length; i++)
            {
                this.comboBox5.Items.Add(Constant.STOP_BITS[i]);
                this.comboBox6.Items.Add(Constant.STOP_BITS[i]);
            }

            if (this.port_rru!="")
            {
                this.comboBox1.SelectedText = this.port_rru;
                this.comboBox2.SelectedText = this.baudrate_rru;
                this.comboBox3.SelectedText = this.parity_rru;
                this.comboBox4.SelectedText = this.databits_rru;
                this.comboBox5.SelectedText = this.stopbits_rru;
            }
            else
            {

                this.comboBox2.SelectedIndex = 1;
                this.comboBox3.SelectedIndex = 0;
                this.comboBox4.SelectedIndex = 4;
                this.comboBox5.SelectedIndex = 0;
            }
            if (this.port_2 != "")
            {
                this.comboBox10.SelectedText = this.port_2;
                this.comboBox9.SelectedText = this.baudrate_2;
                this.comboBox8.SelectedText = this.parity_2;
                this.comboBox7.SelectedText = this.databits_2;
                this.comboBox6.SelectedText = this.stopbits_2;
            }
            else
            {
                
                this.comboBox9.SelectedIndex = 1;
                this.comboBox8.SelectedIndex = 0;
                this.comboBox7.SelectedIndex = 4;
                this.comboBox6.SelectedIndex = 0;
            }
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem!=null)
            {
                this.port_rru = comboBox1.Text;
                this.baudrate_rru = comboBox2.Text;
                this.parity_rru = comboBox3.Text;
                this.databits_rru = comboBox4.Text;
                this.stopbits_rru = comboBox5.Text;
            }

            if (comboBox10.SelectedItem != null)
            {
                this.port_2 = comboBox10.Text;
                this.baudrate_2 = comboBox9.Text;
                this.parity_2 = comboBox8.Text;
                this.databits_2 = comboBox7.Text;
                this.stopbits_2 = comboBox6.Text;
            }
                

            this.DialogResult = DialogResult.OK;
            this.Close();
            
        }
    }
}
