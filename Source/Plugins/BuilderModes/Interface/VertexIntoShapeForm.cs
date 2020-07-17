#region ================== Namespaces

using System;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using CodeImp.DoomBuilder.Controls;

#endregion

//JBR Vertex into Shape form
namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
    public partial class VertexIntoShapeForm : DelayedForm
    {

        #region ================== Properties

        private bool blockEvents;

        public float RadiusX { get { return GetValue(randomsizing.SelectedIndex, radiusX, RradiusX, rand[0], rand[0]); } }
        public float RadiusY { get { return GetValue(randomsizing.SelectedIndex, radiusY, RradiusY, rand[0], rand[1]); } }
        public bool Ellipse { get { return ellipse.Checked; } }
        public bool RemoveVertices { get { return removevertices.Checked; } }
        public bool PreviewReference { get { return previewreference.Checked; } }
        public int CreateAs { get { return createas.SelectedIndex; } }
        public bool FrontOutside { get { return frontoutside.Checked; } }
        public int Sides { get { return GetValue(randomsides.SelectedIndex, sides, Rsides, rand[2], 3); } }
        public int Spikiness { get { return GetValue(randomspikiness.SelectedIndex, spikiness, Rspikiness, rand[3], 0); } }
        public int SpikingMode { get { return spikingmode.SelectedIndex; } }
        public float StartAngle { get { return GetValue(randomstartangle.SelectedIndex, startangle, Rstartangle, rand[4], rand[4]); } }
        public float SweepAngle { get { return GetValue(randomsweepangle.SelectedIndex, sweepangle, Rsweepangle, rand[5], rand[5]); } }

        Random randomizer;
        int randseed;   // Random seed
        float[] rand;   // To hold random values, 0.0 to 1.0
 
        #endregion

        #region ================== Constructor / Disposer

        public VertexIntoShapeForm()
        {
            InitializeComponent();

            blockEvents = true;
            NewSeed();
            createas.SelectedIndex = 0;
            spikingmode.SelectedIndex = 0;
            radiusX.Text = "256";
            radiusY.Text = "256";
            sides.Text = "3";
            spikiness.Text = "0";
            startangle.Text = "0";
            sweepangle.Text = "360";
            RradiusX.Text = "256";
            RradiusY.Text = "256";
            Rsides.Text = "3";
            Rspikiness.Text = "0";
            Rstartangle.Text = "0";
            Rsweepangle.Text = "360";
            randomsizing.SelectedIndex = 0;
            randomsides.SelectedIndex = 0;
            randomspikiness.SelectedIndex = 0;
            randomstartangle.SelectedIndex = 0;
            randomsweepangle.SelectedIndex = 0;
            blockEvents = false;
        }

        #endregion

        #region ================== Methods

        public void NewSeed()
        {
            randomizer = new Random();
            randseed = randomizer.Next();
            randomizer = new Random(randseed);
            rand = new float[6];
            for (int i = 0; i < rand.Length; i++)
                rand[i] = (float)randomizer.NextDouble();
        }

        public void RestartSeed()
        {
            randomizer = new Random(randseed);
            for (int i = 0; i < rand.Length; i++)
                rand[i] = (float)randomizer.NextDouble();
        }

        public void NextShape()
        {
            for (int i = 0; i < rand.Length; i++)
                rand[i] = (float)randomizer.NextDouble();
        }

        private int GetValue(int randtype, ButtonsNumericTextbox setting, ButtonsNumericTextbox rsetting, float randval, int def)
        {
            if (randtype == 0)
                return setting.GetResult(def);
            int va = setting.GetResult(def);
            int vb = rsetting.GetResult(def);
            if (randtype == 1)
                return (randval >= 0.5f) ? vb : va;
            else
                return va + (int)((float)(vb - va) * randval);
        }

        private float GetValue(int randtype, ButtonsNumericTextbox setting, ButtonsNumericTextbox rsetting, float randval1, float randval2)
        {
            if (randtype == 0)
                return setting.GetResultFloat(0f);
            float va = setting.GetResultFloat(0f);
            float vb = rsetting.GetResultFloat(0f);
            if (randtype == 1)
                return (randval1 >= 0.5f) ? vb : va;
            if (randtype == 2)
                return va + (vb - va) * randval1;
            if (randtype == 3)
                return (randval2 >= 0.5f) ? vb : va;
            else
                return va + (vb - va) * randval2;
        }

        #endregion

        #region ================== Interface

        // Window closing
        private void VertexIntoShapeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // User closing the window?
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Just cancel
                General.Editing.CancelMode();
                e.Cancel = true;
            }
        }

        // This shows the window
        public void Show(Form owner)
        {
            // Position at left-top of owner
            this.Location = new Point(owner.Location.X + 20, owner.Location.Y + 90);

            // Show window
            base.Show(owner);
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            if (!blockEvents) General.Interface.RedrawDisplay();
        }

        private void radiusX_ValueChanged(object sender, EventArgs e)
        {
            if (!ellipse.Checked) radiusY.Text = radiusX.GetResultFloat(0f).ToString();
            ValueChanged(this, e);
        }

        private void radiusY_ValueChanged(object sender, EventArgs e)
        {
            if (!ellipse.Checked) radiusX.Text = radiusY.GetResultFloat(0f).ToString();
            ValueChanged(this, e);
        }

        private void ellipse_CheckedChanged(object sender, EventArgs e)
        {
            if (!ellipse.Checked) radiusY.Text = radiusX.GetResultFloat(0f).ToString();
            ValueChanged(this, e);
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
            int spikeperc = spikiness.GetResult(0);
            if (spikeperc > 32767)
                spikiness.Text = "32767";
            ValueChanged(this, e);
        }

        private void startangle_ValueChanged(object sender, EventArgs e)
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

        private void sweepangle_ValueChanged(object sender, EventArgs e)
        {
            float angle = sweepangle.GetResultFloat(1f);
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

        private void randomize_Click(object sender, EventArgs e)
        {
            NewSeed();
            ValueChanged(this, e);
        }

        private void RradiusX_WhenTextChanged(object sender, EventArgs e)
        {
            if (!ellipse.Checked) RradiusY.Text = RradiusX.GetResultFloat(0f).ToString();
            ValueChanged(this, e);
        }

        private void RradiusY_WhenTextChanged(object sender, EventArgs e)
        {
            if (!ellipse.Checked) RradiusX.Text = RradiusY.GetResultFloat(0f).ToString();
            ValueChanged(this, e);
        }

        private void Rsides_WhenTextChanged(object sender, EventArgs e)
        {
            int numsides = Rsides.GetResult(3);
            if (numsides < 3)
                Rsides.Text = "3";
            if (numsides > 1024)
                Rsides.Text = "1024";
            ValueChanged(this, e);
        }

        private void Rspikiness_WhenTextChanged(object sender, EventArgs e)
        {
            int spikeperc = Rspikiness.GetResult(0);
            if (spikeperc > 32767)
                Rspikiness.Text = "32767";
            ValueChanged(this, e);
        }

        private void Rstartangle_WhenTextChanged(object sender, EventArgs e)
        {
            float angle = Rstartangle.GetResultFloat(0f);
            if (angle > 360f)
            {
                angle = 360f;
                Rstartangle.Text = "360";
            }
            Rstartanglewheel.Angle = angle;
            ValueChanged(this, e);
        }

        private void Rsweepangle_WhenTextChanged(object sender, EventArgs e)
        {
            float angle = Rsweepangle.GetResultFloat(1f);
            if (angle < 1f)
            {
                angle = 1f;
                Rsweepangle.Text = "1";
            }
            if (angle > 360f)
            {
                angle = 360f;
                Rsweepangle.Text = "360";
            }
            Rsweepanglewheel.Angle = angle;
            ValueChanged(this, e);
        }

        private void Rstartanglewheel_AngleChanged(object sender, EventArgs e)
        {
            Rstartangle.Text = Rstartanglewheel.Angle.ToString();
            ValueChanged(this, e);
        }

        private void Rsweepanglewheel_AngleChanged(object sender, EventArgs e)
        {
            float v = Rsweepanglewheel.Angle;
            if (v < 0f) v = 0.1f;
            if (v > 360f) v = 360f;
            if (v == 0f) v = 360f;
            Rsweepangle.Text = v.ToString();
            ValueChanged(this, e);
        }

        private void Rsides3_Click(object sender, EventArgs e)
        {
            Rsides.Text = "3";
            ValueChanged(this, e);
        }

        private void Rsides8_Click(object sender, EventArgs e)
        {
            Rsides.Text = "8";
            ValueChanged(this, e);
        }

        private void Rsides24_Click(object sender, EventArgs e)
        {
            Rsides.Text = "24";
            ValueChanged(this, e);
        }

        private void Rspike0_Click(object sender, EventArgs e)
        {
            Rspikiness.Text = "0";
            ValueChanged(this, e);
        }

        private void Rspike50_Click(object sender, EventArgs e)
        {
            Rspikiness.Text = "50";
            ValueChanged(this, e);
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            // Cancel now
            General.Editing.CancelMode();
        }

        private void apply_Click(object sender, EventArgs e)
        {
            // Apply now
            General.Editing.AcceptMode();
        }

        private void VertexIntoShape_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("e_vertexintoshape.html");
        }

        #endregion
    }
}
