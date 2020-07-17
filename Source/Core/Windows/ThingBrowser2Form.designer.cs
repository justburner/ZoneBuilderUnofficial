namespace CodeImp.DoomBuilder.Windows
{
    partial class ThingBrowser2Form
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
            System.Windows.Forms.GroupBox groupBox2;
            this.anglecontrol = new CodeImp.DoomBuilder.GZBuilder.Controls.AngleControl();
            this.anglelabel = new System.Windows.Forms.Label();
            this.posZ = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.angle = new CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox();
            this.zlabel = new System.Windows.Forms.Label();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            this.apply = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.applypanel = new System.Windows.Forms.Panel();
            this.panel = new System.Windows.Forms.Panel();
            this.typegroup = new System.Windows.Forms.GroupBox();
            this.thingslist = new CodeImp.DoomBuilder.Controls.ThingBrowserControl();
            this.settingsgroup = new System.Windows.Forms.GroupBox();
            this.flagsvallabel = new System.Windows.Forms.Label();
            this.flagsvalue = new CodeImp.DoomBuilder.Controls.NumericTextbox();
            this.missingflags = new System.Windows.Forms.PictureBox();
            this.flags = new CodeImp.DoomBuilder.Controls.CheckboxArrayControl();
            groupBox2 = new System.Windows.Forms.GroupBox();
            groupBox2.SuspendLayout();
            this.applypanel.SuspendLayout();
            this.panel.SuspendLayout();
            this.typegroup.SuspendLayout();
            this.settingsgroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.missingflags)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(this.anglecontrol);
            groupBox2.Controls.Add(this.anglelabel);
            groupBox2.Controls.Add(this.posZ);
            groupBox2.Controls.Add(this.angle);
            groupBox2.Controls.Add(this.zlabel);
            groupBox2.Location = new System.Drawing.Point(279, 242);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(271, 134);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            // 
            // anglecontrol
            // 
            this.anglecontrol.Angle = 0;
            this.anglecontrol.AngleOffset = 0;
            this.anglecontrol.Location = new System.Drawing.Point(149, 12);
            this.anglecontrol.Name = "anglecontrol";
            this.anglecontrol.Size = new System.Drawing.Size(116, 116);
            this.anglecontrol.TabIndex = 2;
            this.anglecontrol.AngleChanged += new System.EventHandler(this.anglecontrol_AngleChanged);
            // 
            // anglelabel
            // 
            this.anglelabel.Location = new System.Drawing.Point(9, 23);
            this.anglelabel.Name = "anglelabel";
            this.anglelabel.Size = new System.Drawing.Size(50, 14);
            this.anglelabel.TabIndex = 0;
            this.anglelabel.Text = "Angle:";
            this.anglelabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // posZ
            // 
            this.posZ.AllowDecimal = false;
            this.posZ.AllowNegative = true;
            this.posZ.AllowRelative = true;
            this.posZ.ButtonStep = 8;
            this.posZ.ButtonStepBig = 16F;
            this.posZ.ButtonStepFloat = 1F;
            this.posZ.ButtonStepSmall = 1F;
            this.posZ.ButtonStepsUseModifierKeys = true;
            this.posZ.ButtonStepsWrapAround = false;
            this.posZ.Location = new System.Drawing.Point(61, 104);
            this.posZ.Name = "posZ";
            this.posZ.Size = new System.Drawing.Size(82, 24);
            this.posZ.StepValues = null;
            this.posZ.TabIndex = 4;
            this.posZ.WhenTextChanged += new System.EventHandler(this.posZ_WhenTextChanged);
            // 
            // angle
            // 
            this.angle.AllowDecimal = false;
            this.angle.AllowNegative = true;
            this.angle.AllowRelative = true;
            this.angle.ButtonStep = 5;
            this.angle.ButtonStepBig = 15F;
            this.angle.ButtonStepFloat = 1F;
            this.angle.ButtonStepSmall = 1F;
            this.angle.ButtonStepsUseModifierKeys = true;
            this.angle.ButtonStepsWrapAround = false;
            this.angle.Location = new System.Drawing.Point(61, 19);
            this.angle.Name = "angle";
            this.angle.Size = new System.Drawing.Size(82, 24);
            this.angle.StepValues = null;
            this.angle.TabIndex = 1;
            this.angle.WhenTextChanged += new System.EventHandler(this.angle_WhenTextChanged);
            // 
            // zlabel
            // 
            this.zlabel.Location = new System.Drawing.Point(5, 109);
            this.zlabel.Name = "zlabel";
            this.zlabel.Size = new System.Drawing.Size(50, 14);
            this.zlabel.TabIndex = 3;
            this.zlabel.Text = "Height:";
            this.zlabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // apply
            // 
            this.apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.apply.Location = new System.Drawing.Point(320, 4);
            this.apply.Name = "apply";
            this.apply.Size = new System.Drawing.Size(112, 25);
            this.apply.TabIndex = 0;
            this.apply.Text = "OK";
            this.apply.UseVisualStyleBackColor = true;
            this.apply.Click += new System.EventHandler(this.apply_Click);
            // 
            // cancel
            // 
            this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(438, 4);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(112, 25);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // applypanel
            // 
            this.applypanel.Controls.Add(this.cancel);
            this.applypanel.Controls.Add(this.apply);
            this.applypanel.Location = new System.Drawing.Point(12, 401);
            this.applypanel.Name = "applypanel";
            this.applypanel.Size = new System.Drawing.Size(553, 32);
            this.applypanel.TabIndex = 1;
            // 
            // panel
            // 
            this.panel.BackColor = System.Drawing.SystemColors.Window;
            this.panel.Controls.Add(this.typegroup);
            this.panel.Controls.Add(groupBox2);
            this.panel.Controls.Add(this.settingsgroup);
            this.panel.Location = new System.Drawing.Point(12, 12);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(553, 378);
            this.panel.TabIndex = 0;
            // 
            // typegroup
            // 
            this.typegroup.Controls.Add(this.thingslist);
            this.typegroup.Location = new System.Drawing.Point(4, 3);
            this.typegroup.Name = "typegroup";
            this.typegroup.Size = new System.Drawing.Size(269, 373);
            this.typegroup.TabIndex = 0;
            this.typegroup.TabStop = false;
            this.typegroup.Text = " Thing ";
            // 
            // thingslist
            // 
            this.thingslist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.thingslist.Location = new System.Drawing.Point(9, 13);
            this.thingslist.Margin = new System.Windows.Forms.Padding(6);
            this.thingslist.Name = "thingslist";
            this.thingslist.Size = new System.Drawing.Size(251, 357);
            this.thingslist.TabIndex = 0;
            this.thingslist.UseMultiSelection = false;
            this.thingslist.OnTypeChanged += new CodeImp.DoomBuilder.Controls.ThingBrowserControl.TypeChangedDeletegate(this.thingtype_OnTypeChanged);
            this.thingslist.OnTypeDoubleClicked += new CodeImp.DoomBuilder.Controls.ThingBrowserControl.TypeDoubleClickDeletegate(this.thingtype_OnTypeDoubleClicked);
            // 
            // settingsgroup
            // 
            this.settingsgroup.Controls.Add(this.flagsvallabel);
            this.settingsgroup.Controls.Add(this.flagsvalue);
            this.settingsgroup.Controls.Add(this.missingflags);
            this.settingsgroup.Controls.Add(this.flags);
            this.settingsgroup.Location = new System.Drawing.Point(279, 3);
            this.settingsgroup.Name = "settingsgroup";
            this.settingsgroup.Size = new System.Drawing.Size(271, 233);
            this.settingsgroup.TabIndex = 1;
            this.settingsgroup.TabStop = false;
            this.settingsgroup.Text = " Settings ";
            // 
            // flagsvallabel
            // 
            this.flagsvallabel.Location = new System.Drawing.Point(86, 198);
            this.flagsvallabel.Name = "flagsvallabel";
            this.flagsvallabel.Size = new System.Drawing.Size(75, 14);
            this.flagsvallabel.TabIndex = 1;
            this.flagsvallabel.Text = "Flags value:";
            this.flagsvallabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flagsvalue
            // 
            this.flagsvalue.AllowDecimal = false;
            this.flagsvalue.AllowNegative = false;
            this.flagsvalue.AllowRelative = false;
            this.flagsvalue.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.flagsvalue.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.flagsvalue.Location = new System.Drawing.Point(177, 196);
            this.flagsvalue.Name = "flagsvalue";
            this.flagsvalue.Size = new System.Drawing.Size(72, 20);
            this.flagsvalue.TabIndex = 2;
            this.flagsvalue.TextChanged += new System.EventHandler(this.flagsvalue_TextChanged);
            // 
            // missingflags
            // 
            this.missingflags.BackColor = System.Drawing.SystemColors.Window;
            this.missingflags.Image = global::CodeImp.DoomBuilder.Properties.Resources.Warning;
            this.missingflags.Location = new System.Drawing.Point(55, -2);
            this.missingflags.Name = "missingflags";
            this.missingflags.Size = new System.Drawing.Size(16, 16);
            this.missingflags.TabIndex = 5;
            this.missingflags.TabStop = false;
            this.missingflags.Visible = false;
            // 
            // flags
            // 
            this.flags.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.flags.AutoScroll = true;
            this.flags.Columns = 2;
            this.flags.Location = new System.Drawing.Point(14, 19);
            this.flags.Name = "flags";
            this.flags.Size = new System.Drawing.Size(251, 211);
            this.flags.TabIndex = 0;
            this.flags.VerticalSpacing = 1;
            this.flags.OnValueChanged += new System.EventHandler(this.flags_OnValueChanged);
            // 
            // ThingBrowser2Form
            // 
            this.AcceptButton = this.apply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(577, 447);
            this.Controls.Add(this.applypanel);
            this.Controls.Add(this.panel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ThingBrowser2Form";
            this.Opacity = 1D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Choose Thing Type";
            this.Shown += new System.EventHandler(this.ThingBrowserAdvForm_Shown);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.ThingBrowserAdvForm_HelpRequested);
            groupBox2.ResumeLayout(false);
            this.applypanel.ResumeLayout(false);
            this.panel.ResumeLayout(false);
            this.typegroup.ResumeLayout(false);
            this.settingsgroup.ResumeLayout(false);
            this.settingsgroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.missingflags)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button apply;
        private System.Windows.Forms.GroupBox settingsgroup;
        private CodeImp.DoomBuilder.Controls.CheckboxArrayControl flags;
        private System.Windows.Forms.Label zlabel;
        private CodeImp.DoomBuilder.Controls.ThingBrowserControl thingslist;
        private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox angle;
        private CodeImp.DoomBuilder.Controls.ButtonsNumericTextbox posZ;
        private System.Windows.Forms.PictureBox missingflags;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.GroupBox typegroup;
        private System.Windows.Forms.Panel applypanel;
        private System.Windows.Forms.Label flagsvallabel;
        private Controls.NumericTextbox flagsvalue;
        private GZBuilder.Controls.AngleControl anglecontrol;
        private System.Windows.Forms.Label anglelabel;
    }
}