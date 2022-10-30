namespace YANF.Screen;

partial class YANUpdateScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(YANUpdateScreen));
            this.panelMain = new System.Windows.Forms.Panel();
            this.pnlProgressBar = new System.Windows.Forms.Panel();
            this.lblPercent = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBoxUpdating = new System.Windows.Forms.PictureBox();
            this.lblCapacity = new System.Windows.Forms.Label();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpdating)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.pnlProgressBar);
            this.panelMain.Controls.Add(this.lblPercent);
            this.panelMain.Controls.Add(this.label1);
            this.panelMain.Controls.Add(this.pictureBoxUpdating);
            this.panelMain.Controls.Add(this.lblCapacity);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Font = new System.Drawing.Font("Segoe UI Light", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Margin = new System.Windows.Forms.Padding(0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(360, 240);
            this.panelMain.TabIndex = 0;
            // 
            // pnlProgressBar
            // 
            this.pnlProgressBar.BackColor = System.Drawing.Color.LightSkyBlue;
            this.pnlProgressBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlProgressBar.Location = new System.Drawing.Point(0, 235);
            this.pnlProgressBar.Margin = new System.Windows.Forms.Padding(0);
            this.pnlProgressBar.Name = "pnlProgressBar";
            this.pnlProgressBar.Size = new System.Drawing.Size(1, 5);
            this.pnlProgressBar.TabIndex = 0;
            // 
            // lblPercent
            // 
            this.lblPercent.BackColor = System.Drawing.Color.Transparent;
            this.lblPercent.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPercent.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPercent.ForeColor = System.Drawing.Color.Gray;
            this.lblPercent.Location = new System.Drawing.Point(0, 215);
            this.lblPercent.Margin = new System.Windows.Forms.Padding(0);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new System.Drawing.Size(360, 20);
            this.lblPercent.TabIndex = 0;
            this.lblPercent.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Verdana", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Gray;
            this.label1.Location = new System.Drawing.Point(0, 170);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(360, 45);
            this.label1.TabIndex = 0;
            this.label1.Text = "Đang cập nhật...";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pictureBoxUpdating
            // 
            this.pictureBoxUpdating.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxUpdating.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBoxUpdating.Image = global::YANF.Properties.Resources.gUpdate;
            this.pictureBoxUpdating.Location = new System.Drawing.Point(0, 20);
            this.pictureBoxUpdating.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxUpdating.Name = "pictureBoxUpdating";
            this.pictureBoxUpdating.Size = new System.Drawing.Size(360, 150);
            this.pictureBoxUpdating.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxUpdating.TabIndex = 1;
            this.pictureBoxUpdating.TabStop = false;
            // 
            // lblCapacity
            // 
            this.lblCapacity.BackColor = System.Drawing.Color.Transparent;
            this.lblCapacity.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCapacity.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCapacity.ForeColor = System.Drawing.Color.Gray;
            this.lblCapacity.Location = new System.Drawing.Point(0, 0);
            this.lblCapacity.Margin = new System.Windows.Forms.Padding(0);
            this.lblCapacity.Name = "lblCapacity";
            this.lblCapacity.Size = new System.Drawing.Size(360, 20);
            this.lblCapacity.TabIndex = 0;
            this.lblCapacity.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // YANUpdateScreen
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(360, 240);
            this.ControlBox = false;
            this.Controls.Add(this.panelMain);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "YANUpdateScreen";
            this.Opacity = 0D;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Updating...";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.YANUpdateScreen_Shown);
            this.panelMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpdating)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel panelMain;
    public System.Windows.Forms.Panel pnlProgressBar;
    public System.Windows.Forms.Label lblPercent;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.PictureBox pictureBoxUpdating;
    public System.Windows.Forms.Label lblCapacity;
}