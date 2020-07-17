namespace CodeImp.DoomBuilder.BuilderModes
{
	partial class DrawShapeOptionsPanel
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
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label3;
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.spikiness = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.sides = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.createas = new System.Windows.Forms.ComboBox();
            this.spike50 = new System.Windows.Forms.Button();
            this.sides24 = new System.Windows.Forms.Button();
            this.sides8 = new System.Windows.Forms.Button();
            this.sides3 = new System.Windows.Forms.Button();
            this.spikingmode = new System.Windows.Forms.ComboBox();
            this.frontoutside = new System.Windows.Forms.CheckBox();
            this.spike0 = new System.Windows.Forms.Button();
            this.ellipse = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.sweepangle = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.startangle = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.limitanglequad = new System.Windows.Forms.CheckBox();
            this.startanglewheel = new CodeImp.DoomBuilder.GZBuilder.Controls.AngleControlF();
            this.sweepanglewheel = new CodeImp.DoomBuilder.GZBuilder.Controls.AngleControlF();
            this.previewreference = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.firstpointtype = new System.Windows.Forms.ComboBox();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(17, 39);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(65, 13);
            label5.TabIndex = 1;
            label5.Text = "1st Point as:";
            this.tooltip.SetToolTip(label5, "Select how the shape will be placed in the map:\r\n* Starting from origin\r\n* Replic" +
                    "ate Draw Ellipse Mode behaviour\r\n* Side/corner into origin.");
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(20, 20);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(93, 13);
            label6.TabIndex = 0;
            label6.Text = "Create shape as...";
            this.tooltip.SetToolTip(label6, "Select which type of elements to create.\r\nCheck help (F1) for more information.");
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(17, 113);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(55, 13);
            label2.TabIndex = 5;
            label2.Text = "Spikiness:";
            this.tooltip.SetToolTip(label2, "Sets percentage of spikiness.\r\nExcept \"Draw Ellipse behaviour\" spike type, in tha" +
                    "t case spikiness is in mp.");
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(35, 88);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(36, 13);
            label1.TabIndex = 3;
            label1.Text = "Sides:";
            this.tooltip.SetToolTip(label1, "Sets number of sides that the shape should have.\r\nNote that reducing sweep angle " +
                    "won\'t reduce the number of sides.\r\nWhole number only, 3 sides minimum.");
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(125, 24);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(73, 13);
            label4.TabIndex = 3;
            label4.Text = "Sweep Angle:";
            this.tooltip.SetToolTip(label4, "Controls the sweep angle of the slice.\r\n360º will create a closed shape without s" +
                    "lices.\r\nUse comma ( , ) for fractional number, e.g.: 12,5");
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(32, 24);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(62, 13);
            label3.TabIndex = 0;
            label3.Text = "Start Angle:";
            this.tooltip.SetToolTip(label3, "Controls the starting angle of the first point.\r\nUse comma ( , ) for fractional n" +
                    "umber, e.g.: 12,5");
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.spikiness);
            this.groupBox1.Controls.Add(this.sides);
            this.groupBox1.Controls.Add(label6);
            this.groupBox1.Controls.Add(this.createas);
            this.groupBox1.Controls.Add(this.spike50);
            this.groupBox1.Controls.Add(this.sides24);
            this.groupBox1.Controls.Add(this.sides8);
            this.groupBox1.Controls.Add(this.sides3);
            this.groupBox1.Controls.Add(this.spikingmode);
            this.groupBox1.Controls.Add(this.frontoutside);
            this.groupBox1.Controls.Add(this.spike0);
            this.groupBox1.Controls.Add(label2);
            this.groupBox1.Controls.Add(label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 128);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(234, 180);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Shape Control: ";
            // 
            // spikiness
            // 
            this.spikiness.AllowDecimal = false;
            this.spikiness.AllowNegative = false;
            this.spikiness.AllowRelative = false;
            this.spikiness.ButtonStep = 1;
            this.spikiness.ButtonStepBig = 10F;
            this.spikiness.ButtonStepFloat = 1F;
            this.spikiness.ButtonStepSmall = 5F;
            this.spikiness.ButtonStepsUseModifierKeys = true;
            this.spikiness.ButtonStepsWrapAround = false;
            this.spikiness.Location = new System.Drawing.Point(75, 108);
            this.spikiness.Name = "spikiness";
            this.spikiness.Size = new System.Drawing.Size(60, 24);
            this.spikiness.StepValues = null;
            this.spikiness.TabIndex = 6;
            this.spikiness.WhenTextChanged += new System.EventHandler(this.spikiness_WhenTextChanged);
            // 
            // sides
            // 
            this.sides.AllowDecimal = false;
            this.sides.AllowNegative = false;
            this.sides.AllowRelative = false;
            this.sides.ButtonStep = 1;
            this.sides.ButtonStepBig = 4F;
            this.sides.ButtonStepFloat = 1F;
            this.sides.ButtonStepSmall = 2F;
            this.sides.ButtonStepsUseModifierKeys = true;
            this.sides.ButtonStepsWrapAround = false;
            this.sides.Location = new System.Drawing.Point(75, 83);
            this.sides.Name = "sides";
            this.sides.Size = new System.Drawing.Size(60, 24);
            this.sides.StepValues = null;
            this.sides.TabIndex = 4;
            this.sides.WhenTextChanged += new System.EventHandler(this.sides_WhenTextChanged);
            // 
            // createas
            // 
            this.createas.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.createas.FormattingEnabled = true;
            this.createas.Items.AddRange(new object[] {
            "Close shape towards origin",
            "Close shape, first to last vertex",
            "Open or close shape",
            "Shape w/o intersects or sectors",
            "Vertices",
            "Things"});
            this.createas.Location = new System.Drawing.Point(18, 36);
            this.createas.Name = "createas";
            this.createas.Size = new System.Drawing.Size(200, 21);
            this.createas.TabIndex = 1;
            this.tooltip.SetToolTip(this.createas, "Select which type of elements to create.\r\nCheck help (F1) for more information.");
            this.createas.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
            // 
            // spike50
            // 
            this.spike50.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.DrawShapeSpike50;
            this.spike50.Location = new System.Drawing.Point(170, 111);
            this.spike50.Name = "spike50";
            this.spike50.Size = new System.Drawing.Size(24, 24);
            this.spike50.TabIndex = 12;
            this.spike50.UseVisualStyleBackColor = true;
            this.spike50.Click += new System.EventHandler(this.spike50_Click);
            // 
            // sides24
            // 
            this.sides24.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.DrawShape24Sides;
            this.sides24.Location = new System.Drawing.Point(200, 83);
            this.sides24.Name = "sides24";
            this.sides24.Size = new System.Drawing.Size(24, 24);
            this.sides24.TabIndex = 10;
            this.sides24.UseVisualStyleBackColor = true;
            this.sides24.Click += new System.EventHandler(this.sides24_Click);
            // 
            // sides8
            // 
            this.sides8.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.DrawShape8Sides;
            this.sides8.Location = new System.Drawing.Point(170, 83);
            this.sides8.Name = "sides8";
            this.sides8.Size = new System.Drawing.Size(24, 24);
            this.sides8.TabIndex = 9;
            this.sides8.UseVisualStyleBackColor = true;
            this.sides8.Click += new System.EventHandler(this.sides8_Click);
            // 
            // sides3
            // 
            this.sides3.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.DrawShape3Sides;
            this.sides3.Location = new System.Drawing.Point(140, 83);
            this.sides3.Name = "sides3";
            this.sides3.Size = new System.Drawing.Size(24, 24);
            this.sides3.TabIndex = 8;
            this.sides3.UseVisualStyleBackColor = true;
            this.sides3.Click += new System.EventHandler(this.sides3_Click);
            // 
            // spikingmode
            // 
            this.spikingmode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.spikingmode.FormattingEnabled = true;
            this.spikingmode.Items.AddRange(new object[] {
            "Spike outside, start spiked",
            "Spike outside, start normal",
            "Spike inside, start spiked",
            "Spike inside, start normal",
            "Spike zig-zag, start spiked",
            "Spike gear, start spiked",
            "Spike zig-zag, start normal",
            "Spike gear, start normal",
            "DrawEllipse behaviour, start spiked",
            "DrawEllipse behaviour, start normal"});
            this.spikingmode.Location = new System.Drawing.Point(18, 141);
            this.spikingmode.Name = "spikingmode";
            this.spikingmode.Size = new System.Drawing.Size(200, 21);
            this.spikingmode.TabIndex = 7;
            this.tooltip.SetToolTip(this.spikingmode, "Controls how spikes behave in relation to the Spikiness level.");
            this.spikingmode.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
            // 
            // frontoutside
            // 
            this.frontoutside.AutoSize = true;
            this.frontoutside.Location = new System.Drawing.Point(20, 63);
            this.frontoutside.Name = "frontoutside";
            this.frontoutside.Size = new System.Drawing.Size(159, 17);
            this.frontoutside.TabIndex = 2;
            this.frontoutside.Text = "Front Outside / Flip Linedefs";
            this.tooltip.SetToolTip(this.frontoutside, "Shape linedefs will point outside when checked.\r\nIgnored if \"Create shape as..\" i" +
                    "s \"Vertices\" or \"Things\".");
            this.frontoutside.UseVisualStyleBackColor = true;
            this.frontoutside.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // spike0
            // 
            this.spike0.Image = global::CodeImp.DoomBuilder.BuilderModes.Properties.Resources.DrawShapeSpike0;
            this.spike0.Location = new System.Drawing.Point(140, 111);
            this.spike0.Name = "spike0";
            this.spike0.Size = new System.Drawing.Size(24, 24);
            this.spike0.TabIndex = 11;
            this.spike0.UseVisualStyleBackColor = true;
            this.spike0.Click += new System.EventHandler(this.spike0_Click);
            // 
            // ellipse
            // 
            this.ellipse.AutoSize = true;
            this.ellipse.Location = new System.Drawing.Point(20, 19);
            this.ellipse.Name = "ellipse";
            this.ellipse.Size = new System.Drawing.Size(56, 17);
            this.ellipse.TabIndex = 0;
            this.ellipse.Text = "Ellipse";
            this.tooltip.SetToolTip(this.ellipse, "Shape can only be scaled in one axis when unchecked.\r\nThis also allows to be much" +
                    " easier to setup angles with the mouse.");
            this.ellipse.UseVisualStyleBackColor = true;
            this.ellipse.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.sweepangle);
            this.groupBox2.Controls.Add(this.startangle);
            this.groupBox2.Controls.Add(this.limitanglequad);
            this.groupBox2.Controls.Add(this.startanglewheel);
            this.groupBox2.Controls.Add(this.sweepanglewheel);
            this.groupBox2.Controls.Add(label4);
            this.groupBox2.Controls.Add(label3);
            this.groupBox2.Location = new System.Drawing.Point(3, 314);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(234, 200);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Adjustment / Slicing: ";
            // 
            // sweepangle
            // 
            this.sweepangle.AllowDecimal = true;
            this.sweepangle.AllowNegative = false;
            this.sweepangle.AllowRelative = false;
            this.sweepangle.ButtonStep = 1;
            this.sweepangle.ButtonStepBig = 45F;
            this.sweepangle.ButtonStepFloat = 1F;
            this.sweepangle.ButtonStepSmall = 3F;
            this.sweepangle.ButtonStepsUseModifierKeys = true;
            this.sweepangle.ButtonStepsWrapAround = false;
            this.sweepangle.Location = new System.Drawing.Point(122, 40);
            this.sweepangle.Name = "sweepangle";
            this.sweepangle.Size = new System.Drawing.Size(96, 24);
            this.sweepangle.StepValues = null;
            this.sweepangle.TabIndex = 4;
            this.sweepangle.WhenTextChanged += new System.EventHandler(this.sweepangle_WhenTextChanged);
            // 
            // startangle
            // 
            this.startangle.AllowDecimal = true;
            this.startangle.AllowNegative = false;
            this.startangle.AllowRelative = false;
            this.startangle.ButtonStep = 1;
            this.startangle.ButtonStepBig = 45F;
            this.startangle.ButtonStepFloat = 1F;
            this.startangle.ButtonStepSmall = 3F;
            this.startangle.ButtonStepsUseModifierKeys = true;
            this.startangle.ButtonStepsWrapAround = false;
            this.startangle.Location = new System.Drawing.Point(18, 40);
            this.startangle.Name = "startangle";
            this.startangle.Size = new System.Drawing.Size(96, 24);
            this.startangle.StepValues = null;
            this.startangle.TabIndex = 1;
            this.startangle.WhenTextChanged += new System.EventHandler(this.startangle_WhenTextChanged);
            // 
            // limitanglequad
            // 
            this.limitanglequad.AutoSize = true;
            this.limitanglequad.Location = new System.Drawing.Point(20, 172);
            this.limitanglequad.Name = "limitanglequad";
            this.limitanglequad.Size = new System.Drawing.Size(176, 17);
            this.limitanglequad.TabIndex = 6;
            this.limitanglequad.Text = "Limit to one quadrant (Top-right)";
            this.tooltip.SetToolTip(this.limitanglequad, "Locks start angle to top-right quadrant when checked.\r\nInternally the angle will " +
                    "automatically adjust based of which quadrant the mouse is in.");
            this.limitanglequad.UseVisualStyleBackColor = true;
            this.limitanglequad.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // startanglewheel
            // 
            this.startanglewheel.AllowLoops = false;
            this.startanglewheel.Angle = 0F;
            this.startanglewheel.AngleOffset = 0F;
            this.startanglewheel.Location = new System.Drawing.Point(19, 70);
            this.startanglewheel.Name = "startanglewheel";
            this.startanglewheel.Size = new System.Drawing.Size(96, 96);
            this.startanglewheel.SnapAngle = 22.5F;
            this.startanglewheel.TabIndex = 2;
            this.startanglewheel.AngleChanged += new System.EventHandler(this.startanglewheel_AngleChanged);
            // 
            // sweepanglewheel
            // 
            this.sweepanglewheel.AllowLoops = false;
            this.sweepanglewheel.Angle = 0F;
            this.sweepanglewheel.AngleOffset = 0F;
            this.sweepanglewheel.Location = new System.Drawing.Point(122, 70);
            this.sweepanglewheel.Name = "sweepanglewheel";
            this.sweepanglewheel.Size = new System.Drawing.Size(96, 96);
            this.sweepanglewheel.SnapAngle = 22.5F;
            this.sweepanglewheel.TabIndex = 5;
            this.sweepanglewheel.AngleChanged += new System.EventHandler(this.sweepanglewheel_AngleChanged);
            // 
            // previewreference
            // 
            this.previewreference.AutoSize = true;
            this.previewreference.Location = new System.Drawing.Point(23, 3);
            this.previewreference.Name = "previewreference";
            this.previewreference.Size = new System.Drawing.Size(112, 17);
            this.previewreference.TabIndex = 0;
            this.previewreference.Text = "Preview reference";
            this.tooltip.SetToolTip(this.previewreference, "Visual only. \r\nThe reference circle will be shown on screen when checked.");
            this.previewreference.UseVisualStyleBackColor = true;
            this.previewreference.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(label5);
            this.groupBox3.Controls.Add(this.firstpointtype);
            this.groupBox3.Controls.Add(this.ellipse);
            this.groupBox3.Location = new System.Drawing.Point(3, 26);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(234, 96);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = " Sizing: ";
            // 
            // firstpointtype
            // 
            this.firstpointtype.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.firstpointtype.FormattingEnabled = true;
            this.firstpointtype.Items.AddRange(new object[] {
            "Center towards Radius",
            "Corner like DrawEllipse",
            "Side/Corner towards Origin"});
            this.firstpointtype.Location = new System.Drawing.Point(18, 55);
            this.firstpointtype.Name = "firstpointtype";
            this.firstpointtype.Size = new System.Drawing.Size(200, 21);
            this.firstpointtype.TabIndex = 2;
            this.tooltip.SetToolTip(this.firstpointtype, "Select how the shape will be placed in the map:\r\n* Starting from origin\r\n* Replic" +
                    "ate Draw Ellipse Mode behaviour\r\n* Side/corner into origin.");
            this.firstpointtype.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
            // 
            // tooltip
            // 
            this.tooltip.AutoPopDelay = 10000;
            this.tooltip.InitialDelay = 500;
            this.tooltip.ReshowDelay = 100;
            // 
            // DrawShapeOptionsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.previewreference);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "DrawShapeOptionsPanel";
            this.Size = new System.Drawing.Size(240, 516);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox ellipse;
        private System.Windows.Forms.Button spike0;
        private GZBuilder.Controls.AngleControlF sweepanglewheel;
        private GZBuilder.Controls.AngleControlF startanglewheel;
        private System.Windows.Forms.CheckBox frontoutside;
        private System.Windows.Forms.CheckBox previewreference;
        private System.Windows.Forms.ComboBox spikingmode;
        private System.Windows.Forms.Button sides24;
        private System.Windows.Forms.Button sides8;
        private System.Windows.Forms.Button sides3;
        private System.Windows.Forms.Button spike50;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox firstpointtype;
        private System.Windows.Forms.CheckBox limitanglequad;
        private System.Windows.Forms.ComboBox createas;
        private System.Windows.Forms.ToolTip tooltip;
        private Controls.ButtonsNumericTextbox sides;
        private Controls.ButtonsNumericTextbox spikiness;
        private Controls.ButtonsNumericTextbox startangle;
        private Controls.ButtonsNumericTextbox sweepangle;
	}
}
