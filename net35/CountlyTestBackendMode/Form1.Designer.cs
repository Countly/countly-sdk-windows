namespace CountlySampleWindowsForm
{
    partial class Form1
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
            if (disposing && (components != null)) {
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
            this.btnSend = new System.Windows.Forms.Button();
            this.appCount = new System.Windows.Forms.TextBox();
            this.deviceCount = new System.Windows.Forms.TextBox();
            this.eventCount = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(251, 222);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(85, 38);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "Send Events";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // appCount
            // 
            this.appCount.Location = new System.Drawing.Point(251, 94);
            this.appCount.Name = "appCount";
            this.appCount.Size = new System.Drawing.Size(100, 20);
            this.appCount.TabIndex = 2;
            this.appCount.Text = "3";
            // 
            // deviceCount
            // 
            this.deviceCount.Location = new System.Drawing.Point(251, 130);
            this.deviceCount.Name = "deviceCount";
            this.deviceCount.Size = new System.Drawing.Size(100, 20);
            this.deviceCount.TabIndex = 3;
            this.deviceCount.Text = "10";
            // 
            // eventCount
            // 
            this.eventCount.Location = new System.Drawing.Point(251, 173);
            this.eventCount.Name = "eventCount";
            this.eventCount.Size = new System.Drawing.Size(100, 20);
            this.eventCount.TabIndex = 4;
            this.eventCount.Text = "1000";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(182, 94);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "App Count";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(173, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Device Count";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(182, 173);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Event Count";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 472);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.eventCount);
            this.Controls.Add(this.deviceCount);
            this.Controls.Add(this.appCount);
            this.Controls.Add(this.btnSend);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox appCount;
        private System.Windows.Forms.TextBox deviceCount;
        private System.Windows.Forms.TextBox eventCount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

