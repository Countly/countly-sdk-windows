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
            this.btnBeginSession = new System.Windows.Forms.Button();
            this.btnEndSession = new System.Windows.Forms.Button();
            this.btnEventSimple = new System.Windows.Forms.Button();
            this.btnCrash = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnBeginSession
            // 
            this.btnBeginSession.Location = new System.Drawing.Point(12, 12);
            this.btnBeginSession.Name = "btnBeginSession";
            this.btnBeginSession.Size = new System.Drawing.Size(85, 38);
            this.btnBeginSession.TabIndex = 1;
            this.btnBeginSession.Text = "BeginSession";
            this.btnBeginSession.UseVisualStyleBackColor = true;
            this.btnBeginSession.Click += new System.EventHandler(this.btnBeginSession_Click);
            // 
            // btnEndSession
            // 
            this.btnEndSession.Location = new System.Drawing.Point(103, 12);
            this.btnEndSession.Name = "btnEndSession";
            this.btnEndSession.Size = new System.Drawing.Size(85, 38);
            this.btnEndSession.TabIndex = 2;
            this.btnEndSession.Text = "EndSession";
            this.btnEndSession.UseVisualStyleBackColor = true;
            this.btnEndSession.Click += new System.EventHandler(this.btnEndSession_Click);
            // 
            // btnEventSimple
            // 
            this.btnEventSimple.Location = new System.Drawing.Point(12, 72);
            this.btnEventSimple.Name = "btnEventSimple";
            this.btnEventSimple.Size = new System.Drawing.Size(85, 38);
            this.btnEventSimple.TabIndex = 3;
            this.btnEventSimple.Text = "Send Event";
            this.btnEventSimple.UseVisualStyleBackColor = true;
            this.btnEventSimple.Click += new System.EventHandler(this.btnEventSimple_Click);
            // 
            // btnCrash
            // 
            this.btnCrash.Location = new System.Drawing.Point(103, 72);
            this.btnCrash.Name = "btnCrash";
            this.btnCrash.Size = new System.Drawing.Size(85, 38);
            this.btnCrash.TabIndex = 4;
            this.btnCrash.Text = "Send Crash";
            this.btnCrash.UseVisualStyleBackColor = true;
            this.btnCrash.Click += new System.EventHandler(this.btnCrash_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 472);
            this.Controls.Add(this.btnCrash);
            this.Controls.Add(this.btnEventSimple);
            this.Controls.Add(this.btnEndSession);
            this.Controls.Add(this.btnBeginSession);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnBeginSession;
        private System.Windows.Forms.Button btnEndSession;
        private System.Windows.Forms.Button btnEventSimple;
        private System.Windows.Forms.Button btnCrash;
    }
}

