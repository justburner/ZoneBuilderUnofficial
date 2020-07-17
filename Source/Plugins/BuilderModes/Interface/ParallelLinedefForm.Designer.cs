namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    partial class ParallelLinedefForm
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
            System.Windows.Forms.Label label2;
            this.cancel = new System.Windows.Forms.Button();
            this.apply = new System.Windows.Forms.Button();
            this.closeopenpath = new System.Windows.Forms.CheckBox();
            this.backwards = new System.Windows.Forms.CheckBox();
            this.createas = new System.Windows.Forms.ComboBox();
            this.numopentracks = new System.Windows.Forms.Label();
            this.numclosetracks = new System.Windows.Forms.Label();
            this.distance = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            label1 = new System.Windows.Forms.Label();
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
            this.tooltip.SetToolTip(label1, "Set the distance between selected linedef(s) and parallel linedef(s).\r\n");
            // 
            // cancel
            // 
            this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(89, 163);
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
            this.apply.Location = new System.Drawing.Point(12, 163);
            this.apply.Name = "apply";
            this.apply.Size = new System.Drawing.Size(70, 25);
            this.apply.TabIndex = 8;
            this.apply.Text = "OK";
            this.apply.UseVisualStyleBackColor = true;
            this.apply.Click += new System.EventHandler(this.apply_Click);
            // 
            // closeopenpath
            // 
            this.closeopenpath.AutoSize = true;
            this.closeopenpath.Checked = true;
            this.closeopenpath.CheckState = System.Windows.Forms.CheckState.Checked;
            this.closeopenpath.Location = new System.Drawing.Point(20, 80);
            this.closeopenpath.Name = "closeopenpath";
            this.closeopenpath.Size = new System.Drawing.Size(133, 17);
            this.closeopenpath.TabIndex = 4;
            this.closeopenpath.Text = "Close Open-Path Ends";
            this.tooltip.SetToolTip(this.closeopenpath, "Attach linedefs between original paths and open-paths at both ends.\r\nOnly availab" +
                    "le for linedefs elements and doesn\'t affect close-paths.");
            this.closeopenpath.UseVisualStyleBackColor = true;
            this.closeopenpath.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // backwards
            // 
            this.backwards.AutoSize = true;
            this.backwards.Location = new System.Drawing.Point(20, 103);
            this.backwards.Name = "backwards";
            this.backwards.Size = new System.Drawing.Size(126, 17);
            this.backwards.TabIndex = 5;
            this.backwards.Text = "Backwards / Outside";
            this.tooltip.SetToolTip(this.backwards, "When checked, it creates parallel path on the opposite side.");
            this.backwards.UseVisualStyleBackColor = true;
            this.backwards.CheckedChanged += new System.EventHandler(this.ValueChanged);
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
            this.tooltip.SetToolTip(this.createas, "Select which type of element to create.\r\nCheck help (F1) for more information.");
            this.createas.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
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
            // numopentracks
            // 
            this.numopentracks.AutoSize = true;
            this.numopentracks.Location = new System.Drawing.Point(12, 128);
            this.numopentracks.Name = "numopentracks";
            this.numopentracks.Size = new System.Drawing.Size(78, 13);
            this.numopentracks.TabIndex = 6;
            this.numopentracks.Text = "0 Open-Path(s)";
            this.tooltip.SetToolTip(this.numopentracks, "Number of open-paths found.");
            // 
            // numclosetracks
            // 
            this.numclosetracks.AutoSize = true;
            this.numclosetracks.Location = new System.Drawing.Point(12, 142);
            this.numclosetracks.Name = "numclosetracks";
            this.numclosetracks.Size = new System.Drawing.Size(78, 13);
            this.numclosetracks.TabIndex = 7;
            this.numclosetracks.Text = "0 Close-Path(s)";
            this.tooltip.SetToolTip(this.numclosetracks, "Number of close-paths found.");
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
            // tooltip
            // 
            this.tooltip.AutoPopDelay = 10000;
            this.tooltip.InitialDelay = 500;
            this.tooltip.ReshowDelay = 100;
            // 
            // ParallelLinedefForm
            // 
            this.AcceptButton = this.apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(170, 200);
            this.Controls.Add(this.distance);
            this.Controls.Add(this.numclosetracks);
            this.Controls.Add(this.numopentracks);
            this.Controls.Add(this.createas);
            this.Controls.Add(label2);
            this.Controls.Add(this.backwards);
            this.Controls.Add(this.closeopenpath);
            this.Controls.Add(label1);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.apply);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ParallelLinedefForm";
            this.Opacity = 1D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Parallel (Linedef)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ParallelLinedefForm_FormClosing);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.ParallelLinedefForm_HelpRequested);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button apply;
        private System.Windows.Forms.CheckBox closeopenpath;
        private System.Windows.Forms.CheckBox backwards;
        private System.Windows.Forms.ComboBox createas;
        private System.Windows.Forms.Label numopentracks;
        private System.Windows.Forms.Label numclosetracks;
        private Controls.ButtonsNumericTextbox distance;
        private System.Windows.Forms.ToolTip tooltip;
    }
}