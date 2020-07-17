namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    partial class VertexSlopeAssistTagForm
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
            this.apply = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.newtag = new System.Windows.Forms.Button();
            this.tagID = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(15, 17);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(64, 13);
            label1.TabIndex = 1;
            label1.Text = "New Tag #:";
            this.tooltip.SetToolTip(label1, "Tag number to be used for the vertex slope, should be between 1 to 65535.");
            // 
            // apply
            // 
            this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.apply.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.apply.Location = new System.Drawing.Point(12, 48);
            this.apply.Name = "apply";
            this.apply.Size = new System.Drawing.Size(70, 25);
            this.apply.TabIndex = 9;
            this.apply.Text = "OK";
            this.apply.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(89, 48);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(70, 25);
            this.cancel.TabIndex = 10;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // newtag
            // 
            this.newtag.Location = new System.Drawing.Point(163, 12);
            this.newtag.Name = "newtag";
            this.newtag.Size = new System.Drawing.Size(48, 23);
            this.newtag.TabIndex = 3;
            this.newtag.Text = "New";
            this.tooltip.SetToolTip(this.newtag, "Find a new unused tag.");
            this.newtag.UseVisualStyleBackColor = true;
            this.newtag.Click += new System.EventHandler(this.newtag_Click);
            // 
            // tagID
            // 
            this.tagID.AllowDecimal = false;
            this.tagID.AllowNegative = false;
            this.tagID.AllowRelative = false;
            this.tagID.ButtonStep = 1;
            this.tagID.ButtonStepBig = 100F;
            this.tagID.ButtonStepFloat = 1F;
            this.tagID.ButtonStepSmall = 10F;
            this.tagID.ButtonStepsUseModifierKeys = true;
            this.tagID.ButtonStepsWrapAround = false;
            this.tagID.Location = new System.Drawing.Point(85, 12);
            this.tagID.Name = "tagID";
            this.tagID.Size = new System.Drawing.Size(72, 24);
            this.tagID.StepValues = null;
            this.tagID.TabIndex = 2;
            // 
            // tooltip
            // 
            this.tooltip.AutoPopDelay = 10000;
            this.tooltip.InitialDelay = 500;
            this.tooltip.ReshowDelay = 100;
            // 
            // VertexSlopeAssistTagForm
            // 
            this.AcceptButton = this.apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(230, 85);
            this.Controls.Add(this.tagID);
            this.Controls.Add(this.newtag);
            this.Controls.Add(label1);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.apply);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VertexSlopeAssistTagForm";
            this.Opacity = 1D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Vertex Slope Assistant (Tag)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VertexSlopeAssistRetagForm_FormClosing);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.VertexSlopeAssistRetagForm_HelpRequested);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button apply;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button newtag;
        private Controls.ButtonsNumericTextbox tagID;
        private System.Windows.Forms.ToolTip tooltip;
    }
}