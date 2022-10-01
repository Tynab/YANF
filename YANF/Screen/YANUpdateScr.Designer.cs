namespace YANF.Screen;

partial class YANUpdateScr
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(YANUpdateScr));
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelProgressBar = new System.Windows.Forms.Panel();
            this.labelPercent = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBoxUpdating = new System.Windows.Forms.PictureBox();
            this.labelCapacity = new System.Windows.Forms.Label();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpdating)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.panelProgressBar);
            this.panelMain.Controls.Add(this.labelPercent);
            this.panelMain.Controls.Add(this.label1);
            this.panelMain.Controls.Add(this.pictureBoxUpdating);
            this.panelMain.Controls.Add(this.labelCapacity);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Font = new System.Drawing.Font("Segoe UI Light", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Margin = new System.Windows.Forms.Padding(0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(360, 240);
            this.panelMain.TabIndex = 0;
            // 
            // panelProgressBar
            // 
            this.panelProgressBar.BackColor = System.Drawing.Color.LightSkyBlue;
            this.panelProgressBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelProgressBar.Location = new System.Drawing.Point(0, 235);
            this.panelProgressBar.Margin = new System.Windows.Forms.Padding(0);
            this.panelProgressBar.Name = "panelProgressBar";
            this.panelProgressBar.Size = new System.Drawing.Size(360, 5);
            this.panelProgressBar.TabIndex = 0;
            // 
            // labelPercent
            // 
            this.labelPercent.BackColor = System.Drawing.Color.Transparent;
            this.labelPercent.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelPercent.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPercent.ForeColor = System.Drawing.Color.Gray;
            this.labelPercent.Location = new System.Drawing.Point(0, 215);
            this.labelPercent.Margin = new System.Windows.Forms.Padding(0);
            this.labelPercent.Name = "labelPercent";
            this.labelPercent.Size = new System.Drawing.Size(360, 20);
            this.labelPercent.TabIndex = 0;
            this.labelPercent.Text = "0%";
            this.labelPercent.TextAlign = System.Drawing.ContentAlignment.TopCenter;
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
            this.pictureBoxUpdating.Image = global::YANF.Properties.Resources.gUpdating;
            this.pictureBoxUpdating.Location = new System.Drawing.Point(0, 20);
            this.pictureBoxUpdating.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBoxUpdating.Name = "pictureBoxUpdating";
            this.pictureBoxUpdating.Size = new System.Drawing.Size(360, 150);
            this.pictureBoxUpdating.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxUpdating.TabIndex = 1;
            this.pictureBoxUpdating.TabStop = false;
            // 
            // labelCapacity
            // 
            this.labelCapacity.BackColor = System.Drawing.Color.Transparent;
            this.labelCapacity.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelCapacity.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCapacity.ForeColor = System.Drawing.Color.Gray;
            this.labelCapacity.Location = new System.Drawing.Point(0, 0);
            this.labelCapacity.Margin = new System.Windows.Forms.Padding(0);
            this.labelCapacity.Name = "labelCapacity";
            this.labelCapacity.Size = new System.Drawing.Size(360, 20);
            this.labelCapacity.TabIndex = 0;
            this.labelCapacity.Text = "0 MB / 0 MB";
            this.labelCapacity.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
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
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Updating...";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.panelMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpdating)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel panelMain;
    public System.Windows.Forms.Panel panelProgressBar;
    public System.Windows.Forms.Label labelPercent;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.PictureBox pictureBoxUpdating;
    public System.Windows.Forms.Label labelCapacity;
}