using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSFiglet
{
    public class FigletFont
    {
		#region Private variables
		private const string EmbeddedFilePrefix = "CSFiglet.Fonts.";
		private const string EmbeddedFileExtension = ".flf";

		private readonly List<int> _specialVals = new List<int> { 196, 214, 220, 228, 246, 252, 223 };
		private readonly List<string> _specialComments = new List<string>
		{
			"Umlaut A",
			"Umlaut O",
			"Umlaut U",
			"Umlaut a",
			"Umlaut o",
			"Umlaut u",
			"Ess-zed"
		}; 
		#endregion

		#region Public Properties
		/// <summary>
		/// Header info
		/// </summary>
		public HeaderInfo Header { get; private set; }

		/// <summary>
		/// Comments from the font file
		/// </summary>
		public string Comments { get; set; }

		/// <summary>
		/// Dictionary of individual character data
		/// </summary>
		public ReadOnlyDictionary<int, CharInfo> Chars { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Instantiate from a figlet font file
		/// </summary>
		/// <param name="streamReader">StreamReader for font file</param>
		private FigletFont(StreamReader streamReader)
		{
			// Inititialize and read opening data
			var chars = new Dictionary<int, CharInfo>();
			Header = new HeaderInfo(streamReader);
			ReadComments(streamReader);

			// Set up the list of required values and comments
			var requiredValues = new List<int>();
			var requiredComments = new List<string>();

			for (var i = 32; i <= 126; i++)
			{
				requiredValues.Add(i);
				requiredComments.Add(new string((char)i, 1));
			}
			requiredComments[0] = "Space";
			for (var i = 0; i < _specialVals.Count; i++)
			{
				requiredValues.Add(_specialVals[i]);
				requiredComments.Add(_specialComments[i]);
			}

			// Read in required characters
			CharInfo charInfo;
			for (var iChar = 0; iChar < requiredValues.Count; iChar++)
			{
				charInfo = ReadCharInfo(streamReader, requiredValues[iChar], requiredComments[iChar]);
				if (charInfo == null)
				{
					throw new InvalidOperationException("Missing required characters in char info");
				}
				chars[charInfo.Val] = charInfo;
			}

			// Read in codetag characters
			while ((charInfo = ReadCharInfo(streamReader, -1, null)) != null)
			{
				chars[charInfo.Val] = charInfo;
			}

			Chars = new ReadOnlyDictionary<int, CharInfo>(chars);
		}

		public FigletFont(string file)
			: this(new StreamReader(file)) { }

		private CharInfo ReadCharInfo(StreamReader sr, int value, string comment)
		{
			var ret = new CharInfo(sr, value, comment, Header);
			return ret.Valid ? ret : null;
		}

		private void ReadComments(StreamReader sr)
		{
			for (var iLine = 0; iLine < Header.CommentLines; iLine++)
			{
				Comments += sr.ReadLine() + "\n";
			}
		}
		#endregion

		#region Public Static Functions
		/// <summary>
		/// Return a figlet font from it's friendly name
		/// </summary>
		/// <param name="name">Friendly name for the font</param>
		/// <returns>Figlet font corresponding to the friendly name</returns>
		public static FigletFont FigletFromName(string name)
		{
			var resourceName = EmbeddedFilePrefix + name + EmbeddedFileExtension;
			if (resourceName == null)
			{
				throw new ArgumentException("FigletFromName has invalid name");
			}
			var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			Debug.Assert(resourceStream != null, "resourceStream != null");
			var sr = new StreamReader(resourceStream);
			return new FigletFont(sr);
		}

		/// <summary>
		/// Return all the internal friendly names available
		/// </summary>
		/// <returns>List of all internal friendly names</returns>
		public static List<string> Names()
		{
			var prefixLength = EmbeddedFilePrefix.Length;
			var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			return names.Select(name => name.Substring(prefixLength, name.LastIndexOf('.') - prefixLength)).ToList();
		} 
		#endregion
    }
}
