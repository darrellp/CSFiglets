using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RegexStringLibrary;

namespace CSFiglet
{
	public class CharInfo
	{
		public bool Valid { get; set; }
		public bool CodeTag { get; set; }
		int Width { get; set; }
		int Val { get; set; }
		string Comment { get; set; }
		List<string> SubChars { get; set; }
		private static Regex _rgxCodeTag;

		static CharInfo()
		{
			var neg = "-".Optional();
			var octal = Stex.Cat("0", Stex.Range("0", "7").Rep(1)).Named("Octal");
			var hex = Stex.Cat("0",
				Stex.AnyCharFrom("Xx"),
				Stex.AnyCharFrom("A-Fa-f0-9").Rep(1)).Named("Hex");
			var code = neg + Stex.AnyOf(octal, hex, Stex.Integer("Decimal")).Named("Code");
			var codeTagExpr = code + Stex.WhitePadding + Stex.Any.Rep(1).Named("Comment");
			_rgxCodeTag = new Regex(codeTagExpr, RegexOptions.Compiled);
		}

		internal CharInfo(StreamReader sr, int val, string comment, HeaderInfo headerInfo)
		{
			CodeTag = val < 0;
			Valid = false;
			if (!CodeTag)
			{
				Val = val;
				Comment = comment;
			}
			else
			{
				var line = sr.ReadLine();
				if (line == null)
				{
					return;
				}
				var mtch = _rgxCodeTag.Match(line);
				var codeVal = -1;
				string strVal;
				var fNeg = mtch.Groups["Code"].Value.StartsWith("-");

				if ((strVal = mtch.Groups["Octal"].Value) != string.Empty)
				{
					codeVal = ParseOctal(strVal);
					if (fNeg)
					{
						codeVal = -codeVal;
					}
				}
				else if ((strVal = mtch.Groups["Hex"].Value) != string.Empty)
				{
					codeVal = ParseHex(strVal);
					if (fNeg)
					{
						codeVal = -codeVal;
					}
				}
				else
				{
					if (!int.TryParse(mtch.Groups["Code"].Value, out codeVal))
					{
						throw new InvalidOperationException("Invalid Codetag");
					}
				}
				Val = codeVal;
				Comment = mtch.Groups["Comment"].Value;
			}
			Width = -1;
			SubChars = new List<string>();
			var firstLine = sr.ReadLine();
			if (firstLine == null)
			{
				return;
			}
			for (var i = 0; i < headerInfo.Height - 1; i++)
			{
				var line = i == 0 ? firstLine : sr.ReadLine();
				if (line == null)
				{
					throw new InvalidOperationException("Invalid font file");
				}

				SubChars.Add(line.Substring(0, line.Length - 1));
				if (Width < 0)
				{
					Width = line.Length - 1;
				}
				else if (Width != line.Length - 1)
				{
					throw new InvalidOperationException("Invalid font file");
				}
			}
			var lastLine = sr.ReadLine();
			if (lastLine == null)
			{
				throw new InvalidOperationException("Invalid font file");
			}
			SubChars.Add(lastLine.Substring(0, lastLine.Length - 2));
			if (Width < 0)
			{
				Width = lastLine.Length - 2;
			}
			else if (Width != lastLine.Length - 2)
			{
				throw new InvalidOperationException("Invalid font file");
			}
			Valid = true;
		}

		private int ParseHex(string strVal)
		{
			return strVal
				.Substring(2).
				Aggregate(0, (current, hexDigit) => current * 16 + ((hexDigit >= '0' && hexDigit <= '9') ? (hexDigit - '0') : (char.ToLower(hexDigit) - 'a' + 10)));
		}

		private int ParseOctal(string strVal)
		{
			return strVal.Substring(1).Aggregate(0, (current, hexDigit) => current * 8 + hexDigit - '0');
		}
	}
}
