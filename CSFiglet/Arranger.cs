using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegexStringLibrary;

namespace CSFiglet
{
	public class Arranger
	{
		#region Private variables
		private readonly List<StringBuilder> _contents;
		private FigletFont _font;
		#endregion

		#region Properties
		public bool Smush { get; set; }
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

			var lShiftCur = 0;
			var chPrev = (char)0;
			var rightBorder = 0;
			foreach(var ch in _text)
			{
				var curChar = _font.Chars[ch];
				if (chPrev != 0)
				{
					lShiftCur = _font.Chars[chPrev].KerningOffset(curChar) + (Smush ? 1 : 0);
				}
				SetText(0, rightBorder - lShiftCur, rightBorder, curChar);
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
		}
		#endregion

		#region Block Get/Set
		internal void SetText(int row, int column, int curWidth, CharInfo charInfo)
		{
			var rowCount = charInfo.SubChars.Count;

			for (var iRow = row; iRow < row + rowCount; iRow++)
			{
				var lPadding = charInfo.LeftPads[iRow - row];
				if (lPadding == -1)
				{
					// No subcharacters
					_contents[iRow].Append(new string(' ', charInfo.Width - curWidth + column));
					continue;
				}
				var colStart = column + lPadding;
				var replacementText = charInfo.SubChars[iRow - row];
				if (iRow < 0 || iRow >= Rows)
				{
					continue;
				}
				if (curWidth > colStart)
				{
					_contents[iRow].Remove(colStart, curWidth - colStart);
				}
				else
				{
					lPadding -= colStart - curWidth;
				}
				_contents[iRow].Append(replacementText.Substring(lPadding));
			}
		}
		#endregion
	}
}
