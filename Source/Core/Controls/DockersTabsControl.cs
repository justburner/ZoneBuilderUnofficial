
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	internal class DockersTabsControl : TabControl
	{
		#region ================== Constants

		#endregion

		#region ================== Variables
		
		private int highlighttab;
		private readonly StringFormat stringformat;
		
		#endregion

		#region ================== Constructor

		// Constructor
		public DockersTabsControl()
		{
			if(VisualStyleInformation.IsSupportedByOS && VisualStyleInformation.IsEnabledByUser)
			{
				// Style settings
				this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
				this.DrawMode = TabDrawMode.OwnerDrawFixed;
			}
			
			stringformat = new StringFormat {Alignment = StringAlignment.Center, HotkeyPrefix = HotkeyPrefix.None, LineAlignment = StringAlignment.Center};
			highlighttab = -1;
		}
		
		#endregion

		#region ================== Methods

		//mxd
		private void DrawTab(Graphics graphics, int index)
		{
			Rectangle bounds = this.GetTabRect(index);
			VisualStyleRenderer renderer;
			bool selected = (index == this.SelectedIndex);

			// Transform bounds?
			switch(this.Alignment)
			{
				case TabAlignment.Right:
					bounds = new Rectangle((selected ? bounds.X - 1 : bounds.X + 1), bounds.Y, bounds.Height, bounds.Width);
					break;

				case TabAlignment.Left:
					bounds = new Rectangle(bounds.X, bounds.Y, bounds.Height, bounds.Width);
					break;

				default:
					if(selected) bounds.Y -= 2;
					break;
			}

			if(selected)
			{
				bounds.Height += 2;
				renderer = new VisualStyleRenderer(VisualStyleElement.Tab.TabItem.Pressed);
			}
			else
			{
				renderer = new VisualStyleRenderer(index == highlighttab ? VisualStyleElement.Tab.TabItem.Hot : VisualStyleElement.Tab.TabItem.Normal);
			}

			Bitmap drawimage = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

			// Draw tab
			using(Graphics g = Graphics.FromImage(drawimage))
			{
				Rectangle bgbounds = new Rectangle(0, 0, bounds.Width, bounds.Height + 1);
				bgbounds.Inflate(-1, 0);
				renderer.DrawBackground(g, bgbounds);
				g.DrawString(this.TabPages[index].Text, this.Font, SystemBrushes.ControlText, new RectangleF(bgbounds.Location, bounds.Size), stringformat);
			}

			// Rotate image?
			switch(this.Alignment)
			{
				case TabAlignment.Right:
					drawimage.RotateFlip(RotateFlipType.Rotate270FlipNone);
					break;

				case TabAlignment.Left:
					drawimage.RotateFlip(RotateFlipType.Rotate90FlipNone);
					break;
			}

			graphics.DrawImage(drawimage, bounds.X, bounds.Y);
		}
		
		#endregion
		
		#region ================== Events
		
		//mxd. Redrawing needed
		protected override void OnPaint(PaintEventArgs e)
		{
			if(VisualStyleInformation.IsSupportedByOS && VisualStyleInformation.IsEnabledByUser)
			{
				// Draw tabs
				for(int i = 0; i < this.TabPages.Count; i++)
				{
					if(i == this.SelectedIndex) continue;
					DrawTab(e.Graphics, i);
				}

				// Draw selected tab
				if(this.SelectedIndex != -1) DrawTab(e.Graphics, this.SelectedIndex);
			}
			else
			{
				base.OnPaint(e);
			}
		}
		
		// Mouse moves
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if(VisualStyleInformation.IsSupportedByOS && VisualStyleInformation.IsEnabledByUser)
			{
				int foundindex = -1;
				Rectangle prect = new Rectangle(e.Location, Size.Empty);
				
				// Check in which tab the mouse is
				for(int i = this.TabPages.Count - 1; i >= 0; i--)
				{
					Rectangle tabrect = this.GetTabRect(i);
					tabrect.Inflate(1, 1);
					if(tabrect.IntersectsWith(prect))
					{
						foundindex = i;
						break;
					}
				}
				
				// Redraw?
				if(foundindex != highlighttab)
				{
					highlighttab = foundindex;
					this.Invalidate();
				}
			}
			
			base.OnMouseMove(e);
		}
		
		// Mouse leaves
		protected override void OnMouseLeave(EventArgs e)
		{
			if(VisualStyleInformation.IsSupportedByOS && VisualStyleInformation.IsEnabledByUser)
			{
				// Redraw?
				if(highlighttab != -1)
				{
					highlighttab = -1;
					this.Invalidate();
				}
			}
			
			base.OnMouseLeave(e);
		}
		
		// Tabs don't process keys
		protected override void OnKeyDown(KeyEventArgs ke)
		{
			if(this.Parent is DockersControl)
			{
				// Only absorb the key press when no focused on an input control, otherwise
				// the input controls may not receive certain keys such as delete and arrow keys
				DockersControl docker = (this.Parent as DockersControl);
				if(!docker.IsFocused) ke.Handled = true;
			}
		}
		
		// Tabs don't process keys
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if(this.Parent is DockersControl)
			{
				// Only absorb the key press when no focused on an input control, otherwise
				// the input controls may not receive certain keys such as delete and arrow keys
				DockersControl docker = (this.Parent as DockersControl);
				if(!docker.IsFocused) e.Handled = true;
			}
		}
		
		#endregion
	}
}
