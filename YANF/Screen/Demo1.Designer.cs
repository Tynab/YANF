namespace YANF.Screen
{
    partial class Demo1
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
            this.panel2 = new System.Windows.Forms.Panel();
            this.yanTxt1 = new YANF.Control.YANTxt();
            this.yanDdl1 = new YANF.Control.YANDdl();
            this.yanBtn1 = new YANF.Control.YANBtn();
            this.yanCirPic1 = new YANF.Control.YANCirPic();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.yanCirPic1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(51)))), ((int)(((byte)(73)))));
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1200, 600);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.yanBtn1);
            this.panel2.Controls.Add(this.yanDdl1);
            this.panel2.Controls.Add(this.yanTxt1);
            this.panel2.Controls.Add(this.yanCirPic1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(260, 600);
            this.panel2.TabIndex = 0;
            // 
            // yanTxt1
            // 
            this.yanTxt1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.yanTxt1.BorderColor = System.Drawing.Color.Cyan;
            this.yanTxt1.BorderFocusColor = System.Drawing.Color.LightYellow;
            this.yanTxt1.BorderRadius = 0;
            this.yanTxt1.BorderSize = 1;
            this.yanTxt1.Font = new System.Drawing.Font("Tahoma", 10F);
            this.yanTxt1.ForeColor = System.Drawing.Color.DimGray;
            this.yanTxt1.Location = new System.Drawing.Point(70, 140);
            this.yanTxt1.MaxLength = 32767;
            this.yanTxt1.Multiline = false;
            this.yanTxt1.Name = "yanTxt1";
            this.yanTxt1.Padding = new System.Windows.Forms.Padding(10, 7, 10, 7);
            this.yanTxt1.PasswordChar = false;
            this.yanTxt1.PlaceholderColor = System.Drawing.Color.DarkGray;
            this.yanTxt1.PlaceholderText = "Mã nhân viên";
            this.yanTxt1.Size = new System.Drawing.Size(120, 32);
            this.yanTxt1.String = null;
            this.yanTxt1.TabIndex = 1;
            this.yanTxt1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.yanTxt1.UnderlinedStyle = true;
            // 
            // yanDdl1
            // 
            this.yanDdl1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.yanDdl1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.yanDdl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.yanDdl1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.yanDdl1.BorderSize = 0;
            this.yanDdl1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.yanDdl1.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.yanDdl1.ForeColor = System.Drawing.Color.White;
            this.yanDdl1.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(30)))), ((int)(((byte)(54)))));
            this.yanDdl1.ListBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(60)))), ((int)(((byte)(84)))));
            this.yanDdl1.ListTextColor = System.Drawing.Color.White;
            this.yanDdl1.Location = new System.Drawing.Point(0, 190);
            this.yanDdl1.MinimumSize = new System.Drawing.Size(200, 30);
            this.yanDdl1.Name = "yanDdl1";
            this.yanDdl1.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.yanDdl1.Size = new System.Drawing.Size(260, 40);
            this.yanDdl1.String = "Trạng Thái Nhân Viên...";
            this.yanDdl1.TabIndex = 0;
            this.yanDdl1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // yanBtn1
            // 
            this.yanBtn1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(51)))), ((int)(((byte)(73)))));
            this.yanBtn1.BorderColor = System.Drawing.Color.Empty;
            this.yanBtn1.BorderRadius = 0;
            this.yanBtn1.BorderSize = 0;
            this.yanBtn1.FlatAppearance.BorderSize = 0;
            this.yanBtn1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.yanBtn1.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Bold);
            this.yanBtn1.ForeColor = System.Drawing.Color.DodgerBlue;
            this.yanBtn1.Image = global::YANF.Properties.Resources.pPTTCB;
            this.yanBtn1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.yanBtn1.Location = new System.Drawing.Point(0, 250);
            this.yanBtn1.Name = "yanBtn1";
            this.yanBtn1.Size = new System.Drawing.Size(260, 50);
            this.yanBtn1.TabIndex = 0;
            this.yanBtn1.TabStop = false;
            this.yanBtn1.Text = "            Thông Tin Cơ Bản";
            this.yanBtn1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.yanBtn1.UseVisualStyleBackColor = false;
            // 
            // yanCirPic1
            // 
            this.yanCirPic1.Angle = 315F;
            this.yanCirPic1.BackColor = System.Drawing.Color.Transparent;
            this.yanCirPic1.BorderAngle = 50F;
            this.yanCirPic1.BorderBottomColor = System.Drawing.Color.Empty;
            this.yanCirPic1.BorderCapStyle = System.Drawing.Drawing2D.DashCap.Flat;
            this.yanCirPic1.BorderLineStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            this.yanCirPic1.BorderSize = 0;
            this.yanCirPic1.BorderTopColor = System.Drawing.Color.Empty;
            this.yanCirPic1.BottomColor = System.Drawing.Color.Cyan;
            this.yanCirPic1.Image = global::YANF.Properties.Resources.pUserSolid1;
            this.yanCirPic1.Location = new System.Drawing.Point(80, 20);
            this.yanCirPic1.Name = "yanCirPic1";
            this.yanCirPic1.Size = new System.Drawing.Size(100, 100);
            this.yanCirPic1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.yanCirPic1.TabIndex = 1;
            this.yanCirPic1.TabStop = false;
            this.yanCirPic1.TopColor = System.Drawing.Color.RoyalBlue;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.DodgerBlue;
            this.panel3.Location = new System.Drawing.Point(59, 329);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(65, 131);
            this.panel3.TabIndex = 0;
            // 
            // Demo1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(1200, 600);
            this.ControlBox = false;
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Demo1";
            this.Opacity = 0D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HRM";
            this.TransparencyKey = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.yanCirPic1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private Control.YANCirPic yanCirPic1;
        private Control.YANTxt yanTxt1;
        private Control.YANBtn yanBtn1;
        private Control.YANDdl yanDdl1;
        private System.Windows.Forms.Panel panel3;
    }
}