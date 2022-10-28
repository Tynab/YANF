namespace YANF.Screen
{
    partial class Demo2
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Demo2));
            this.yanGradPnl1 = new YANF.Control.YANGradPnl();
            this.btnBack = new YANF.Control.YANBtn();
            this.toolTipMain = new System.Windows.Forms.ToolTip(this.components);
            this.yanGradPnl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // yanGradPnl1
            // 
            this.yanGradPnl1.Angle = 45F;
            this.yanGradPnl1.BackColor = System.Drawing.Color.Transparent;
            this.yanGradPnl1.BottomColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(121)))), ((int)(((byte)(209)))));
            this.yanGradPnl1.Controls.Add(this.btnBack);
            this.yanGradPnl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.yanGradPnl1.Font = new System.Drawing.Font("Segoe UI Light", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(254)));
            this.yanGradPnl1.ForeColor = System.Drawing.Color.White;
            this.yanGradPnl1.Location = new System.Drawing.Point(0, 0);
            this.yanGradPnl1.Name = "yanGradPnl1";
            this.yanGradPnl1.Size = new System.Drawing.Size(1200, 750);
            this.yanGradPnl1.TabIndex = 0;
            this.yanGradPnl1.TopColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(37)))), ((int)(((byte)(175)))));
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.Transparent;
            this.btnBack.BackgroundImage = global::YANF.Properties.Resources.pBI;
            this.btnBack.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btnBack.BorderColor = System.Drawing.Color.PaleVioletRed;
            this.btnBack.BorderRadius = 0;
            this.btnBack.BorderSize = 0;
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.Location = new System.Drawing.Point(10, 10);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(20, 20);
            this.btnBack.TabIndex = 0;
            this.btnBack.TabStop = false;
            this.toolTipMain.SetToolTip(this.btnBack, "Quay lại");
            this.btnBack.UseVisualStyleBackColor = false;
            // 
            // Demo2
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.ControlBox = false;
            this.Controls.Add(this.yanGradPnl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Demo2";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Profile";
            this.TransparencyKey = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.yanGradPnl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Control.YANGradPnl yanGradPnl1;
        private Control.YANBtn btnBack;
        private System.Windows.Forms.ToolTip toolTipMain;
    }
}