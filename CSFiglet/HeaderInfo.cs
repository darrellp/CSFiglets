using System;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text.RegularExpressions;
using RegexStringLibrary;

namespace CSFiglet
{
	class HeaderInfo
	{
		public int Baseline { get; set; }
		public int MaxLength { get; set; }
		public int OldLayout { get; set; }
		public int CommentLines { get; set; }
		public int PrintDirection { get; set; }
		public int FullLayout { get; set; }
		public int CodetagCount { get; set; }
		public int Height { get; set; }
		public string Signature { get; set; }
		public char HardBlank { get; set; }
		private static Regex _rgxHeader;

		static HeaderInfo()
		{
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
			var rgx = Stex.Cat(
				signature,
				hardBlank, sep,
				height, sep,
				baseline, sep,
				maxLength, sep,
				oldLayout, sep,
				commentLines, sep,
				printDirection, sep,
				fullLayout, sep,
				codetagCount);

			_rgxHeader = new Regex(rgx, RegexOptions.Compiled);
		}
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
			var headerLine = sr.ReadLine();
			if (headerLine == null)
			{
				throw new InvalidOperationException("Couldn't find header line");
			}
			var mtch = _rgxHeader.Match(headerLine);
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
			PrintDirection = GetNamedInt("PrintDirection", mtch);
			FullLayout = GetNamedInt("FullLayout", mtch);
			CodetagCount = GetNamedInt("CodetagCount", mtch);
		}
	}
}
