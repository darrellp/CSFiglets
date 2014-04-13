using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSFiglet
{
	public class Arranger
	{
		#region Private variables
		private List<string> _contents;
		private readonly List<StringBuilder> _stagingArea; 
		private readonly FigletFont _font;
		private string _text;
		#endregion

		#region Properties
		public bool DoSmush
		{
			get
			{
				return SmushRule != HSmushRule.NoSmush;
			}
		}

		public string StringContents
		{
			get
			{
				var sb = new StringBuilder();
				for (var iRow = 0; iRow < _contents.Count; iRow++)
				{
					sb.Append(_contents[iRow] + (iRow == _contents.Count - 1 ? "" : "\n"));
				}
				return sb.ToString().Replace(_font.Header.HardBlank, ' ');
			}
		}

		public string[] Contents
		{
			get
			{
				return _contents.Select(l => l.Length < MaxWidth ? l : l.Substring(0, MaxWidth)).ToArray();
			}
		}

		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				_text = value;
				Arrange();
			}
		}

		public CharacterSpacing CharacterSpacing { get; set; }
		public HSmushRule SmushRule { get; set; }

		private int MaxWidth { get; set; }
		#endregion

		#region Constructor
		public Arranger(FigletFont font, int maxWidth = int.MaxValue)
		{
			_stagingArea = new List<StringBuilder>();
			_font = font;
			MaxWidth = maxWidth;
			SetCharSpacing();
			for (var iRow = 0; iRow < _font.Header.Height; iRow++)
			{
				_stagingArea.Add(new StringBuilder());
			}
		}
		#endregion

		#region Arrangement
		private void Arrange()
		{
			_contents = new List<string>();
			var lShiftCur = 0;							// Amount to "shift" right char into the left one
			var chPrev = (char)0;
			var rightBorder = 0;
			var stagedCharCount = 0;
			foreach(var ch in _text)
			{
				var curChar = _font.Chars[ch];
				var smushable = false;					// True if the characters can be smushed

				if (CharacterSpacing == CharacterSpacing.FullWidth)
				{
					// No shifting occurs
					lShiftCur = 0;
				}
				else if (chPrev != 0)
				{
					lShiftCur = _font.Chars[chPrev].KerningOffset(curChar, SmushRule, out smushable);
					if (smushable && DoSmush)
					{
						lShiftCur++;
					}
				}

				// Have we extended beyond the right border with more than one character?
				if (rightBorder + curChar.Width - lShiftCur > MaxWidth && stagedCharCount != 0)
				{
					// Yes, move the staging text to contents
					TransferStagingToContents(0);
					// ...and clear the staging area
					foreach (var stagingRow in _stagingArea)
					{
						stagingRow.Clear();
					}
					stagedCharCount = 0;
					lShiftCur = 0;
					chPrev = (char) 0;
					rightBorder = 0;
				}
				stagedCharCount++;
				SetText(0, rightBorder - lShiftCur, rightBorder, curChar, DoSmush && smushable);
				rightBorder += curChar.Width - lShiftCur;
				chPrev = ch;
			}
			TransferStagingToContents(0);
		}

		private void TransferStagingToContents(int padding)
		{
			var paddingString = padding > 0 ? new string(' ', padding) : string.Empty;

			foreach (var row in _stagingArea)
			{
				var justifiedLine = paddingString + (padding > 0 ? row.ToString() : row.ToString().Substring(-padding));
				var clippedLine = justifiedLine.Length <= MaxWidth ? justifiedLine : justifiedLine.Substring(0, MaxWidth);
				_contents.Add(clippedLine);
			}
		}

		public void SetCharSpacing()
		{
			var header = _font.Header;
			SmushRule = HSmushRule.NoSmush;
			if ((int)header.OldLayout == -1)
			{
				CharacterSpacing = CharacterSpacing.FullWidth;
			}
			else if (header.OptionalValuesPresent && (header.FullLayout.HasFlag(HSmushRule.KerningByDefault)))
			{
				CharacterSpacing = CharacterSpacing.Kerning;
			}
			else
			{
				CharacterSpacing = CharacterSpacing.Smushing;
				SmushRule = _font.Header.OptionalValuesPresent ?
					_font.Header.FullLayout :
					_font.Header.OldLayout;
			}
		}

		internal void SetText(int row, int column, int curWidth, CharInfo charInfo, bool doSmush)
		{
			var rowCount = charInfo.SubChars.Count;

			for (var iRow = row; iRow < row + rowCount; iRow++)
			{
				// Left padding for our righthand character
				var lPadding = charInfo.LeftPads[iRow - row];
				if (lPadding == -1)
				{
					// lPadding == -1 conventionally means no subchars in this row so we just
					// append on the proper amount of spacing and continue on
					_stagingArea[iRow].Append(new string(' ', charInfo.Width - curWidth + column));
					continue;
				}

				// absolute column start of non-padding portion of the character
				var colStart = column + lPadding;
				// New string we want to place into the row
				var replacementText = charInfo.SubChars[iRow - row];

				if (curWidth > colStart)
				{
					// We have to actually replace part of the last character we've already placed - i.e., we're
					// kerning into the body of the previous character.
					if (doSmush)
					{
						// Left char to smush comes from the stagingArea
						var lSmushChar = _stagingArea[iRow][colStart];
						// Right char to smush comes from replacement text
						var rSmushChar = replacementText[lPadding];

						// Replace _stagingArea char with the smushability char
						_stagingArea[iRow][colStart] = 
							CharInfo.CheckSmushability(SmushRule, lSmushChar, rSmushChar, _font.Header.HardBlank);

						// This will cause the smushed char to be skipped in replacement text
						lPadding++;
						// This will keep us from deleting the newly placed smush char from _stagingArea
						colStart++;
					}
					// Eliminate blanks that need replacing
					_stagingArea[iRow].Remove(colStart, curWidth - colStart);
				}
				else
				{
					// The replacement comes after our current last column so include
					// some of his padding to position him correctly
					lPadding -= colStart - curWidth;
				}
				
				// Put the replacement text in properly
				_stagingArea[iRow].Append(replacementText.Substring(lPadding));
			}
		}
		#endregion
	}
}
