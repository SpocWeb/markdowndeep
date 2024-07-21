using System;

namespace MarkdownDeep {

	/// <summary> simple class to help scan through an input string. </summary>
	/// <remarks>
	/// Maintains a current position with various operations 
	/// to inspect the current character, skip forward, check for matches, skip whitespace etc.
	/// </remarks>
	public class StringScanner {

		int _End;
		int _Mark;
		int _Pos;
		int _Start;

		public StringScanner() {}

		// Constructor
		public StringScanner(string str) {
			Reset(str);
		}

		// Constructor
		public StringScanner(string str, int pos) {
			Reset(str, pos);
		}

		// Constructor
		public StringScanner(string str, int pos, int len) {
			Reset(str, pos, len);
		}

		// Reset

		// Get the entire input string
		public string Input { get; private set; }

		// Get the character at the current position
		public char Current {
			get {
				if (_Pos < _Start || _Pos >= _End) {
					return '\0';
				}
				return Input[_Pos];
			}
		}

		// Get/set the current position
		public int Position {
			get { return _Pos; }
			set { _Pos = value; }
		}

		// Get the remainder of the input 
		// (use this in a watch window while debugging :)
		public string Remainder => Substring(Position);

		public bool Eof => _Pos >= _End;

		// Are we at eol?
		public bool Eol => IsLineEnd(Current);

		// Are we at bof?
		public bool Bof => _Pos == _Start;

		public void Reset(string str) => Reset(str, 0, str?.Length ?? 0);

		// Reset
		public void Reset(string str, int pos) => Reset(str, pos, str?.Length - pos ?? 0);

		// Reset
		public void Reset(string str, int pos, int len) {
			if (str == null) {
				str = "";
			}
			if (len < 0) {
				len = 0;
			}
			if (pos < 0) {
				pos = 0;
			}
			if (pos > str.Length) {
				pos = str.Length;
			}

			Input = str;
			_Start = pos;
			_Pos = pos;
			_End = pos + len;

			if (_End > str.Length) {
				_End = str.Length;
			}
		}

		// Skip to the end of file
		public void SkipToEof() => _Pos = _End;


		// Skip to the end of the current line
		public void SkipToEol() {
			while (_Pos < _End) {
				char ch = Input[_Pos];
				if (ch == '\r' || ch == '\n') {
					break;
				}
				_Pos++;
			}
		}

		// Skip if currently at a line end
		public bool SkipEol() {
			if (_Pos < _End) {
				char ch = Input[_Pos];
				if (ch == '\r') {
					_Pos++;
					if (_Pos < _End && Input[_Pos] == '\n') {
						_Pos++;
					}
					return true;
				}

				if (ch == '\n') {
					_Pos++;
					if (_Pos < _End && Input[_Pos] == '\r') {
						_Pos++;
					}
					return true;
				}
			}

			return false;
		}

		// Skip to the next line
		public void SkipToNextLine() {
			SkipToEol();
			SkipEol();
		}

		// Get the character at offset from current position
		// Or, \0 if out of range
		public char CharAtOffset(int offset) {
			int index = _Pos + offset;

			if (index < _Start) {
				return '\0';
			}
			if (index >= _End) {
				return '\0';
			}
			return Input[index];
		}

		// Skip a number of characters
		public void SkipForward(int characters) => _Pos += characters;

		/// <summary> Skips the given character if present </summary>
		public bool SkipChar(char ch) {
			if (Current == ch) {
				SkipForward(1);
				return true;
			}
			return false;
		}

		// Skip a matching string
		public bool SkipString(string str) {
			if (DoesMatch(str)) {
				SkipForward(str.Length);
				return true;
			}

			return false;
		}

		// Skip a matching string
		public bool SkipStringI(string str) {
			if (DoesMatchI(str)) {
				SkipForward(str.Length);
				return true;
			}

			return false;
		}

		// Skip any whitespace
		public bool SkipWhitespace() {
			if (!char.IsWhiteSpace(Current)) {
				return false;
			}
			SkipForward(1);

			while (char.IsWhiteSpace(Current))
				SkipForward(1);

			return true;
		}

		// Check if a character is space or tab
		public static bool IsLineSpace(char ch) => ch == ' ' || ch == '\t';

		// Skip spaces and tabs
		public bool SkipLinespace() {
			if (!IsLineSpace(Current)) {
				return false;
			}
			SkipForward(1);

			while (IsLineSpace(Current))
				SkipForward(1);

			return true;
		}

		// Does current character match something
		public bool DoesMatch(char ch) => Current == ch;

		// Does character at offset match a character
		public bool DoesMatch(int offset, char ch) => CharAtOffset(offset) == ch;

		// Does current character match any of a range of characters
		public bool DoesMatchAny(char[] chars) {
			for (int i = 0; i < chars.Length; i++) {
				if (DoesMatch(chars[i])) {
					return true;
				}
			}
			return false;
		}

		// Does current character match any of a range of characters
		public bool DoesMatchAny(int offset, char[] chars) {
			for (int i = 0; i < chars.Length; i++) {
				if (DoesMatch(offset, chars[i])) {
					return true;
				}
			}
			return false;
		}

		// Does current string position match a string
		public bool DoesMatch(string str) {
			for (int i = 0; i < str.Length; i++) {
				if (str[i] != CharAtOffset(i)) {
					return false;
				}
			}
			return true;
		}

		// Does current string position match a string
		public bool DoesMatchI(string str) => Substring(Position, str.Length).Equals(str, StringComparison.OrdinalIgnoreCase);

		// Extract a substring
		public string Substring(int start) => Input.Substring(start, _End - start);

		// Extract a substring
		public string Substring(int start, int len) {
			if (start + len > _End) {
				len = _End - start;
			}
			return Input.Substring(start, len);
		}

		// Scan forward for a character
		public bool Find(char ch) {
			if (_Pos >= _End) {
				return false;
			}

			// Find it
			int index = Input.IndexOf(ch, _Pos);
			if (index < 0 || index >= _End) {
				return false;
			}

			// Store new position
			_Pos = index;
			return true;
		}

		// Find any of a range of characters
		public bool FindAny(char[] chars) {
			if (_Pos >= _End) {
				return false;
			}

			// Find it
			int index = Input.IndexOfAny(chars, _Pos);
			if (index < 0 || index >= _End) {
				return false;
			}

			// Store new position
			_Pos = index;
			return true;
		}

		// Forward scan for a string
		public bool Find(string find) {
			if (_Pos >= _End) {
				return false;
			}

			int index = Input.IndexOf(find, _Pos, StringComparison.Ordinal);
			if (index < 0 || index > _End - find.Length) {
				return false;
			}

			_Pos = index;
			return true;
		}

		// Forward scan for a string (case insensitive)
		public bool FindI(string find) {
			if (_Pos >= _End) {
				return false;
			}

			int index = Input.IndexOf(find, _Pos, StringComparison.InvariantCultureIgnoreCase);
			if (index < 0 || index >= _End - find.Length) {
				return false;
			}

			_Pos = index;
			return true;
		}

		// Are we at eof?

		// Mark current position
		public void Mark() => _Mark = _Pos;

		// Extract string from mark to current position
		public string Extract() {
			if (_Mark >= _Pos) {
				return "";
			}

			return Input.Substring(_Mark, _Pos - _Mark);
		}

		// Skip an identifier
		public bool SkipIdentifier(ref string? identifier) {
			int savePos = Position;
			if (!Utils.ParseIdentifier(Input, ref _Pos, ref identifier)) {
				return false;
			}
			if (_Pos >= _End) {
				_Pos = savePos;
				return false;
			}
			return true;
		}

		public bool SkipFootnoteId(out string id) {
			int savepos = Position;

			SkipLinespace();

			Mark();

			for(;;) {
				char ch = Current;
				if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == ':' || ch == '.' || ch == ' ') {
					SkipForward(1);
				} else {
					break;
				}
			}

			if (Position > _Mark) {
				id = Extract().Trim();
				if (!string.IsNullOrEmpty(id)) {
					SkipLinespace();
					return true;
				}
			}

			Position = savepos;
			id = null;
			return false;
		}

		// Skip a Html entity (eg: &amp;)
		public bool SkipHtmlEntity(ref string entity) {
			int savepos = Position;
			if (!Utils.SkipHtmlEntity(Input, ref _Pos, ref entity)) {
				return false;
			}
			if (_Pos > _End) {
				_Pos = savepos;
				return false;
			}
			return true;
		}

		// Check if a character marks end of line
		public static bool IsLineEnd(char ch) => ch == '\r' || ch == '\n' || ch == '\0';

		public static bool IsUrlChar(char ch) {
			switch (ch) {
				case '+':
				case '&':
				case '@':
				case '#':
				case '/':
				case '%':
				case '?':
				case '=':
				case '~':
				case '_':
				case '|':
				case '[':
				case ']':
				case '(':
				case ')':
				case '!':
				case ':':
				case ',':
				case '.':
				case ';':
					return true;

				default:
					return char.IsLetterOrDigit(ch);
			}
		}

		// Attributes
	}
}