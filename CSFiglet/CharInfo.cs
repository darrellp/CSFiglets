using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using RegexStringLibrary;

[Flags]
public enum HSmushRule
{
	NoSmush = -1,
	Universal = 0,
	EqualCharacter = 1,
	Underscore = 2,
	Heirarchy = 4,
	OppositePair = 8,
	BigX = 16,
	HardBlank = 32,
	KerningByDefault = 64,
	SmushingByDefault = 128
}

public enum CharacterSpacing
{
	FullWidth,
	Kerning,
	Smushing
}

namespace CSFiglet
{
	public class CharInfo
	{
		#region Private Variables
		private static readonly Regex RgxCodeTag;
		private readonly char _hardBlank;
		private readonly List<int> _leftPads = new List<int>();
 		private readonly List<int> _rightPads = new List<int>();
		private readonly List<char> _lChars = new List<char>();
		private readonly List<char> _rChars = new List<char>();
		#endregion

		#region Public Properties
		public bool Valid { get; private set; }
		public bool CodeTag { get; private set; }
		public int Width { get; private set; }
		public int Val { get; private set; }
		public string Comment { get; private set; }
		public List<string> SubChars { get; private set; }

		public List<int> LeftPads
		{
			get { return _leftPads; }
		}

		public List<int> RightPads
		{
			get { return _rightPads; }
		}

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

		#region Kerning
		public int KerningOffset(CharInfo nextChar, HSmushRule smushRule, out bool smushable)
		{
			var val = int.MaxValue;
			smushable = smushRule != HSmushRule.NoSmush && Width > 1 && nextChar.Width > 1;

			for (var iRow = 0; iRow < SubChars.Count; iRow++)
			{
				var r = RightPads[iRow];
				var l = nextChar.LeftPads[iRow];
				if (r == -1 || l == -1)
				{
					continue;
				}
				val = Math.Min(val, r + l);

				smushable = smushable &&
					CheckSmushability(smushRule, _rChars[iRow], nextChar._lChars[iRow], _hardBlank) != '\0';
			}
			if (val == int.MaxValue)
			{
				// If there is nothing intersecting between the two characters then use 0
				// padding - we can't slide the character back infinitely just because there's
				// nothing in the other character to clash with it.
				val = 0;
			}
			return val;
		}

		internal static char CheckSmushability(HSmushRule rule, char lChar, char rChar, char hardBlank)
		{
			if (rChar == ' ')
			{
				return lChar;
			}
			if (lChar == ' ')
			{
				return rChar;
			}

			if (rChar == lChar && rChar != hardBlank)
			{
				return rChar;
			}

			if (rule == HSmushRule.Universal)
			{
				if (rChar == ' ' || rChar == hardBlank)
				{
					return lChar;
				}
				return rChar;
			}

			if (rule.HasFlag(HSmushRule.HardBlank))
			{
				if (rChar == hardBlank && lChar == hardBlank)
				{
					return hardBlank;
				}
			}

			if (lChar == hardBlank || rChar == hardBlank)
			{
				return '\0';
			}

			if (rule.HasFlag(HSmushRule.EqualCharacter))
			{
				if (lChar == rChar)
				{
					return lChar;
				}
			}

			if (rule.HasFlag(HSmushRule.Underscore))
			{
				if (lChar == '_' && @"|/\[]{}()<>".Contains(rChar))
				{
					return rChar;
				}
				if (rChar == '_' && @"|/\[]{}()<>".Contains(lChar))
				{
					return lChar;
				}
			}

			if (rule.HasFlag(HSmushRule.Heirarchy))
			{
				const string heirarchyChars = @"|/\[]{}()<>";
				var breaks = new int[] {1, 3, 5, 7, 9};

				foreach (var iBreak in breaks)
				{
					int ch1, ch2;
					if (iBreak == 1)
					{
						ch1 = ch2 = heirarchyChars[0];
					}
					else
					{
						ch1 = heirarchyChars[iBreak - 2];
						ch2 = heirarchyChars[iBreak - 1];
					}
					if ((lChar == ch1 || lChar == ch2) && heirarchyChars.Substring(iBreak).Contains(rChar))
					{
						return rChar;
					}
					if ((rChar == ch1 || rChar == ch2) && heirarchyChars.Substring(iBreak).Contains(lChar))
					{
						return lChar;
					}
				}
			}

			if (rule.HasFlag(HSmushRule.OppositePair))
			{
				const string pairs = "[]{}()";
				for (int i = 0; i < 6; i += 2)
				{
					if (lChar == pairs[i] && rChar == pairs[i + 1] ||
						rChar == pairs[i] && lChar == pairs[i + 1])
					{
						return '|';
					}
				}
			}

			if (rule.HasFlag(HSmushRule.BigX))
			{
				if (lChar == '/' && rChar == '\\')
				{
					return '|';
				}
				if (lChar == '\\' && rChar == '/')
				{
					return 'Y';
				}
				if (lChar == '>' && rChar == '<')
				{
					return 'X';
				}
			}

			return '\0';
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
			_hardBlank = headerInfo.HardBlank;

			// If we're not a codetag
			if (!CodeTag)
			{
				//...then take our values from the passed in parameters
				Val = val;
				Comment = comment;
			}
			else if (!ReadCodetag(sr))
			{
				return;
			}

			ReadCharInfo(sr, headerInfo);
			Valid = true;
		}

		private bool ReadCodetag(StreamReader sr)
		{
			int codeVal;
			string strVal;

			// Read Codetag line
			var line = sr.ReadLine();
			if (line == null)
			{
				// If no more lines, we're done
				return false;
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
			return true;
		}

		private void ReadCharInfo(StreamReader sr, HeaderInfo headerInfo)
		{
			// Read the main character data
			SubChars = new List<string>();
			for (var i = 0; i < headerInfo.Height; i++)
			{
				var line = sr.ReadLine();
				if (line == null)
				{
					throw new InvalidOperationException("Invalid font file");
				}

				// Drop off the terminating character - last line has two terminating characters
				line = line.Substring(0, line.Length - (i == headerInfo.Height - 1 ? 2 : 1));
				SubChars.Add(line);

				if (i == 0)
				{
					// Set the width the first time through
					Width = line.Length;
				}
				else if (Width != line.Length)
				{
					// If any subsequent widths are different, we've got a problem
					throw new InvalidOperationException("Invalid font file");
				}

				// TODO: Check whether we need to keep hard spaces around after keeping the padding
				// If they're only used for the endpoints of regions then the padding should take care of it
				int left, right;
				char chLeft, chRight;
				CalculatePadding(line, out left, out right, out chLeft, out chRight);
				LeftPads.Add(left);
				RightPads.Add(right);
				_lChars.Add(chLeft);
				_rChars.Add(chRight);
			}
		}

		private void CalculatePadding(string line, out int left, out int right, out char chLeft, out char chRight)
		{
			left = right = -1;
			chRight = chLeft = ' ';
			for (var i = 0; i < line.Length; i++)
			{
				if (right < 0 && line[line.Length - i - 1] != ' ')
				{
					right = i;
					chRight = line[line.Length - i - 1];
				}
				if (left < 0 && line[i] != ' ')
				{
					left = i;
					chLeft = line[i];
				}
				if (right >= 0 && left >= 0)
				{
					break;
				}
			}
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
