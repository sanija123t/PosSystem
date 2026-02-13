namespace PosSystem
{
    partial class frmConfirmPaasword
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtAdminUser = new MetroFramework.Controls.MetroTextBox();
            this.txtAdminPass = new MetroFramework.Controls.MetroTextBox();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(158)))), ((int)(((byte)(132)))));
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(-2, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(340, 38);
            this.panel1.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(4, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "Change Password";
            // 
            // txtAdminUser
            // 
            // 
            // 
            // 
            this.txtAdminUser.CustomButton.Image = null;
            this.txtAdminUser.CustomButton.Location = new System.Drawing.Point(232, 1);
            this.txtAdminUser.CustomButton.Name = "";
            this.txtAdminUser.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtAdminUser.CustomButton.Style = MetroFramework.MetroColorStyle.Blue;
            this.txtAdminUser.CustomButton.TabIndex = 1;
            this.txtAdminUser.CustomButton.Theme = MetroFramework.MetroThemeStyle.Light;
            this.txtAdminUser.CustomButton.UseSelectable = true;
            this.txtAdminUser.CustomButton.Visible = false;
            this.txtAdminUser.Lines = new string[0];
            this.txtAdminUser.Location = new System.Drawing.Point(25, 54);
            this.txtAdminUser.MaxLength = 32767;
            this.txtAdminUser.Name = "txtAdminUser";
            this.txtAdminUser.PasswordChar = '\0';
            this.txtAdminUser.WaterMark = "User Name Here !!";
            this.txtAdminUser.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtAdminUser.SelectedText = "";
            this.txtAdminUser.SelectionLength = 0;
            this.txtAdminUser.SelectionStart = 0;
            this.txtAdminUser.ShortcutsEnabled = true;
            this.txtAdminUser.Size = new System.Drawing.Size(254, 23);
            this.txtAdminUser.TabIndex = 3;
            this.txtAdminUser.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtAdminUser.UseSelectable = true;
            this.txtAdminUser.WaterMark = "User Name Here !!";
            this.txtAdminUser.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtAdminUser.WaterMarkFont = new System.Drawing.Font("Georgia", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAdminUser.Click += new System.EventHandler(this.txtAdminUser_Click);
            // 
            // txtAdminPass
            // 
            // 
            // 
            // 
            this.txtAdminPass.CustomButton.Image = null;
            this.txtAdminPass.CustomButton.Location = new System.Drawing.Point(232, 1);
            this.txtAdminPass.CustomButton.Name = "";
            this.txtAdminPass.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtAdminPass.CustomButton.Style = MetroFramework.MetroColorStyle.Blue;
            this.txtAdminPass.CustomButton.TabIndex = 1;
            this.txtAdminPass.CustomButton.Theme = MetroFramework.MetroThemeStyle.Light;
            this.txtAdminPass.CustomButton.UseSelectable = true;
            this.txtAdminPass.CustomButton.Visible = false;
            this.txtAdminPass.Lines = new string[0];
            this.txtAdminPass.Location = new System.Drawing.Point(25, 92);
            this.txtAdminPass.MaxLength = 32767;
            this.txtAdminPass.Name = "txtAdminPass";
            this.txtAdminPass.PasswordChar = '*';
            this.txtAdminPass.WaterMark = "Password Here !!";
            this.txtAdminPass.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtAdminPass.SelectedText = "";
            this.txtAdminPass.SelectionLength = 0;
            this.txtAdminPass.SelectionStart = 0;
            this.txtAdminPass.ShortcutsEnabled = true;
            this.txtAdminPass.Size = new System.Drawing.Size(254, 23);
            this.txtAdminPass.TabIndex = 4;
            this.txtAdminPass.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtAdminPass.UseSelectable = true;
            this.txtAdminPass.WaterMark = "Password Here !!";
            this.txtAdminPass.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtAdminPass.WaterMarkFont = new System.Drawing.Font("Georgia", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAdminPass.Click += new System.EventHandler(this.txtAdminPass_Click);
            // 
            // btnConfirm
            // 
            this.btnConfirm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.btnConfirm.FlatAppearance.BorderSize = 0;
            this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirm.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnConfirm.ForeColor = System.Drawing.Color.White;
            this.btnConfirm.Location = new System.Drawing.Point(162, 136);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(117, 30);
            this.btnConfirm.TabIndex = 19;
            this.btnConfirm.Text = "Confirm";
            this.btnConfirm.UseVisualStyleBackColor = false;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(25, 136);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(117, 30);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmConfirmPaasword
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(302, 191);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.txtAdminPass);
            this.Controls.Add(this.txtAdminUser);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmConfirmPaasword";
            this.Text = "frmConfirmPaasword";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private MetroFramework.Controls.MetroTextBox txtAdminUser;
        private MetroFramework.Controls.MetroTextBox txtAdminPass;
        public System.Windows.Forms.Button btnConfirm;
        public System.Windows.Forms.Button btnCancel;
    }
}