namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    partial class PerpendicularVertexForm
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
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label2;
            this.cancel = new System.Windows.Forms.Button();
            this.apply = new System.Windows.Forms.Button();
            this.backwards = new System.Windows.Forms.CheckBox();
            this.processtips = new System.Windows.Forms.CheckBox();
            this.createas = new System.Windows.Forms.ComboBox();
            this.distance = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.snapmp = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            label1 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(22, 54);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(52, 13);
            label1.TabIndex = 2;
            label1.Text = "Distance:";
            this.tooltip.SetToolTip(label1, "Set distance between selected linedef(s) and the end of the perpendicular.");
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(17, 128);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(57, 13);
            label4.TabIndex = 6;
            label4.Text = "Snap dist.:";
            this.tooltip.SetToolTip(label4, "Adjust snap distance to vertices that already exist in the map so perpendiculars " +
                    "get connected to them.\r\nSet to zero to disable snapping.\r\nUse comma ( , ) for fr" +
                    "actional number, e.g.: 0,5");
            // 
            // cancel
            // 
            this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(89, 155);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(70, 25);
            this.cancel.TabIndex = 9;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // apply
            // 
            this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.apply.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.apply.Location = new System.Drawing.Point(12, 155);
            this.apply.Name = "apply";
            this.apply.Size = new System.Drawing.Size(70, 25);
            this.apply.TabIndex = 8;
            this.apply.Text = "OK";
            this.apply.UseVisualStyleBackColor = true;
            this.apply.Click += new System.EventHandler(this.apply_Click);
            // 
            // backwards
            // 
            this.backwards.AutoSize = true;
            this.backwards.Location = new System.Drawing.Point(20, 103);
            this.backwards.Name = "backwards";
            this.backwards.Size = new System.Drawing.Size(79, 17);
            this.backwards.TabIndex = 5;
            this.backwards.Text = "Backwards";
            this.tooltip.SetToolTip(this.backwards, "When checked, it creates a perpendicular on the opposite side of where the linede" +
                    "fs are facing.");
            this.backwards.UseVisualStyleBackColor = true;
            this.backwards.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // processtips
            // 
            this.processtips.AutoSize = true;
            this.processtips.Checked = true;
            this.processtips.CheckState = System.Windows.Forms.CheckState.Checked;
            this.processtips.Location = new System.Drawing.Point(20, 80);
            this.processtips.Name = "processtips";
            this.processtips.Size = new System.Drawing.Size(83, 17);
            this.processtips.TabIndex = 4;
            this.processtips.Text = "Process tips";
            this.tooltip.SetToolTip(this.processtips, "When unchecked, vertices with only one linedef selected won\'t be processed.");
            this.processtips.UseVisualStyleBackColor = true;
            this.processtips.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(22, 9);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(61, 13);
            label2.TabIndex = 0;
            label2.Text = "Create as...";
            this.tooltip.SetToolTip(label2, "Select which type of elements to create.\r\nCheck help (F1) for more information.");
            // 
            // createas
            // 
            this.createas.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.createas.FormattingEnabled = true;
            this.createas.Items.AddRange(new object[] {
            "Linked Linedefs",
            "Unlinked Linedefs",
            "Vertices",
            "Things"});
            this.createas.Location = new System.Drawing.Point(12, 25);
            this.createas.Name = "createas";
            this.createas.Size = new System.Drawing.Size(146, 21);
            this.createas.TabIndex = 1;
            this.tooltip.SetToolTip(this.createas, "Select which type of elements to create.\r\nCheck help (F1) for more information.");
            this.createas.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
            // 
            // distance
            // 
            this.distance.AllowDecimal = false;
            this.distance.AllowNegative = false;
            this.distance.AllowRelative = false;
            this.distance.ButtonStep = 8;
            this.distance.ButtonStepBig = 64F;
            this.distance.ButtonStepFloat = 1F;
            this.distance.ButtonStepSmall = 1F;
            this.distance.ButtonStepsUseModifierKeys = true;
            this.distance.ButtonStepsWrapAround = false;
            this.distance.Location = new System.Drawing.Point(80, 49);
            this.distance.Name = "distance";
            this.distance.Size = new System.Drawing.Size(72, 24);
            this.distance.StepValues = null;
            this.distance.TabIndex = 3;
            this.distance.WhenTextChanged += new System.EventHandler(this.ValueChanged);
            // 
            // snapmp
            // 
            this.snapmp.AllowDecimal = true;
            this.snapmp.AllowNegative = false;
            this.snapmp.AllowRelative = false;
            this.snapmp.ButtonStep = 1;
            this.snapmp.ButtonStepBig = 1F;
            this.snapmp.ButtonStepFloat = 1F;
            this.snapmp.ButtonStepSmall = 1F;
            this.snapmp.ButtonStepsUseModifierKeys = false;
            this.snapmp.ButtonStepsWrapAround = false;
            this.snapmp.Location = new System.Drawing.Point(80, 123);
            this.snapmp.Name = "snapmp";
            this.snapmp.Size = new System.Drawing.Size(72, 24);
            this.snapmp.StepValues = null;
            this.snapmp.TabIndex = 7;
            // 
            // tooltip
            // 
            this.tooltip.AutoPopDelay = 10000;
            this.tooltip.InitialDelay = 500;
            this.tooltip.ReshowDelay = 100;
            // 
            // PerpendicularVertexForm
            // 
            this.AcceptButton = this.apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(170, 192);
            this.Controls.Add(this.snapmp);
            this.Controls.Add(this.distance);
            this.Controls.Add(label4);
            this.Controls.Add(this.createas);
            this.Controls.Add(label2);
            this.Controls.Add(this.processtips);
            this.Controls.Add(this.backwards);
            this.Controls.Add(label1);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.apply);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PerpendicularVertexForm";
            this.Opacity = 1D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Perpendicular (Vertex)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PerpendicularVertexForm_FormClosing);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.PerpendicularVertexForm_HelpRequested);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button apply;
        private System.Windows.Forms.CheckBox backwards;
        private System.Windows.Forms.CheckBox processtips;
        private System.Windows.Forms.ComboBox createas;
        private Controls.ButtonsNumericTextbox distance;
        private Controls.ButtonsNumericTextbox snapmp;
        private System.Windows.Forms.ToolTip tooltip;
    }
}