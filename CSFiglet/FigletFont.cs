using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CSFiglet
{
    public class FigletFont
    {
	    private HeaderInfo _header;
	    private string _comments;
	    private readonly List<CharInfo> _chars = new List<CharInfo>();
	    private readonly List<int> _specialVals = new List<int> {196, 214, 220, 228, 246, 252, 223};
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

	    /// <summary>
	    /// Instantiate from a figlet font file
	    /// </summary>
	    /// <param name="streamReader">StreamReader for font file</param>
	    private FigletFont(StreamReader streamReader)
		{
			_header = new HeaderInfo(streamReader);
			ReadComments(streamReader);

			var requiredValues = new List<int>();
		    var requiredComments = new List<string>();

		    for (var i = 32; i <= 126; i++)
		    {
			    requiredValues.Add(i);
			    requiredComments.Add(new string((char) i, 1));
		    }
		    requiredComments[0] = "Space";
		    for (var i = 0; i < _specialVals.Count; i++)
		    {
			    requiredValues.Add(_specialVals[i]);
				requiredComments.Add(_specialComments[i]);
			}

			CharInfo charInfo;
		    for (var iChar = 0; iChar < requiredValues.Count; iChar++)
		    {
				charInfo = ReadCharInfo(streamReader, requiredValues[iChar], requiredComments[iChar]);
		    }
			while ((charInfo = ReadCharInfo(streamReader, -1, null)) != null)
			{
				_chars.Add(charInfo);
			}
		}

		public FigletFont(string file)
			: this(new StreamReader(file)) {}

		private const string EmbeddedFilePrefix = "CSFiglet.Fonts.";
	    private const string EmbeddedFileExtension = ".flf";

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

	    public static List<string> Names()
	    {
			var prefixLength = EmbeddedFilePrefix.Length;
		    var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
		    return names.Select(name => name.Substring(prefixLength, name.LastIndexOf('.') - prefixLength)).ToList();
	    }

	    private CharInfo ReadCharInfo(StreamReader sr, int value, string comment)
	    {
		    var ret = new CharInfo(sr, value, comment, _header);
		    return ret.Valid ? ret : null;
	    }

	    private void ReadComments(StreamReader sr)
	    {
		    for (var iLine = 0; iLine < _header.CommentLines; iLine++)
		    {
			    _comments += sr.ReadLine() + "\n";
		    }
	    }
    }
}
