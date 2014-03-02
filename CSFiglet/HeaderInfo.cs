using System;
using System.IO;
using System.Text.RegularExpressions;
using RegexStringLibrary;

namespace CSFiglet
{
	public class HeaderInfo
	{
		#region Private variables
		private static readonly Regex RgxHeader; 
		#endregion

		#region Public Properties
		public int Baseline { get; private set; }
		public int MaxLength { get; private set; }
		public int OldLayout { get; private set; }
		public int CommentLines { get; private set; }
		public int PrintDirection { get; private set; }
		public int FullLayout { get; private set; }
		public int CodetagCount { get; private set; }
		public int Height { get; private set; }
		public string Signature { get; private set; }
		public char HardBlank { get; private set; }
		public bool OptionalValuesPresent { get; private set; }
		#endregion

		#region Static Constructor
		static HeaderInfo()
		{
			// Set up the regular expression to read the header
			var signature = Stex.Any.Rep(5, 5).Named("Signature");
			var hardBlank = ".".Named("HardBlank");
			var height = Stex.Integer().Named("Height");
			var baseline = Stex.Integer().Named("Baseline");
			var maxLength = Stex.Integer().Named("MaxLength");
			var oldLayout = Stex.Integer().Named("OldLayout");
			var commentLines = Stex.Integer().Named("CommentLines");
			var printDirection = Stex.Integer().Named("PrintDirection");
			var fullLayout = Stex.Integer().Named("FullLayout");
			var codetagCount = Stex.Integer().Named("CodetagCount");
			var sep = Stex.WhitePadding;
			var optionalParams = Stex.Cat(sep, printDirection, sep, fullLayout, sep, codetagCount).Optional();
			var rgx = Stex.Cat(
				signature,
				hardBlank, sep,
				height, sep,
				baseline, sep,
				maxLength, sep,
				oldLayout, sep,
				commentLines,
				optionalParams);

			RgxHeader = new Regex(rgx, RegexOptions.Compiled);
		} 
		#endregion

		#region Constructor
		private static int GetNamedInt(string name, Match match)
		{
			int val;
			if (!int.TryParse(match.Groups[name].Value, out val))
			{
				throw new InvalidOperationException("Couldn't read Header");
			}
			return val;
		}

		internal HeaderInfo(StreamReader sr)
		{
			// Get the line
			var headerLine = sr.ReadLine();
			if (headerLine == null)
			{
				throw new InvalidOperationException("Couldn't find header line");
			}

			// Match it against our regular expression
			var mtch = RgxHeader.Match(headerLine);

			// Pull out the values
			Signature = mtch.Groups["Signature"].Value;
			if (Signature != "flf2a")
			{
				throw new InvalidOperationException("Invalid Figlet file");
			}
			HardBlank = mtch.Groups["HardBlank"].Value[0];
			Height = GetNamedInt("Height", mtch);
			Baseline = GetNamedInt("Baseline", mtch);
			MaxLength = GetNamedInt("MaxLength", mtch);
			OldLayout = GetNamedInt("OldLayout", mtch);
			CommentLines = GetNamedInt("CommentLines", mtch);
			if (mtch.Groups["PrintDirection"].Value == string.Empty)
			{
				// Optional values not present - make our own values
				PrintDirection = 0;
				FullLayout = 0;
				CodetagCount = 0;
				OptionalValuesPresent = false;
			}
			else
			{
				// Optional values are present so pull them out also
				PrintDirection = GetNamedInt("PrintDirection", mtch);
				FullLayout = GetNamedInt("FullLayout", mtch);
				CodetagCount = GetNamedInt("CodetagCount", mtch);
				OptionalValuesPresent = true;
			}
		} 
		#endregion
	}
}
