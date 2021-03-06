#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CodeImp.DoomBuilder.Compilers;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.ZDoom;
using CodeImp.DoomBuilder.GZBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.SRB2
{
    internal sealed class SOCObjectParser : ZDTextParser
    {
        #region ================== Delegates

        public delegate void IncludeDelegate(SOCObjectParser parser, string includefile, bool clearerror);
        public IncludeDelegate OnInclude;

        #endregion

        #region ================== Variables

        private Dictionary<string, SRB2Object> objects;
        /*private Dictionary<string, SRB2State> states;
        private List<string> objectfreeslots;
        private List<string> statefreeslots;
        private List<string> spritefreeslots;*/
        private IDictionary<string, int> flagValues = new Dictionary<string, int>
        {
            { "MF_SPECIAL", 0x1 },
            { "MF_SOLID", 0x2 },
            { "MF_SHOOTABLE", 0x4 },
            { "MF_NOSECTOR", 0x8 },
            { "MF_NOBLOCKMAP", 0x10 },
            { "MF_AMBUSH", 0x20 },
            { "MF_PUSHABLE", 0x40 },
            { "MF_BOSS", 0x80 },
            { "MF_SPAWNCEILING", 0x100 },
            { "MF_NOGRAVITY", 0x200 },
            { "MF_AMBIENT", 0x400 },
            { "MF_SLIDEME", 0x800 },
            { "MF_NOCLIP", 0x1000 },
            { "MF_FLOAT", 0x2000 },
            { "MF_BOXICON", 0x4000 },
            { "MF_MISSILE", 0x8000 },
            { "MF_SPRING", 0x10000 },
            { "MF_BOUNCE", 0x20000 },
            { "MF_MONITOR", 0x40000 },
            { "MF_NOTHINK", 0x80000 },
            { "MF_FIRE", 0x100000 },
            { "MF_NOCLIPHEIGHT", 0x200000 },
            { "MF_ENEMY", 0x400000 },
            { "MF_SCENERY", 0x800000 },
            { "MF_PAIN", 0x1000000 },
            { "MF_STICKY", 0x2000000 },
            { "MF_NIGHTSITEM", 0x4000000 },
            { "MF_NOCLIPTHING", 0x8000000 },
            { "MF_GRENADEBOUNCE", 0x10000000 },
            { "MF_RUNSPAWNFUNC", 0x10000000 }
        };
        private StreamReader streamreader;
        private int linenumber;

        #endregion

        #region ================== Properties

        public Dictionary<string, SRB2Object> Objects { get { return objects; } }

        #endregion

        #region ================== Constructor

        public SOCObjectParser()
        {
            // Syntax
            whitespace = "\n \t\r\u00A0";
            specialtokens = "=\n";

            objects = new Dictionary<string,SRB2Object>();
            /*states = new Dictionary<string,SRB2State>();
            objectfreeslots = new List<string>();
            statefreeslots = new List<string>();
            spritefreeslots = new List<string>();*/
        }

        // Disposer
        public void Dispose()
        {
            objects = null;
            /*states = null;
            objectfreeslots = null;
            statefreeslots = null;
            spritefreeslots = null;*/
        }

        #endregion

        #region ================== Parsing

        override public bool Parse(Stream stream, string sourcefilename, bool clearerrors)
        {
            if (!base.Parse(stream, sourcefilename, clearerrors)) return false;

            // Keep local data
            streamreader = new StreamReader(stream, Encoding.ASCII);
            linenumber = -1;

            while (!streamreader.EndOfStream)
            {
                string line = RemoveComments(streamreader.ReadLine());
                linenumber++;
                if (String.IsNullOrEmpty(line) || line.StartsWith("\n")) continue;
                string[] tokens = line.Split(new char[] { ' ' });
                switch (tokens[0].ToUpperInvariant())
                {
                    /*case "FREESLOT":
                        if (!ParseFreeslots()) return false;
                        break;*/
                    case "OBJECT":
                    case "MOBJ":
                    case "THING":
                        if (tokens.Length < 2 || String.IsNullOrEmpty(tokens[1]))
                        {
                            ReportError("Object block is missing an object name");
                            break;
                        }
                        if (!ParseObject(tokens[1].ToUpperInvariant())) return false;
                        break;
                    /*case "STATE":
                    case "FRAME":
                        if (tokens.Length < 2 || String.IsNullOrEmpty(tokens[1]))
                        {
                            ReportError("State block is missing an state name");
                            break;
                        }
                        if (!ParseState(tokens[1].ToUpperInvariant())) return false;
                        break;*/
                }
            }

            // All done
            return !this.HasError;
        }

        #endregion

        #region ================== Map block parsing

        /*private bool ParseFreeslots()
        {
            while (!streamreader.EndOfStream)
            {
                string line = streamreader.ReadLine();
                linenumber++;
                if (String.IsNullOrEmpty(line) || line.StartsWith("\n")) break;
                if (line.StartsWith("#")) continue;
                line = RemoveComments(line).Trim();
                if (line.StartsWith("MT_")) objectfreeslots.Add(line);
                else if (line.StartsWith("S_")) statefreeslots.Add(line);
                else if (line.StartsWith("SPR_")) spritefreeslots.Add(line);
            }
            return true;
        }*/

        private bool ParseObject(string objname)
        {
            if (objname == null) return false;
            string name = objname;
            string category = "";
            string sprite = DataManager.INTERNAL_PREFIX + "unknownthing";
            string[] states = new string[8];
            int mapThingNum = -1;
            int radius = 0;
            int height = 0;
            int flags = 0;
            while (!streamreader.EndOfStream)
            {
                string line = streamreader.ReadLine();
                linenumber++;
                if (String.IsNullOrEmpty(line) || line.StartsWith("\n")) break;
                if (line.StartsWith("#$Sprite "))
                {
                    string spritename = line.Substring(9);
                    if (((spritename.Length > DataManager.INTERNAL_PREFIX.Length) &&
                        spritename.ToLowerInvariant().StartsWith(DataManager.INTERNAL_PREFIX)) ||
                        General.Map.Data.GetSpriteExists(spritename))
                    {
                        sprite = spritename;
                        continue;
                    }
                    LogWarning("The sprite \"" + spritename + "\" assigned by the \"$sprite\" property does not exist");
                    continue;
                }
                if (line.StartsWith("#$Name "))
                {
                    name = ZDTextParser.StripQuotes(line.Substring(7));
                    continue;
                }
                if (line.StartsWith("#$Category "))
                {
                    category = line.Substring(11);
                    continue;
                }
                if (line.StartsWith("#")) continue;
                line = RemoveComments(line);
                string[] tokens = line.Split(new char[] { '=' });
                if (tokens.Length != 2)
                {
                    ReportError("Invalid line");
                    return false;
                }
                tokens[0] = tokens[0].Trim().ToUpperInvariant();
                tokens[1] = tokens[1].Trim().ToUpperInvariant();
                switch(tokens[0])
                {
                    case "MAPTHINGNUM":
                        if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out mapThingNum))
                            LogWarning("Could not parse map thing number");
                        break;
                    case "RADIUS":
                        if (!ParseWithArithmetic(tokens[1], out radius))
                            LogWarning("Could not parse radius");
                        radius /= 65536;
                        break;
                    case "HEIGHT":
                        if (!ParseWithArithmetic(tokens[1], out height))
                            LogWarning("Could not parse height");
                        height /= 65536;
                        break;

                    case "SPAWNSTATE":
                        states[0] = tokens[1];
                        break;
                    case "SEESTATE":
                        states[1] = tokens[1];
                        break;
                    case "PAINSTATE":
                        states[2] = tokens[1];
                        break;
                    case "MELEESTATE":
                        states[3] = tokens[1];
                        break;
                    case "MISSILESTATE":
                        states[4] = tokens[1];
                        break;
                    case "DEATHSTATE":
                        states[5] = tokens[1];
                        break;
                    case "XDEATHSTATE":
                        states[6] = tokens[1];
                        break;
                    case "RAISESTATE":
                        states[7] = tokens[1];
                        break;
                    case "FLAGS":
                        if (!ParseFlags(tokens[1], out flags))
                            LogWarning("Could not parse flags");
                        break;
                }
            }
            if (mapThingNum > 0)
            {
                SRB2Object o = new SRB2Object(name, sprite, category, states, mapThingNum, radius, height, flags);
                if (objects.ContainsKey(objname))
                    objects[objname] = o;
                else
                    objects.Add(objname, o);
            }

            return true;
        }

        /*private bool ParseState(string name)
        {
            if (name == null) return false;
            string spritename = "";
            int spriteframe = 0;
            string next = "";
            while (!streamreader.EndOfStream)
            {
                string line = streamreader.ReadLine();
                linenumber++;
                if (String.IsNullOrEmpty(line) || line.StartsWith("\n")) break;
                if (line.StartsWith("#")) continue;
                line = RemoveComments(line);
                string[] tokens = line.Split(new char[] { '=' });
                if (tokens.Length != 2)
                {
                    ReportError("Invalid line");
                    return false;
                }
                tokens[0] = tokens[0].Trim().ToUpperInvariant();
                tokens[1] = tokens[1].Trim().ToUpperInvariant();
                switch (tokens[0])
                {
                    case "SPRITENAME":
                    case "SPRITENUMBER":
                        spritename = tokens[1];
                        break;
                    case "SPRITEFRAME":
                    case "SPRITESUBNUMBER":
                    //TODO: Strip flags
                        spriteframe = ParseSpriteFrame(tokens[1]);
                        break;
                    case "NEXT":
                        next = tokens[1];
                        break;
                }
            }
            states.Add(new SRB2State(name, spritename, spriteframe, next));

            return true;
        }*/

        #endregion

        #region ================== Methods

        private bool ParseWithArithmetic(string input, out int output)
        {
            output = 0;
            string[] tokens = input.Split(new char[] { '+' });
            foreach (string t in tokens)
            {
                int val = 0;
                if (!ParseMultiplication(t, out val))
                    return false;
                output += val;
            }
            return true;
        }

        private bool ParseMultiplication(string input, out int output)
        {
            output = 1;
            string[] tokens = input.Split(new char[] { '*' });
            foreach (string t in tokens)
            {
                string trimmed = t.Trim();
                int val = 1;
                if (trimmed == "FRACUNIT") val = 65536;
                else if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                    return false;
                output *= val;
            }
            return true;
        }

        private bool ParseFlags(string input, out int output)
        {
            output = 0;
            string[] tokens = input.Split(new char[] { '|' });
            foreach (string token in tokens)
            {
                if (flagValues.ContainsKey(token))
                {
                    output |= flagValues[token];
                }
                else
                {
                    int val = 0;
                    if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                        return false;
                    output |= val;
                }
            }
            return true;
        }

        // This reports an error
        protected internal override void ReportError(string message)
        {
            // Set error information
            errordesc = message;
            errorline = (streamreader != null ? linenumber : CompilerError.NO_LINE_NUMBER); //mxd
            errorsource = sourcename;
        }

        private string RemoveComments(string line)
        {
            string[] tokens = line.Split(new char[] { '#' });
            return tokens[0];
        }

        protected override string GetLanguageType()
        {
            return "SOC";
        }

        #endregion
    }
}
