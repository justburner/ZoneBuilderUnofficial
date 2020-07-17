
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Types;

#endregion

//JBR A little more advanced version of ThingBrowserForm
namespace CodeImp.DoomBuilder.Windows
{
	/// <summary>
	/// Dialog window that allows setting up Thing properties.
	/// </summary>
	public partial class ThingBrowser2Form : DelayedForm
	{
        #region ================== Properties

        public int SelectedType { get { return s_thingType; } }
        public int SelectedParameter { get { return s_parameter; } }
        public Dictionary<string, bool> SelectedFlags { get { return s_flags; } } // oh no... not read-only at all but atleast it's fast.
        public float SelectedAngleDoom { get { return s_angleDoom; } }
        public float SelectedPosZ { get { return s_posZ; } }

        #endregion

        #region ================== Variables

        private bool m_ok;
        private static int s_thingType = 1;
        private static int s_parameter = 0;
        private static Dictionary<string, bool> s_flags = new Dictionary<string, bool>();
        private static int s_angleDoom = 0;
        private static float s_posZ = 0f;

        private ThingTypeInfo thinginfo;
		private bool preventchanges;
        private bool flagsvalue_ignore = false;
        private bool flags_ignore = false;

        //JBR Hold result
        public struct Result
        {
            public readonly bool OK;
            public readonly int Type;
            public readonly int Parameter;
            public readonly Dictionary<string, bool> Flags;
            public readonly int AngleDoom;
            public readonly float Z;

            public Result(bool ok, int type, int parameter, Dictionary<string, bool> flags, int angledoom, float z)
            {
                this.OK = ok;
                this.Type = type;
                this.Parameter = parameter;
                this.Flags = flags;
                this.AngleDoom = angledoom;
                this.Z = z;
            }

            public void Apply(Thing t)
            {
                t.SRB2Type = this.Type;
                t.Parameter = this.Parameter;
                foreach (KeyValuePair<string, bool> flag in this.Flags)
                {
                    t.SetFlag(flag.Key, flag.Value);
                }
                t.Move(t.Position.x, t.Position.y, this.Z);
                t.Rotate(this.AngleDoom);
            }
        }

		#endregion

		#region ================== Constructor

		// Constructor
		public ThingBrowser2Form()
		{
			// Initialize
			InitializeComponent();
            m_ok = false;

            preventchanges = true;

			// Fill flags list
			foreach(KeyValuePair<string, string> tf in General.Map.Config.ThingFlags)
				flags.Add(tf.Value, tf.Key);
			
			// Thing height?
			posZ.Visible = General.Map.FormatInterface.HasThingHeight;
			zlabel.Visible = General.Map.FormatInterface.HasThingHeight;

			//mxd. Decimals allowed?
			if(General.Map.FormatInterface.VertexDecimals > 0) 
			{
				posZ.AllowDecimal = true;
			}

            // Setup types list
			thingslist.Setup();

            // Set type
            thingslist.SelectType(s_thingType, s_parameter);

            // Flags
            ThingTypeInfo ti = General.Map.Data.GetThingInfoEx(s_thingType);
            IDictionary<string, string> newFlags = (ti == null || ti.Flags.Count == 0) ? General.Map.Config.ThingFlags : ti.Flags;
            flags.UpdateCheckboxes(newFlags);
            anglelabel.Text = (ti == null) ? "Angle:" : ti.AngleText + ":";
            flagsvallabel.Text = (ti == null) ? "Flags value:" : ti.FlagsValueText + ":";
            foreach (CheckBox c in flags.Checkboxes)
                if (s_flags.ContainsKey(c.Tag.ToString())) c.Checked = s_flags[c.Tag.ToString()];

            // Coordination
            angle.Text = s_angleDoom.ToString();

            // Z Position
            posZ.Text = s_posZ.ToString();
            posZ.AllowNegative = General.Map.FormatInterface.MinThingHeight < 0;

            // Update...
            flags.UpdateCheckboxes(newFlags);
            if (ti != null)
            {
                flagsvallabel.Text = ti.FlagsValueText + ":";
            }

            preventchanges = false;

            angle_WhenTextChanged(angle, EventArgs.Empty);
            flags_OnValueChanged(flags, EventArgs.Empty);
            flagsvalue.Text = evaluateFlagsValue();
		}

        #endregion

        #region ================== Methods

        private string evaluateFlagsValue()
        {
            int i = 1;
            int value = 0;
            foreach (CheckBox box in flags.Checkboxes)
            {
                if (box.CheckState == CheckState.Indeterminate) return "";
                if (box.CheckState == CheckState.Checked) value += i;
                i *= 2;
            }

            if (General.Map.SRB2)
            {
                float z = posZ.GetResultFloat(s_posZ);
                return (value + ((int)z << 4)).ToString();
            }
            else return value.ToString();
        }

        public Result GetThingResult()
        {
            return new Result(m_ok, s_thingType, s_parameter, s_flags, s_angleDoom, s_posZ);
        }

        // This browses for a thing type
        // Returns the new thing type or the same thing type when cancelled
        public static Result BrowseThing(IWin32Window owner)
        {
            ThingBrowser2Form f = new ThingBrowser2Form();
            DialogResult result = f.ShowDialog(owner);
            Result tr = f.GetThingResult();
            f.Dispose();
            return tr;
        }
	
		#endregion

		#region ================== Events

		//mxd
		private void thingtype_OnTypeDoubleClicked() 
		{
			apply_Click(this, EventArgs.Empty);
		}
		
		// Angle text changes
		private void angle_WhenTextChanged(object sender, EventArgs e)
		{
			if(preventchanges) return;
			preventchanges = true;
			anglecontrol.Angle = angle.GetResult(GZBuilder.Controls.AngleControl.NO_ANGLE);
			preventchanges = false;
		}

		//mxd. Angle control clicked
		private void anglecontrol_AngleChanged(object sender, EventArgs e) 
		{
			if(preventchanges) return;
			angle.Text = anglecontrol.Angle.ToString();
		}

		// Apply clicked
		private void apply_Click(object sender, EventArgs e)
		{
			List<string> defaultflags = new List<string>();

			// Verify the type
			if(((thingslist.GetFullType(0) < General.Map.FormatInterface.MinThingType) || (thingslist.GetFullType(0) > General.Map.FormatInterface.MaxThingType)))
			{
				General.ShowWarningMessage("Thing type must be between " + General.Map.FormatInterface.MinThingType + " and " + General.Map.FormatInterface.MaxThingType + ".", MessageBoxButtons.OK);
				return;
			}

            // Apply all flags
            s_thingType = thingslist.GetResult(s_thingType);
            s_parameter = thingslist.GetFullType(s_thingType) / 4096;
            s_angleDoom = angle.GetResult(s_angleDoom);
            s_posZ = posZ.GetResultFloat(s_posZ);
            s_flags = new Dictionary<string, bool>();
            foreach (CheckBox c in flags.Checkboxes)
            {
                s_flags.Add(c.Tag.ToString(), c.Checked);
            }
            m_ok = true;

			// Done
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Be gone
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		//mxd
		private void ThingBrowserAdvForm_Shown(object sender, EventArgs e)
		{
			thingslist.FocusTextbox();
		}

		// Help
		private void ThingBrowserAdvForm_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			General.ShowHelp("w_thingedit.html");
			hlpevent.Handled = true;
		}

        private void posZ_WhenTextChanged(object sender, EventArgs e)
        {
            if (preventchanges) return;
            if (!flagsvalue_ignore)
            {
                flagsvalue_ignore = true;
                flagsvalue.Text = evaluateFlagsValue();
                flagsvalue_ignore = false;
            }
        }

        private void flagsvalue_TextChanged(object sender, EventArgs e)
        {
            if (!flagsvalue_ignore && !string.IsNullOrEmpty(flagsvalue.Text))
            {
                flagsvalue_ignore = true;
                int value = General.Clamp(flagsvalue.GetResult(0), 0, 0xFFFF);
                int i = 1;
                flags_ignore = true;
                foreach (CheckBox box in flags.Checkboxes)
                {
                    box.Checked = ((value & i) == i);
                    i *= 2;
                }
                flags_ignore = false;
                flags_OnValueChanged(this, null);

                if (General.Map.SRB2)
                {
                    int z = value >> 4;
                    posZ.Text = z.ToString();
                }
                flagsvalue_ignore = false;
            }
        }

        // Selected type changes
        private void thingtype_OnTypeChanged(ThingTypeInfo value) 
		{
			thinginfo = value;

            //mxd. Update things
			if(preventchanges) return;

			if(((thingslist.GetResult(0) < General.Map.FormatInterface.MinThingType) || (thingslist.GetResult(0) > General.Map.FormatInterface.MaxThingType)))
				return;

            IDictionary<string, string> newFlags = (thinginfo == null || thinginfo.Flags.Count == 0) ? General.Map.Config.ThingFlags : thinginfo.Flags;
            flags.UpdateCheckboxes(newFlags);
            anglelabel.Text = (thinginfo == null) ? "Angle:" : thinginfo.AngleText + ":";
            flagsvallabel.Text = (thinginfo == null) ? "Flags value:" : thinginfo.FlagsValueText + ":";
		}

		//mxd
		private void flags_OnValueChanged(object sender, EventArgs e) 
		{
			if(flags_ignore || preventchanges) return;

			// Gather enabled flags
			HashSet<string> activeflags = new HashSet<string>();
			foreach(CheckBox cb in flags.Checkboxes)
			{
				if(cb.CheckState != CheckState.Unchecked) activeflags.Add(cb.Tag.ToString());
			}

			// Check em
			List<string> warnings = ThingFlagsCompare.CheckFlags(activeflags);
			if(warnings.Count > 0) 
			{
				//got missing flags
				tooltip.SetToolTip(missingflags, string.Join(Environment.NewLine, warnings.ToArray()));
				missingflags.Visible = true;
				settingsgroup.ForeColor = Color.DarkRed;
				return;
			}

			//everything is OK
			missingflags.Visible = false;
			settingsgroup.ForeColor = SystemColors.ControlText;
            if (!flagsvalue_ignore)
            {
                flagsvalue_ignore = true;
                flagsvalue.Text = evaluateFlagsValue();
                flagsvalue_ignore = false;
            }
        }
        
        #endregion
	}
}