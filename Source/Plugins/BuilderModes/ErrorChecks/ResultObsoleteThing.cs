#region ================== Namespaces

using System;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	public class ResultObsoleteThing : ErrorResult
	{
		#region ================== Variables

		private readonly Thing thing;

		#endregion

		#region ================== Properties

		public override int Buttons { get { return 1; } }
		public override string Button1Text { get { return "Delete Thing"; } }

		#endregion

		#region ================== Constructor / Destructor

		public ResultObsoleteThing(Thing t, string message) 
		{
			// Initialize
			this.thing = t;
			this.viewobjects.Add(t);
			this.hidden = t.IgnoredErrorChecks.Contains(this.GetType());

			if(string.IsNullOrEmpty(message))
				this.description = "This thing is marked as obsolete in DECORATE. You should probably replace or delete it.";
			else
				this.description = "This thing is marked as obsolete in DECORATE: " + message;
		}

		#endregion

		#region ================== Methods

		// This sets if this result is displayed in ErrorCheckForm (mxd)
		internal override void Hide(bool hide) 
		{
			hidden = hide;
			Type t = this.GetType();
			if(hide) thing.IgnoredErrorChecks.Add(t);
			else if(thing.IgnoredErrorChecks.Contains(t)) thing.IgnoredErrorChecks.Remove(t);
		}

		// This must return the string that is displayed in the listbox
		public override string ToString() 
		{
			return "Thing " + thing.Index + " (" + General.Map.Data.GetThingInfo(thing.SRB2Type).Title + ") at " + thing.Position.x + ", " + thing.Position.y + " is obsolete.";
		}

		// Rendering
		public override void  RenderOverlaySelection(IRenderer2D renderer) 
		{
			renderer.RenderThing(thing, General.Colors.Selection, Presentation.THINGS_ALPHA);
		}
		
		// This removes the thing
		public override bool Button1Click(bool batchMode) 
		{
			if(!batchMode) General.Map.UndoRedo.CreateUndo("Delete thing");
			thing.Dispose();
			General.Map.IsChanged = true;
			General.Map.ThingsFilter.Update();
			return true;
		}

		#endregion
	}
}
