#region Namespaces

//Downloaded from
//Visual C# Kicks - http://vckicks.110mb.com
//The Code Project - http://www.codeproject.com

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Geometry;
using System.ComponentModel;

#endregion

// JBR Float version of the AngleControl!
// Supports changing snap angle
namespace CodeImp.DoomBuilder.GZBuilder.Controls
{
	public partial class AngleControlF : UserControl
	{
		#region Variables

		private float angle;
		private float angleoffset;
        private bool allowLoops = true; //JBR
        private bool startAtOne = true; //JBR
        private int turnThrehold = 16; //JBR
        private float snapangle = 45f; //JBR

		private Rectangle drawRegion;
		private const int drawOffset = 2;
		private const int markScaler = 5;
		private Point origin;

        private Point startClick; //JBR

		//UI colors
		private readonly Color fillColor = SystemColors.Window;
		private readonly Color fillInactiveColor = SystemColors.Control;
		private readonly Color outlineColor = SystemColors.WindowFrame;
		private readonly Color outlineInactiveColor = SystemColors.ControlDarkDark;
		private readonly Color needleColor = SystemColors.ControlText;
		private readonly Color needleInactiveColor = SystemColors.ControlDarkDark;
		private readonly Color marksColor = SystemColors.ActiveBorder;
		private readonly Color marksInactiveColor = SystemColors.ControlDark;
        private readonly Color turnTextColor = SystemColors.ControlText;
        private readonly Color turnTextInactiveColor = SystemColors.ControlDarkDark;

		#endregion

		#region Properties

		public event EventHandler AngleChanged;

		public float Angle { get { return (angle == NO_ANGLE ? NO_ANGLE : angle - angleoffset); } set { angle = (value == NO_ANGLE ? NO_ANGLE : value + angleoffset); this.Refresh(); } }
		public float AngleOffset { get { return angleoffset; } set { angleoffset = value; this.Refresh(); } }
        public const float NO_ANGLE = float.MinValue;

        [Description("Allow loop changing, setting to false will restore old behaviour.")]
        [DefaultValue(true)]
        public bool AllowLoops //JBR
        {
            get { return allowLoops; }
            set
            {
                allowLoops = value;
                if (value)
                {
                    this.toolTip.SetToolTip(this, "Left-click (and drag) to set snapped angle.\r\nRight-click (and drag) to set precise angle.\r\nMiddle-click (and drag) to set loop number.");
                }
                else
                {
                    this.toolTip.SetToolTip(this, "Left-click (and drag) to set snapped angle.\r\nRight-click (and drag) to set precise angle.");
                }
            }
        }

        [Description("Start at loop 1 instead of loop 0, useful for checkpoint number match in SRB2.")]
        [DefaultValue(true)]
        public bool StartAtOne { get { return startAtOne; } set { startAtOne = value; } } //JBR

        [Description("Drag distance in pixels for user to change the loop number.")]
        [DefaultValue(16)]
        public int TurnThrehold { get { return turnThrehold; } set { turnThrehold = value; } } //JBR

        [Description("Snap angle in degrees.")]
        [DefaultValue(45f)]
        public float SnapAngle { get { return snapangle; } set { snapangle = value; } } //JBR

		#endregion

		public AngleControlF() 
		{
			InitializeComponent();
			this.DoubleBuffered = true;
		}

		#region Methods

		private void SetDrawRegion() 
		{
			drawRegion = new Rectangle(0, 0, this.Width, this.Height);
			drawRegion.X += 2;
			drawRegion.Y += 2;
			drawRegion.Width -= 4;
			drawRegion.Height -= 4;

			origin = new Point(drawRegion.Width / 2 + drawOffset, drawRegion.Height / 2 + drawOffset);

			this.Refresh();
		}

		private static PointF DegreesToXY(float degrees, float radius, Point origin) 
		{
			PointF xy = new PointF();
			float radians = degrees * Angle2D.PI / 180.0f;

			xy.X = (float)Math.Cos(radians) * radius + origin.X;
			xy.Y = (float)Math.Sin(-radians) * radius + origin.Y;

			return xy;
		}

		private static float XYToDegrees(Point xy, Point origin) 
		{
			float xDiff = xy.X - origin.X;
			float yDiff = xy.Y - origin.Y;
            float angle = ((float)Math.Atan2(-yDiff, xDiff) * 180f / Angle2D.PI) + 360f;
            if (angle < 0f) angle += 360f;
            if (angle >= 360f) angle -= 360f;
            return angle;
		}

        private static int GetNumLoops(float angle) //JBR
        {
            if (angle == NO_ANGLE) return 0;
            if (angle > 0f) return (int)angle / 360;
            return ((int)angle - 359) / 360;
        }

		#endregion

		#region Events

		private void AngleSelector_Load(object sender, EventArgs e) 
		{
			SetDrawRegion();
		}

		private void AngleSelector_SizeChanged(object sender, EventArgs e) 
		{
			this.Height = this.Width; // Keep it there and keep it square!
			SetDrawRegion();
		}

		protected override void OnPaint(PaintEventArgs e) 
		{
			Graphics g = e.Graphics;

			Pen outline;
			Pen needle;
			Pen marks;
			SolidBrush fill;
			Brush center;
            Brush text;

			if(this.Enabled) 
			{
				outline = new Pen(outlineColor, 2.0f);
				fill = new SolidBrush(fillColor);
				needle = new Pen(needleColor);
				center = new SolidBrush(needleColor);
				marks = new Pen(marksColor);
                text = new SolidBrush(turnTextColor);
            } 
			else 
			{
                outline = new Pen(outlineInactiveColor, 2.0f);
				fill = new SolidBrush(fillInactiveColor);
				needle = new Pen(needleInactiveColor);
				center = new SolidBrush(needleInactiveColor);
				marks = new Pen(marksInactiveColor);
                text = new SolidBrush(turnTextInactiveColor);
            }

			Rectangle originSquare = new Rectangle(origin.X - 1, origin.Y - 1, 3, 3);

			//Draw circle
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.DrawEllipse(outline, drawRegion);
			g.FillEllipse(fill, drawRegion);

			// Draw angle marks
			int offset = this.Height / markScaler;
            for (float di = 0f; di < 360f; di += snapangle)
            {
                PointF p1 = DegreesToXY(di, origin.X - 6, origin);
                PointF p2 = DegreesToXY(di, origin.X - offset, origin);
                g.DrawLine(marks, p1, p2);
            }

            //JBR Draw loop number
            if (allowLoops)
            {
                int loop = GetNumLoops(angle);
                if (startAtOne && loop >= 0) loop++;
                string loopStr = "↺" + loop.ToString();
                StringFormat strFormat = new StringFormat();
                strFormat.LineAlignment = StringAlignment.Far;
                strFormat.Alignment = StringAlignment.Far;
                if (loop != (startAtOne ? 1 : 0))
                {
                    g.DrawString(loopStr, Font, fill, drawRegion.Right - 1, drawRegion.Bottom - 1, strFormat);
                    g.DrawString(loopStr, Font, fill, drawRegion.Right + 1, drawRegion.Bottom - 1, strFormat);
                    g.DrawString(loopStr, Font, fill, drawRegion.Right - 1, drawRegion.Bottom + 1, strFormat);
                    g.DrawString(loopStr, Font, fill, drawRegion.Right + 1, drawRegion.Bottom + 1, strFormat);
                    g.DrawString(loopStr, Font, text, drawRegion.Right, drawRegion.Bottom, strFormat);
                }
            }

			// Draw needle
			if(angle != NO_ANGLE) 
			{
				PointF anglePoint = DegreesToXY(angle, origin.X - 4, origin);
				g.DrawLine(needle, origin, anglePoint);
			}

			g.SmoothingMode = SmoothingMode.HighSpeed; //Make the square edges sharp
			g.FillRectangle(center, originSquare);

			//mxd. Dispose brushes
			fill.Dispose();
			center.Dispose();
			outline.Dispose();
			marks.Dispose();
			needle.Dispose();

			base.OnPaint(e);
		}

        private void AngleSelector_MouseDown(object sender, MouseEventArgs e) //JBR supports looping
		{
            startClick = new Point(e.X, e.Y);

            if (e.Button == MouseButtons.Middle) return;
            float thisAngle = XYToDegrees(startClick, origin);

			if(e.Button == MouseButtons.Left) 
			{
				thisAngle = (float)Math.Round(thisAngle / snapangle) * snapangle;
				if (thisAngle == 360f) thisAngle = 0f;
			}
            if (allowLoops) thisAngle += (float)GetNumLoops(angle) * 360f;

            if(thisAngle != angle)
			{
				angle = thisAngle;
				if(!this.DesignMode && AngleChanged != null) AngleChanged(this, EventArgs.Empty); //Raise event
				this.Refresh();
			}
		}

        private void AngleSelector_MouseMove(object sender, MouseEventArgs e) //JBR supports looping
		{
            if (allowLoops && e.Button == MouseButtons.Middle)
            {
                int dist = (e.X - startClick.X) - (e.Y - startClick.Y);
                if (dist < -turnThrehold || dist >= turnThrehold)
                {
                    startClick = new Point(e.X, e.Y);
                    float thisAngle = angle + 360f;
                    if (dist < 0) thisAngle = angle - 360f;

                    if (thisAngle != angle)
                    {
                        angle = thisAngle;
                        if (!this.DesignMode && AngleChanged != null) AngleChanged(this, EventArgs.Empty); //Raise event
                        this.Refresh();
                    }
                }
            }
			if(e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) 
			{
                startClick = new Point(e.X, e.Y);
				float thisAngle = XYToDegrees(startClick, origin);

				if(e.Button == MouseButtons.Left) 
				{
					thisAngle = (float)Math.Round(thisAngle / snapangle) * snapangle;
					if (thisAngle == 360f) thisAngle = 0f;
				}
                if (allowLoops) thisAngle += (float)GetNumLoops(angle) * 360f;

				if(thisAngle != angle) 
				{
					angle = thisAngle;
					if(!this.DesignMode && AngleChanged != null) AngleChanged(this, EventArgs.Empty); //Raise event
					this.Refresh();
				}
			}
		}

		#endregion
	}
}
