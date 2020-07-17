using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Controls;

//JBR Draw Shape options panel
namespace CodeImp.DoomBuilder.BuilderModes
{
	internal partial class DrawShapeOptionsPanel : UserControl
	{
		public event EventHandler OnValueChanged;
		private bool blockEvents;

        public bool PreviewReference { get { return previewreference.Checked; } set { blockEvents = true; previewreference.Checked = value; blockEvents = false; } }
        public bool Ellipse { get { return ellipse.Checked; } set { blockEvents = true; ellipse.Checked = value; blockEvents = false; } }
        public int FirstPointType { get { return firstpointtype.SelectedIndex; } set { blockEvents = true; firstpointtype.SelectedIndex = value; blockEvents = false; } }
        public int CreateAs { get { return createas.SelectedIndex; } set { blockEvents = true; createas.SelectedIndex = value; blockEvents = false; } }
        public bool FrontOutside { get { return frontoutside.Checked; } set { blockEvents = true; frontoutside.Checked = value; blockEvents = false; } }
        public int Sides { get { return sides.GetResult(3); } set { blockEvents = true; SetValue(sides, value, 3, 1000); blockEvents = false; } }
        public int Spikiness { get { return spikiness.GetResult(0); } set { blockEvents = true; SetValue(spikiness, value, 0, 32767); blockEvents = false; } }
        public int SpikingMode { get { return spikingmode.SelectedIndex; } set { blockEvents = true; spikingmode.SelectedIndex = value; blockEvents = false; } }
        public float StartAngle { get { return startangle.GetResultFloat(0f); } set { blockEvents = true; SetValue(startangle, value, 0f, 360f); blockEvents = false; } }
        public float SweepAngle { get { return sweepangle.GetResultFloat(0f); } set { blockEvents = true; SetValue(sweepangle, value, 1f, 360f); blockEvents = false; } }
        public bool LimitAngleQuad { get { return limitanglequad.Checked; } set { blockEvents = true; limitanglequad.Checked = value; blockEvents = false; } }

		public DrawShapeOptionsPanel() 
		{
			InitializeComponent();
            spikingmode.SelectedIndex = 0;
            firstpointtype.SelectedIndex = 0;
            sides.Text = "3";
            spikiness.Text = "0";
            startangle.Text = "0";
            sweepangle.Text = "360";
        }

        private void SetValue(ButtonsNumericTextbox component, int value, int min, int max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            component.Text = value.ToString();
        }

        private void SetValue(ButtonsNumericTextbox component, float value, float min, float max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            component.Text = value.ToString();
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            if (!blockEvents && OnValueChanged != null) OnValueChanged(this, e);
        }

        private void sides_WhenTextChanged(object sender, EventArgs e)
        {
            int numsides = sides.GetResult(3);
            if (numsides < 3)
                sides.Text = "3";
            if (numsides > 1024)
                sides.Text = "1024";
            ValueChanged(this, e);
        }

        private void spikiness_WhenTextChanged(object sender, EventArgs e)
        {
            int spikeperc = sides.GetResult(0);
            if (spikeperc > 32767)
                spikiness.Text = "32767";
            ValueChanged(this, e);
        }

        private void startangle_WhenTextChanged(object sender, EventArgs e)
        {
            float angle = startangle.GetResultFloat(0f);
            if (angle > 360f)
            {
                angle = 360f;
                startangle.Text = "360";
            }
            startanglewheel.Angle = angle;
            ValueChanged(this, e);
        }

        private void sweepangle_WhenTextChanged(object sender, EventArgs e)
        {
            float angle = sweepangle.GetResultFloat(0f);
            if (angle < 1f)
            {
                angle = 1f;
                sweepangle.Text = "1";
            }
            if (angle > 360f)
            {
                angle = 360f;
                sweepangle.Text = "360";
            }
            sweepanglewheel.Angle = angle;
            ValueChanged(this, e);
        }

        private void startanglewheel_AngleChanged(object sender, EventArgs e)
        {
            startangle.Text = startanglewheel.Angle.ToString();
            ValueChanged(this, e);
        }

        private void sweepanglewheel_AngleChanged(object sender, EventArgs e)
        {
            float v = sweepanglewheel.Angle;
            if (v < 0f) v = 0.1f;
            if (v > 360f) v = 360f;
            if (v == 0f) v = 360f;
            sweepangle.Text = v.ToString();
            ValueChanged(this, e);
        }

        private void sides3_Click(object sender, EventArgs e)
        {
            sides.Text = "3";
            ValueChanged(this, e);
        }

        private void sides8_Click(object sender, EventArgs e)
        {
            sides.Text = "8";
            ValueChanged(this, e);
        }

        private void sides24_Click(object sender, EventArgs e)
        {
            sides.Text = "24";
            ValueChanged(this, e);
        }

        private void spike0_Click(object sender, EventArgs e)
        {
            spikiness.Text = "0";
            ValueChanged(this, e);
        }

        private void spike50_Click(object sender, EventArgs e)
        {
            spikiness.Text = "50";
            ValueChanged(this, e);
        }

    }
}
