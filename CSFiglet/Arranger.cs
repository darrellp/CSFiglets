using System.Collections.Generic;
using System.Text;

namespace CSFiglet
{
	public class Arranger
	{
		#region Private variables
		private readonly List<StringBuilder> _contents;
		private readonly FigletFont _font;
		#endregion

		#region Properties
		public CharacterSpacing CharacterSpacing { get; set; }
		public HSmushRule SmushRule { get; set; }

		public bool Smush
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
				for (var iRow = 0; iRow < Rows; iRow++)
				{
					sb.Append(_contents[iRow] + (iRow == Rows - 1 ? "" : "\n"));
				}
				return sb.ToString().Replace(_font.Header.HardBlank, ' ');
			}
		}
		private int Rows { get; set; }
		private int Columns { get; set; }

		private string _text;
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

		private void Arrange()
		{
			Rows = _font.Header.Height;
	
			for (var iRow = 0; iRow < Rows; iRow++)
			{
				_contents.Add(new StringBuilder());
			}

			var lShiftCur = 0;							// Amount to "shift" right char into the left one
			var chPrev = (char)0;
			var rightBorder = 0;
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
					if (smushable && Smush)
					{
						lShiftCur++;
					}
				}
				SetText(0, rightBorder - lShiftCur, rightBorder, curChar, Smush && smushable);
				rightBorder += curChar.Width - lShiftCur;
				chPrev = ch;
			}
		}
		#endregion

		#region Constructor
		public Arranger(FigletFont font, int rows = -1, int cols = -1)
		{
			Rows = rows;
			Columns = cols;
			_contents = new List<StringBuilder>();
			_font = font;
			SetCharSpacing();
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
		#endregion

		#region Block Get/Set
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
					_contents[iRow].Append(new string(' ', charInfo.Width - curWidth + column));
					continue;
				}

				// absolute column start of non-padding portion of the character
				var colStart = column + lPadding;
				// New string we want to place into the row
				var replacementText = charInfo.SubChars[iRow - row];

				if (curWidth > colStart)
				{
					// We have to actually replace part of the last character we've already placed
					if (doSmush)
					{
						// Left char to smush comes from the contents
						var lSmushChar = _contents[iRow][colStart];
						// Right char to smuch comes from replacement text
						var rSmushChar = replacementText[lPadding];

						// Replace _contents char with the smushability char
						_contents[iRow][colStart] = 
							CharInfo.CheckSmushability(SmushRule, lSmushChar, rSmushChar, _font.Header.HardBlank);

						// This will cause the smushed char to be skipped in replacement text
						lPadding++;
						// This will keep us from deleting the newly placed smush char from _contents
						colStart++;
					}
					// Eliminate blanks that need replacing
					_contents[iRow].Remove(colStart, curWidth - colStart);
				}
				else
				{
					// The replacement comes after our current last column so include
					// some of his padding to position him correctly
					lPadding -= colStart - curWidth;
				}
				
				// Put the replacement text in properly
				_contents[iRow].Append(replacementText.Substring(lPadding));
			}
		}
		#endregion
	}
}
