
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
using System.IO;
using CodeImp.DoomBuilder.Data;
using System.Diagnostics;
using CodeImp.DoomBuilder.Actions;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Editing;
using System.Security.Cryptography;

#endregion

namespace CodeImp.DoomBuilder
{
	internal class Launcher : IDisposable
	{
		#region ================== Constants

		#endregion

		#region ================== Variables

		private string tempwad;
		private Process process; //mxd
		private bool isdisposed;

		delegate void EngineExitedCallback(); //mxd
		
		#endregion

		#region ================== Properties

		public string TempWAD { get { return tempwad; } }

		#endregion

		#region ================== Constructor / Destructor

		// Constructor
		public Launcher(MapManager manager)
		{
			// Initialize
			CleanTempFile(manager);

			// Bind actions
			General.Actions.BindMethods(this);
		}

		// Disposer
		public void Dispose()
		{
			// Not yet disposed?
			if(!isdisposed)
			{
				// Unbind actions
				General.Actions.UnbindMethods(this);

				//mxd. Terminate process?
				if(process != null) 
				{
					process.CloseMainWindow();
					process.Close();
				}
				
				// Remove temporary file
				try { File.Delete(tempwad); }
				catch(Exception) { }
				
				// Done
				isdisposed = true;
			}
		}

		#endregion

		#region ================== Parameters

		// This takes the unconverted parameters (with placeholders) and converts it
		// to parameters with full paths, names and numbers where placeholders were put.
		// The tempfile must be the full path and filename to the PWAD file to test.
		public string ConvertParameters(string parameters, int skill, string skin, int gametype, bool shortpaths)
		{
			string outp = parameters;
			DataLocation iwadloc;
			string p_wp = "", p_wf = "";
			string p_ap = "", p_apq = "";
			string p_l1 = "", p_l2 = "";
			string p_nm = "";
			string f = tempwad;
			
			// Make short path if needed
			if(shortpaths) f = General.GetShortFilePath(f);
			
			// Find the first IWAD file
			if(General.Map.Data.FindFirstIWAD(out iwadloc))
			{
				// %WP and %WF result in IWAD file
				p_wp = iwadloc.location;
				p_wf = Path.GetFileName(p_wp);
				if(shortpaths)
				{
					p_wp = General.GetShortFilePath(p_wp);
					p_wf = General.GetShortFilePath(p_wf);
				}
			}
			
			// Make a list of all data locations, including map location
			DataLocation maplocation = new DataLocation(DataLocation.RESOURCE_WAD, General.Map.FilePathName, false, false, false);
			DataLocationList locations = new DataLocationList();
			locations.AddRange(General.Map.ConfigSettings.Resources);
			locations.AddRange(General.Map.Options.Resources);
			if(!string.IsNullOrEmpty(maplocation.location)) locations.Add(maplocation); //mxd. maplocation.location will be empty when a newly created map was not saved yet.

            FileInfo fi = new FileInfo(f);

			// Go for all data locations
			foreach(DataLocation dl in locations)
			{
				// Location not the IWAD file?
				if((dl.type != DataLocation.RESOURCE_WAD) || (dl.location != iwadloc.location))
				{
					// Location not included?
					if(!dl.notfortesting)
					{
                        if (FilesAreEqual(fi, new FileInfo(dl.location)))
                        {
                            outp = outp.Replace("%f", "%F");
                            outp = outp.Replace("\"%F\"", "");
                        }

                        // Add to string of files
                        if (shortpaths)
						{
							p_ap += General.GetShortFilePath(dl.location) + " ";
							p_apq += "\"" + General.GetShortFilePath(dl.location) + "\" ";
						}
						else
						{
							p_ap += dl.location + " ";
							p_apq += "\"" + dl.location + "\" ";
						}
					}
				}
			}

			// Trim last space from resource file locations
			p_ap = p_ap.TrimEnd(' ');
			p_apq = p_apq.TrimEnd(' ');

			// Try finding the L1 and L2 numbers from the map name
			string numstr = "";
			bool first = true;
			foreach(char c in General.Map.Options.CurrentName)
			{
				// Character is a number?
				if(Configuration.NUMBERS.IndexOf(c) > -1)
				{
					// Include it
					numstr += c;
				}
				else
				{
					// Store the number if we found one
					if(numstr.Length > 0)
					{
						int num;
						int.TryParse(numstr, out num);
						if(first) p_l1 = num.ToString(); else p_l2 = num.ToString();
						numstr = "";
						first = false;
					}
				}
			}
			
			// Store the number if we found one
			if(numstr.Length > 0)
			{
				int num;
				int.TryParse(numstr, out num);
				if(first) p_l1 = num.ToString(); else p_l2 = num.ToString();
			}

			// No monsters?
			if(!General.Settings.TestMonsters) p_nm = "-nomonsters";
			
			// Make sure all our placeholders are in uppercase
			outp = outp.Replace("%f", "%F");
			outp = outp.Replace("%wp", "%WP");
			outp = outp.Replace("%wf", "%WF");
			outp = outp.Replace("%wP", "%WP");
			outp = outp.Replace("%wF", "%WF");
			outp = outp.Replace("%Wp", "%WP");
			outp = outp.Replace("%Wf", "%WF");
			outp = outp.Replace("%l1", "%L1");
			outp = outp.Replace("%l2", "%L2");
			outp = outp.Replace("%l", "%L");
			outp = outp.Replace("%ap", "%AP");
			outp = outp.Replace("%aP", "%AP");
			outp = outp.Replace("%Ap", "%AP");
			outp = outp.Replace("%s", "%S");
			outp = outp.Replace("%nM", "%NM");
			outp = outp.Replace("%Nm", "%NM");
			outp = outp.Replace("%nm", "%NM");

            // Replace placeholders with actual values
            outp = outp.Replace("%F", f);
			outp = outp.Replace("%WP", p_wp);
			outp = outp.Replace("%WF", p_wf);
			outp = outp.Replace("%L1", p_l1);
			outp = outp.Replace("%L2", p_l2);
			outp = outp.Replace("%L", General.Map.Options.CurrentName);
			outp = outp.Replace("\"%AP\"", p_apq);
			outp = outp.Replace("%AP", p_ap);
			outp = outp.Replace("%S", skill.ToString());
			outp = outp.Replace("%NM", p_nm);
            outp = outp + " +skin " + skin.ToLowerInvariant();
            if (gametype != -1)
                outp = outp + " -server -gametype " + gametype.ToString();

            // Return result
            return outp;
		}

		//mxd
		private bool AlreadyTesting()
		{
			if(process != null)
			{
				General.ShowWarningMessage("Game engine is already running." + Environment.NewLine + "Please close '" + process.MainModule.FileName + "' first.", MessageBoxButtons.OK);
				return true;
			}

			return false;
		}

		#endregion

		#region ================== Test

		// This saves the map to a temporary file and launches a test
		[BeginAction("testmap")]
		public void Test()
		{
			if(AlreadyTesting() || !General.Editing.Mode.OnMapTestBegin(false)) return; //mxd
			TestAtSkill(General.Map.ConfigSettings.TestSkill);
			General.Editing.Mode.OnMapTestEnd(false); //mxd
		}

		//mxd
		[BeginAction("testmapfromview")]
		public void TestFromView() 
		{
			if(AlreadyTesting() || !General.Editing.Mode.OnMapTestBegin(true)) return;
			TestAtSkill(General.Map.ConfigSettings.TestSkill);
			General.Editing.Mode.OnMapTestEnd(true);
		}
		
		// This saves the map to a temporary file and launches a test with the given skill
		public void TestAtSkill(int skill)
		{
			Cursor oldcursor = Cursor.Current;

            // Check if configuration is OK
            if (string.IsNullOrEmpty(General.Map.ConfigSettings.TestProgram) || !File.Exists(General.Map.ConfigSettings.TestProgram))
            {
				//mxd. Let's be more precise
				string message;
				if(String.IsNullOrEmpty(General.Map.ConfigSettings.TestProgram))
					message = "Your test program is not set for the current game configuration";
				else
					message = "Current test program has invalid path";
				
				// Show message
				Cursor.Current = Cursors.Default;
				DialogResult result = General.ShowWarningMessage(message + ". Would you like to set up your test program now?", MessageBoxButtons.YesNo);
				if(result == DialogResult.Yes)
				{
					// Show game configuration on the right page
					General.MainWindow.ShowConfigurationPage(2);
				}
				return;
			}

			// No custom parameters?
			if(!General.Map.ConfigSettings.CustomParameters)
			{
				// Set parameters to the default ones
				General.Map.ConfigSettings.TestParameters = General.Map.Config.TestParameters;
				General.Map.ConfigSettings.TestShortPaths = General.Map.Config.TestShortPaths;
			}
			
			// Remove temporary file
			try { File.Delete(tempwad); }
			catch(Exception) { }
			
			// Save map to temporary file
			Cursor.Current = Cursors.WaitCursor;
			tempwad = General.MakeTempFilename(General.Map.TempPath, "wad");
			General.Plugins.OnMapSaveBegin(SavePurpose.Testing);
			if(General.Map.SaveMap(tempwad, SavePurpose.Testing))
			{
				// No compiler errors?
				if(General.Map.Errors.Count == 0)
				{
					// Make arguments
					string args = ConvertParameters(General.Map.ConfigSettings.TestParameters, skill, General.Map.ConfigSettings.TestSkin, General.Map.ConfigSettings.TestGametype, General.Map.ConfigSettings.TestShortPaths);

					// Setup process info
					ProcessStartInfo processinfo = new ProcessStartInfo();
					processinfo.Arguments = args;
					processinfo.FileName = General.Map.ConfigSettings.TestProgram;
					processinfo.CreateNoWindow = false;
					processinfo.ErrorDialog = false;
					processinfo.UseShellExecute = true;
					processinfo.WindowStyle = ProcessWindowStyle.Normal;
					processinfo.WorkingDirectory = Path.GetDirectoryName(processinfo.FileName);

					// Output info
					General.WriteLogLine("Running test program: " + processinfo.FileName);
					General.WriteLogLine("Program parameters:  " + processinfo.Arguments);
					General.MainWindow.DisplayStatus(StatusType.Info, "Launching " + processinfo.FileName + "...");

					try
					{
						// Start the program
						process = Process.Start(processinfo);
						process.EnableRaisingEvents = true; //mxd
						process.Exited += ProcessOnExited; //mxd
						Cursor.Current = oldcursor; //mxd
					}
					catch(Exception e)
					{
						// Unable to start the program
						General.ShowErrorMessage("Unable to start the test program, " + e.GetType().Name + ": " + e.Message, MessageBoxButtons.OK);
					}
				}
				else
				{
					General.MainWindow.DisplayStatus(StatusType.Warning, "Unable to test the map due to script errors.");
				}
			}
		}

        //Check if two files are equal using MD5 hash. I can't believe I need this...
        static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            byte[] firstHash = MD5.Create().ComputeHash(first.OpenRead());
            byte[] secondHash = MD5.Create().ComputeHash(second.OpenRead());

            for (int i = 0; i < firstHash.Length; i++)
            {
                if (firstHash[i] != secondHash[i])
                    return false;
            }
            return true;
        }

        //mxd
        private void TestingFinished() 
		{
			//Done
			TimeSpan deltatime = TimeSpan.FromTicks(process.ExitTime.Ticks - process.StartTime.Ticks);
			process = null;
			General.WriteLogLine("Test program has finished.");
			General.WriteLogLine("Run time: " + deltatime.TotalSeconds.ToString("###########0.00") + " seconds");
			General.MainWindow.DisplayReady();

			// Clean up temp file
			CleanTempFile(General.Map);

			General.Plugins.OnMapSaveEnd(SavePurpose.Testing);
			General.MainWindow.FocusDisplay();
			if(General.Editing.Mode is ClassicMode) General.MainWindow.RedrawDisplay();
		}

		//mxd
		private void ProcessOnExited(object sender, EventArgs eventArgs) 
		{
			General.MainWindow.Invoke(new EngineExitedCallback(TestingFinished));
		}

		// This deletes the previous temp file and creates a new, empty temp file
		private void CleanTempFile(MapManager manager)
		{
			// Remove temporary file
			try { File.Delete(tempwad); }
			catch(Exception) { }
			
			// Make new empty temp file
			tempwad = General.MakeTempFilename(manager.TempPath, "wad");
			File.WriteAllText(tempwad, "");
		}

		#endregion
	}
}
