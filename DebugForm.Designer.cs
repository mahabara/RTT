﻿namespace RTT
{
    partial class DebugForm
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
            this.radiovisacom = new System.Windows.Forms.RadioButton();
            this.radiovisa32 = new System.Windows.Forms.RadioButton();
            this.button_ok = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // radiovisacom
            // 
            this.radiovisacom.AutoSize = true;
            this.radiovisacom.Location = new System.Drawing.Point(6, 19);
            this.radiovisacom.Name = "radiovisacom";
            this.radiovisacom.Size = new System.Drawing.Size(64, 17);
            this.radiovisacom.TabIndex = 0;
            this.radiovisacom.TabStop = true;
            this.radiovisacom.Text = "visacom";
            this.radiovisacom.UseVisualStyleBackColor = true;
            this.radiovisacom.CheckedChanged += new System.EventHandler(this.radiovisacom_CheckedChanged);
            // 
            // radiovisa32
            // 
            this.radiovisa32.AutoSize = true;
            this.radiovisa32.Location = new System.Drawing.Point(76, 19);
            this.radiovisa32.Name = "radiovisa32";
            this.radiovisa32.Size = new System.Drawing.Size(56, 17);
            this.radiovisa32.TabIndex = 1;
            this.radiovisa32.TabStop = true;
            this.radiovisa32.Text = "visa32";
            this.radiovisa32.UseVisualStyleBackColor = true;
            this.radiovisa32.CheckedChanged += new System.EventHandler(this.radiovisa32_CheckedChanged);
            // 
            // button_ok
            // 
            this.button_ok.Location = new System.Drawing.Point(123, 227);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(75, 23);
            this.button_ok.TabIndex = 2;
            this.button_ok.Text = "OK";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.Location = new System.Drawing.Point(204, 227);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 3;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radiovisacom);
            this.groupBox1.Controls.Add(this.radiovisa32);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(144, 49);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "visa select";
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DebugForm";
            this.Text = "DebugForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton radiovisacom;
        private System.Windows.Forms.RadioButton radiovisa32;
        private System.Windows.Forms.Button button_ok;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}