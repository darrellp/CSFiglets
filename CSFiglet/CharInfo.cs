using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RegexStringLibrary;

namespace CSFiglet
{
	public class CharInfo
	{
		#region Private Variables
		private static readonly Regex RgxCodeTag; 
		#endregion

		#region Public Properties
		public bool Valid { get; private set; }
		public bool CodeTag { get; private set; }
		public int Width { get; private set; }
		public int Val { get; private set; }
		public string Comment { get; private set; }
		public List<string> SubChars { get; private set; } 
		#endregion

		#region Static Constructor
		static CharInfo()
		{
			// Create the regular expression for reading codetags
			var neg = "-".Optional();
			var octal = Stex.Cat("0", Stex.Range("0", "7").Rep(1)).Named("Octal");
			var hex = Stex.Cat("0",
				Stex.AnyCharFrom("Xx"),
				Stex.AnyCharFrom("A-Fa-f0-9").Rep(1)).Named("Hex");
			var code = neg + Stex.AnyOf(octal, hex, Stex.Integer("Decimal")).Named("Code");
			var codeTagExpr = code + Stex.WhitePadding + Stex.Any.Rep(1).Named("Comment");
			RgxCodeTag = new Regex(codeTagExpr, RegexOptions.Compiled);
		} 
		#endregion

		#region Constructor
		/// <summary>
		/// Read in individual character info from a font file
		/// </summary>
		/// <param name="sr">Stream to read character info from</param>
		/// <param name="val">if -1 then get val/comments from codetag line, else use this as the val</param>
		/// <param name="comment">if val != -1 then this is the comments accompanying val</param>
		/// <param name="headerInfo">Header info from the font file</param>
		internal CharInfo(StreamReader sr, int val, string comment, HeaderInfo headerInfo)
		{
			// Initialization
			CodeTag = val == -1;
			Valid = false;

			// If we're not a codetag
			if (!CodeTag)
			{
				//...then take our values from the passed in parameters
				Val = val;
				Comment = comment;
			}
			else
			{
				int codeVal;
				string strVal;

				// Read Codetag line
				var line = sr.ReadLine();
				if (line == null)
				{
					// If no more lines, we're done
					return;
				}
				
				// Match against our regular expression
				var mtch = RgxCodeTag.Match(line);
				var fNeg = mtch.Groups["Code"].Value.StartsWith("-");

				// Read the code either as octal, hex or decimal
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

				// Set our values
				Val = codeVal;
				Comment = mtch.Groups["Comment"].Value;
			}

			// Read the main character data
			SubChars = new List<string>();
			for (var i = 0; i < headerInfo.Height - 1; i++)
			{
				var line = sr.ReadLine();
				if (line == null)
				{
					throw new InvalidOperationException("Invalid font file");
				}

				// Drop off the terminating character
				SubChars.Add(line.Substring(0, line.Length - 1));

				if (i == 0)
				{
					// Set the width the first time through
					Width = line.Length - 1;
				}
				else if (Width != line.Length - 1)
				{
					// If any subsequent widths are different, we've got a problem
					throw new InvalidOperationException("Invalid font file");
				}
			}

			// Last line has two terminating characters
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
		#endregion

		#region Hex/Octal parsing
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
		#endregion
	}
}
