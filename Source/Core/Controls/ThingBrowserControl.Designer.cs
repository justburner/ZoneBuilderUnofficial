namespace CodeImp.DoomBuilder.Controls
{
	partial class ThingBrowserControl
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
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThingBrowserControl));
            this.sizecaption = new System.Windows.Forms.Label();
            this.blockingcaption = new System.Windows.Forms.Label();
            this.positioncaption = new System.Windows.Forms.Label();
            this.typecaption = new System.Windows.Forms.Label();
            this.parametercaption = new System.Windows.Forms.Label();
            this.sizelabel = new System.Windows.Forms.Label();
            this.blockinglabel = new System.Windows.Forms.Label();
            this.positionlabel = new System.Windows.Forms.Label();
            this.thingimages = new System.Windows.Forms.ImageList(this.components);
            this.infopanel = new System.Windows.Forms.Panel();
            this.spritepanel = new System.Windows.Forms.Panel();
            this.classname = new System.Windows.Forms.LinkLabel();
            this.labelclassname = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbFilter = new System.Windows.Forms.TextBox();
            this.bClear = new System.Windows.Forms.Button();
            this.updatetimer = new System.Windows.Forms.Timer(this.components);
            this.typelist = new CodeImp.DoomBuilder.GZBuilder.Controls.MultiSelectTreeview();
            this.spritetex = new CodeImp.DoomBuilder.Controls.ConfigurablePictureBox();
            this.typeid = new CodeImp.DoomBuilder.Controls.NumericTextbox();
            this.parameterid = new CodeImp.DoomBuilder.Controls.NumericTextbox();
            this.fulltypecaption = new System.Windows.Forms.Label();
            this.fulltypelabel = new System.Windows.Forms.Label();
            this.infopanel.SuspendLayout();
            this.spritepanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spritetex)).BeginInit();
            this.SuspendLayout();
            // 
            // sizecaption
            // 
            this.sizecaption.Location = new System.Drawing.Point(6, 79);
            this.sizecaption.Name = "sizecaption";
            this.sizecaption.Size = new System.Drawing.Size(54, 13);
            this.sizecaption.TabIndex = 16;
            this.sizecaption.Text = "Size:";
            this.sizecaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // blockingcaption
            // 
            this.blockingcaption.Location = new System.Drawing.Point(6, 95);
            this.blockingcaption.Name = "blockingcaption";
            this.blockingcaption.Size = new System.Drawing.Size(54, 13);
            this.blockingcaption.TabIndex = 14;
            this.blockingcaption.Text = "Blocking:";
            this.blockingcaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // positioncaption
            // 
            this.positioncaption.Location = new System.Drawing.Point(6, 63);
            this.positioncaption.Name = "positioncaption";
            this.positioncaption.Size = new System.Drawing.Size(54, 13);
            this.positioncaption.TabIndex = 12;
            this.positioncaption.Text = "Position:";
            this.positioncaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // typecaption
            // 
            this.typecaption.Location = new System.Drawing.Point(6, 6);
            this.typecaption.Name = "typecaption";
            this.typecaption.Size = new System.Drawing.Size(54, 13);
            this.typecaption.TabIndex = 10;
            this.typecaption.Text = "Type:";
            this.typecaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // parametercaption
            // 
            this.parametercaption.Location = new System.Drawing.Point(0, 27);
            this.parametercaption.Name = "parametercaption";
            this.parametercaption.Size = new System.Drawing.Size(60, 13);
            this.parametercaption.TabIndex = 10;
            this.parametercaption.Text = "Parameter:";
            this.parametercaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sizelabel
            // 
            this.sizelabel.AutoSize = true;
            this.sizelabel.Location = new System.Drawing.Point(66, 79);
            this.sizelabel.Name = "sizelabel";
            this.sizelabel.Size = new System.Drawing.Size(42, 13);
            this.sizelabel.TabIndex = 17;
            this.sizelabel.Text = "16 x 96";
            // 
            // blockinglabel
            // 
            this.blockinglabel.AutoSize = true;
            this.blockinglabel.Location = new System.Drawing.Point(66, 95);
            this.blockinglabel.Name = "blockinglabel";
            this.blockinglabel.Size = new System.Drawing.Size(63, 13);
            this.blockinglabel.TabIndex = 15;
            this.blockinglabel.Text = "True-Height";
            // 
            // positionlabel
            // 
            this.positionlabel.AutoSize = true;
            this.positionlabel.Location = new System.Drawing.Point(66, 63);
            this.positionlabel.Name = "positionlabel";
            this.positionlabel.Size = new System.Drawing.Size(38, 13);
            this.positionlabel.TabIndex = 13;
            this.positionlabel.Text = "Ceiling";
            // 
            // thingimages
            // 
            this.thingimages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("thingimages.ImageStream")));
            this.thingimages.TransparentColor = System.Drawing.SystemColors.Window;
            this.thingimages.Images.SetKeyName(0, "thing00.png");
            this.thingimages.Images.SetKeyName(1, "thing01.png");
            this.thingimages.Images.SetKeyName(2, "thing02.png");
            this.thingimages.Images.SetKeyName(3, "thing03.png");
            this.thingimages.Images.SetKeyName(4, "thing04.png");
            this.thingimages.Images.SetKeyName(5, "thing05.png");
            this.thingimages.Images.SetKeyName(6, "thing06.png");
            this.thingimages.Images.SetKeyName(7, "thing07.png");
            this.thingimages.Images.SetKeyName(8, "thing08.png");
            this.thingimages.Images.SetKeyName(9, "thing09.png");
            this.thingimages.Images.SetKeyName(10, "thing10.png");
            this.thingimages.Images.SetKeyName(11, "thing11.png");
            this.thingimages.Images.SetKeyName(12, "thing12.png");
            this.thingimages.Images.SetKeyName(13, "thing13.png");
            this.thingimages.Images.SetKeyName(14, "thing14.png");
            this.thingimages.Images.SetKeyName(15, "thing15.png");
            this.thingimages.Images.SetKeyName(16, "thing16.png");
            this.thingimages.Images.SetKeyName(17, "thing17.png");
            this.thingimages.Images.SetKeyName(18, "thing18.png");
            this.thingimages.Images.SetKeyName(19, "thing19.png");
            this.thingimages.Images.SetKeyName(20, "category00.png");
            this.thingimages.Images.SetKeyName(21, "category01.png");
            this.thingimages.Images.SetKeyName(22, "category02.png");
            this.thingimages.Images.SetKeyName(23, "category03.png");
            this.thingimages.Images.SetKeyName(24, "category04.png");
            this.thingimages.Images.SetKeyName(25, "category05.png");
            this.thingimages.Images.SetKeyName(26, "category06.png");
            this.thingimages.Images.SetKeyName(27, "category07.png");
            this.thingimages.Images.SetKeyName(28, "category08.png");
            this.thingimages.Images.SetKeyName(29, "category09.png");
            this.thingimages.Images.SetKeyName(30, "category10.png");
            this.thingimages.Images.SetKeyName(31, "category11.png");
            this.thingimages.Images.SetKeyName(32, "category12.png");
            this.thingimages.Images.SetKeyName(33, "category13.png");
            this.thingimages.Images.SetKeyName(34, "category14.png");
            this.thingimages.Images.SetKeyName(35, "category15.png");
            this.thingimages.Images.SetKeyName(36, "category16.png");
            this.thingimages.Images.SetKeyName(37, "category17.png");
            this.thingimages.Images.SetKeyName(38, "category18.png");
            this.thingimages.Images.SetKeyName(39, "category19.png");
            this.thingimages.Images.SetKeyName(40, "Warning.png");
            // 
            // infopanel
            // 
            this.infopanel.Controls.Add(this.fulltypecaption);
            this.infopanel.Controls.Add(this.fulltypelabel);
            this.infopanel.Controls.Add(this.spritepanel);
            this.infopanel.Controls.Add(this.classname);
            this.infopanel.Controls.Add(this.labelclassname);
            this.infopanel.Controls.Add(this.sizelabel);
            this.infopanel.Controls.Add(this.typecaption);
            this.infopanel.Controls.Add(this.parametercaption);
            this.infopanel.Controls.Add(this.sizecaption);
            this.infopanel.Controls.Add(this.typeid);
            this.infopanel.Controls.Add(this.parameterid);
            this.infopanel.Controls.Add(this.blockinglabel);
            this.infopanel.Controls.Add(this.positioncaption);
            this.infopanel.Controls.Add(this.blockingcaption);
            this.infopanel.Controls.Add(this.positionlabel);
            this.infopanel.Location = new System.Drawing.Point(0, 233);
            this.infopanel.Name = "infopanel";
            this.infopanel.Size = new System.Drawing.Size(304, 128);
            this.infopanel.TabIndex = 18;
            // 
            // spritepanel
            // 
            this.spritepanel.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.spritepanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.spritepanel.Controls.Add(this.spritetex);
            this.spritepanel.Location = new System.Drawing.Point(235, 2);
            this.spritepanel.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.spritepanel.Name = "spritepanel";
            this.spritepanel.Size = new System.Drawing.Size(68, 68);
            this.spritepanel.TabIndex = 23;
            // 
            // classname
            // 
            this.classname.ActiveLinkColor = System.Drawing.SystemColors.Highlight;
            this.classname.AutoSize = true;
            this.classname.LinkColor = System.Drawing.SystemColors.HotTrack;
            this.classname.Location = new System.Drawing.Point(66, 111);
            this.classname.Name = "classname";
            this.classname.Size = new System.Drawing.Size(165, 13);
            this.classname.TabIndex = 27;
            this.classname.TabStop = true;
            this.classname.Text = "SuperTurboTurkeyPuncherPlayer";
            this.classname.VisitedLinkColor = System.Drawing.SystemColors.HotTrack;
            this.classname.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.classname_LinkClicked);
            // 
            // labelclassname
            // 
            this.labelclassname.Location = new System.Drawing.Point(6, 111);
            this.labelclassname.Name = "labelclassname";
            this.labelclassname.Size = new System.Drawing.Size(54, 13);
            this.labelclassname.TabIndex = 25;
            this.labelclassname.Text = "Class:";
            this.labelclassname.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Filter:";
            // 
            // tbFilter
            // 
            this.tbFilter.Location = new System.Drawing.Point(42, 3);
            this.tbFilter.Name = "tbFilter";
            this.tbFilter.Size = new System.Drawing.Size(232, 20);
            this.tbFilter.TabIndex = 20;
            this.tbFilter.TextChanged += new System.EventHandler(this.tbFilter_TextChanged);
            this.tbFilter.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tbFilter_KeyUp);
            // 
            // bClear
            // 
            this.bClear.Image = global::CodeImp.DoomBuilder.Properties.Resources.SearchClear;
            this.bClear.Location = new System.Drawing.Point(277, 1);
            this.bClear.Name = "bClear";
            this.bClear.Size = new System.Drawing.Size(24, 23);
            this.bClear.TabIndex = 21;
            this.bClear.UseVisualStyleBackColor = true;
            this.bClear.Click += new System.EventHandler(this.bClear_Click);
            // 
            // updatetimer
            // 
            this.updatetimer.Tick += new System.EventHandler(this.updatetimer_Tick);
            // 
            // typelist
            // 
            this.typelist.HideSelection = false;
            this.typelist.ImageIndex = 0;
            this.typelist.ImageList = this.thingimages;
            this.typelist.Location = new System.Drawing.Point(0, 28);
            this.typelist.Margin = new System.Windows.Forms.Padding(8, 8, 9, 8);
            this.typelist.Name = "typelist";
            this.typelist.SelectedImageIndex = 0;
            this.typelist.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.typelist.SelectionMode = CodeImp.DoomBuilder.GZBuilder.Controls.TreeViewSelectionMode.SingleSelect;
            this.typelist.ShowNodeToolTips = true;
            this.typelist.Size = new System.Drawing.Size(304, 203);
            this.typelist.TabIndex = 22;
            this.typelist.SelectionsChanged += new System.EventHandler(this.typelist_SelectionsChanged);
            this.typelist.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.typelist_KeyPress);
            this.typelist.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.typelist_MouseDoubleClick);
            this.typelist.MouseEnter += new System.EventHandler(this.typelist_MouseEnter);
            // 
            // spritetex
            // 
            this.spritetex.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.Default;
            this.spritetex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spritetex.Highlighted = false;
            this.spritetex.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            this.spritetex.Location = new System.Drawing.Point(0, 0);
            this.spritetex.Name = "spritetex";
            this.spritetex.PageUnit = System.Drawing.GraphicsUnit.Pixel;
            this.spritetex.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            this.spritetex.Size = new System.Drawing.Size(64, 64);
            this.spritetex.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.spritetex.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            this.spritetex.TabIndex = 0;
            this.spritetex.TabStop = false;
            // 
            // typeid
            // 
            this.typeid.AllowDecimal = false;
            this.typeid.AllowNegative = false;
            this.typeid.AllowRelative = false;
            this.typeid.ForeColor = System.Drawing.SystemColors.WindowText;
            this.typeid.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.typeid.Location = new System.Drawing.Point(66, 2);
            this.typeid.Name = "typeid";
            this.typeid.Size = new System.Drawing.Size(68, 20);
            this.typeid.TabIndex = 1;
            this.typeid.TextChanged += new System.EventHandler(this.typeid_TextChanged);
            // 
            // parameterid
            // 
            this.parameterid.AllowDecimal = false;
            this.parameterid.AllowNegative = false;
            this.parameterid.AllowRelative = false;
            this.parameterid.ForeColor = System.Drawing.SystemColors.WindowText;
            this.parameterid.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.parameterid.Location = new System.Drawing.Point(66, 24);
            this.parameterid.Name = "parameterid";
            this.parameterid.Size = new System.Drawing.Size(68, 20);
            this.parameterid.TabIndex = 2;
            this.parameterid.TextChanged += new System.EventHandler(this.parameterid_TextChanged);
            // 
            // fulltypecaption
            // 
            this.fulltypecaption.Location = new System.Drawing.Point(6, 47);
            this.fulltypecaption.Name = "fulltypecaption";
            this.fulltypecaption.Size = new System.Drawing.Size(54, 13);
            this.fulltypecaption.TabIndex = 28;
            this.fulltypecaption.Text = "Full type:";
            this.fulltypecaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // fulltypelabel
            // 
            this.fulltypelabel.AutoSize = true;
            this.fulltypelabel.Location = new System.Drawing.Point(66, 47);
            this.fulltypelabel.Name = "fulltypelabel";
            this.fulltypelabel.Size = new System.Drawing.Size(13, 13);
            this.fulltypelabel.TabIndex = 29;
            this.fulltypelabel.Text = "0";
            // 
            // ThingBrowserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.typelist);
            this.Controls.Add(this.bClear);
            this.Controls.Add(this.tbFilter);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.infopanel);
            this.Name = "ThingBrowserControl";
            this.Size = new System.Drawing.Size(304, 361);
            this.Resize += new System.EventHandler(this.ThingBrowserControl_Resize);
            this.infopanel.ResumeLayout(false);
            this.infopanel.PerformLayout();
            this.spritepanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spritetex)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label sizelabel;
		private System.Windows.Forms.Label blockinglabel;
		private System.Windows.Forms.Label positionlabel;
		private NumericTextbox typeid;
        private NumericTextbox parameterid;
        private System.Windows.Forms.ImageList thingimages;
		private System.Windows.Forms.Panel infopanel;
		private System.Windows.Forms.Label sizecaption;
		private System.Windows.Forms.Label blockingcaption;
		private System.Windows.Forms.Label positioncaption;
		private System.Windows.Forms.Label typecaption;
        private System.Windows.Forms.Label parametercaption;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbFilter;
		private System.Windows.Forms.Button bClear;
		private CodeImp.DoomBuilder.GZBuilder.Controls.MultiSelectTreeview typelist;
		private System.Windows.Forms.Panel spritepanel;
		private System.Windows.Forms.Timer updatetimer;
		private ConfigurablePictureBox spritetex;
		private System.Windows.Forms.LinkLabel classname;
		private System.Windows.Forms.Label labelclassname;
        private System.Windows.Forms.Label fulltypecaption;
        private System.Windows.Forms.Label fulltypelabel;
    }
}
