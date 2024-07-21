using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MarkdownDeep {

	/// <summary> Scans a string of input Markdown text from a StringScanner, tokenizes it and renders the final output into a StringBuilder.  </summary>
	/// <remarks>
	/// Spans are internal to blocks (similar HTML block vs inline elements).
	/// </remarks>
	internal class SpanFormatter : StringScanner {

		/// <summary> reference to the owning markdown object, in case we need to check for formatting options </summary>
		readonly Markdown _Markdown;

		readonly List<Token> _Tokens = new();

		internal bool DisableLinks;

		public SpanFormatter(Markdown m) {
			_Markdown = m;
		}


		internal void FormatParagraph(StringBuilder dest, string str, int start, int len) {
			// Parse the string into a list of tokens
			Tokenize(str, start, len);

			// Titled image?
			if (_Tokens.Count == 1 && _Markdown.HtmlClassTitledImages != null && _Tokens[0]._Type == TokenType.img) {
				// Grab the link info
				var li = (LinkInfo) _Tokens[0]._Data;

				// Render the div opening
				dest.Append("<div class=\"");
				dest.Append(_Markdown.HtmlClassTitledImages);
				dest.Append("\">\n");

				// Render the img
				_Markdown.RenderingTitledImage = true;
				Render(dest, str);
				_Markdown.RenderingTitledImage = false;
				dest.Append("\n");

				// Render the title
				if (!string.IsNullOrEmpty(li._Def.Title)) {
					dest.Append("<p>");
					Utils.SmartHtmlEncodeAmpsAndAngles(dest, li._Def.Title);
					dest.Append("</p>\n");
				}

				dest.Append("</div>\n");
			} else {
				// Render the paragraph
				dest.Append("<p>");
				Render(dest, str);
				dest.Append("</p>\n");
			}
		}

		internal void Format(StringBuilder dest, string str) => Format(dest, str, 0, str.Length);

		// Format a range in an input string and write it to the destination string builder.
		internal void Format(StringBuilder dest, string str, int start, int len) {
			// Parse the string into a list of tokens
			Tokenize(str, start, len);

			// Render all tokens
			Render(dest, str);
		}

		internal void FormatPlain(StringBuilder dest, string str, int start, int len) {
			// Parse the string into a list of tokens
			Tokenize(str, start, len);

			// Render all tokens
			RenderPlain(dest, str);
		}

		// Format a string and return it as a new string
		// (used in formatting the text of links)
		internal string Format(string str) {
			var dest = new StringBuilder();
			Format(dest, str, 0, str.Length);
			return dest.ToString();
		}

		internal string MakeId(string str) => MakeId(str, 0, str.Length);

		internal string MakeId(string str, int start, int len) {
			// Parse the string into a list of tokens
			Tokenize(str, start, len);

			var sb = new StringBuilder();

			foreach (Token t in _Tokens) {
				switch (t._Type) {
					case TokenType.Text:
						sb.Append(str, t._StartOffset, t._Length);
						break;

					case TokenType.link:
						var li = (LinkInfo) t._Data;
						sb.Append(li._LinkText);
						break;
				}

				FreeToken(t);
			}

			// Now clean it using the same rules as pandoc
			Reset(sb.ToString());

			// Skip everything up to the first letter
			while (!Eof) {
				if (char.IsLetter(Current)) {
					break;
				}
				SkipForward(1);
			}

			// Process all characters
			sb.Length = 0;
			while (!Eof) {
				char ch = Current;
				if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.') {
					sb.Append(char.ToLower(ch));
				} else if (ch == ' ') {
					sb.Append("-");
				} else if (IsLineEnd(ch)) {
					sb.Append("-");
					SkipEol();
					continue;
				}

				SkipForward(1);
			}

			return sb.ToString();
		}

		// Render a list of tokens to a destinatino string builder.
		void Render(StringBuilder sb, string str) {
			foreach (Token t in _Tokens) {
				switch (t._Type) {
					case TokenType.Text:
						// Append encoded text
						_Markdown.HtmlEncode(sb, str, t._StartOffset, t._Length);
						break;

					case TokenType.HtmlTag:
						// Append html as is
						Utils.SmartHtmlEncodeAmps(sb, str, t._StartOffset, t._Length);
						break;

					case TokenType.Html:
					case TokenType.opening_mark:
					case TokenType.closing_mark:
					case TokenType.internal_mark:
						// Append html as is
						sb.Append(str, t._StartOffset, t._Length);
						break;

					case TokenType.br:
						sb.Append("<br />\n");
						break;

					case TokenType.open_em:
						sb.Append("<em>");
						break;

					case TokenType.close_em:
						sb.Append("</em>");
						break;

					case TokenType.open_strong:
						sb.Append("<strong>");
						break;

					case TokenType.close_strong:
						sb.Append("</strong>");
						break;

					case TokenType.code_span:
						sb.Append("<code>");
						_Markdown.HtmlEncode(sb, str, t._StartOffset, t._Length);
						sb.Append("</code>");
						break;

					case TokenType.link: {
						var li = (LinkInfo) t._Data;
						var sf = new SpanFormatter(_Markdown) {DisableLinks = true};

						li._Def.RenderLink(_Markdown, sb, sf.Format(li._LinkText));
						break;
					}

					case TokenType.img: {
						var li = (LinkInfo) t._Data;
						li._Def.RenderImg(_Markdown, sb, li._LinkText);
						break;
					}

					case TokenType.footnote: {
						var r = (FootnoteReference) t._Data;
						sb.Append("<sup id=\"fnref:");
						sb.Append(r.Id);
						sb.Append("\"><a href=\"#fn:");
						sb.Append(r.Id);
						sb.Append("\" rel=\"footnote\">");
						sb.Append(r.Index + 1);
						sb.Append("</a></sup>");
						break;
					}

					case TokenType.abbreviation: {
						var a = (Abbreviation) t._Data;
						sb.Append("<abbr");
						if (!string.IsNullOrEmpty(a.Title)) {
							sb.Append(" title=\"");
							_Markdown.HtmlEncode(sb, a.Title, 0, a.Title.Length);
							sb.Append("\"");
						}
						sb.Append(">");
						_Markdown.HtmlEncode(sb, a.Abbr, 0, a.Abbr.Length);
						sb.Append("</abbr>");
						break;
					}
				}

				FreeToken(t);
			}
		}

		// Render a list of tokens to a destinatino string builder.
		void RenderPlain(StringBuilder sb, string str) {
			foreach (Token t in _Tokens) {
				switch (t._Type) {
					case TokenType.Text:
						sb.Append(str, t._StartOffset, t._Length);
						break;

					case TokenType.HtmlTag:
						break;

					case TokenType.Html:
					case TokenType.opening_mark:
					case TokenType.closing_mark:
					case TokenType.internal_mark:
						break;

					case TokenType.br:
						break;

					case TokenType.open_em:
					case TokenType.close_em:
					case TokenType.open_strong:
					case TokenType.close_strong:
						break;

					case TokenType.code_span:
						sb.Append(str, t._StartOffset, t._Length);
						break;

					case TokenType.link: {
						var li = (LinkInfo) t._Data;
						sb.Append(li._LinkText);
						break;
					}

					case TokenType.img: {
						var li = (LinkInfo) t._Data;
						sb.Append(li._LinkText);
						break;
					}

					case TokenType.footnote:
					case TokenType.abbreviation:
						break;
				}

				FreeToken(t);
			}
		}

		// Scan the input string, creating tokens for anything special 
		public void Tokenize(string str, int start, int len) {
			// Prepare
			Reset(str, start, len);
			_Tokens.Clear();

			List<Token> emphasisMarks = null;

			List<Abbreviation>? abbreviations = _Markdown.GetAbbreviations();
			bool extraMode = _Markdown.IsExtraMode;

			// Scan string
			int startTextToken = Position;
			while (!Eof) {
				int endTextToken = Position;

				// Work out token
				Token token = null;
				switch (Current) {
					case '*':
					case '_':

						// Create emphasis mark
						token = CreateEmphasisMark();

						if (token != null) {
							// Store marks in a separate list the we'll resolve later
							switch (token._Type) {
								case TokenType.internal_mark:
								case TokenType.opening_mark:
								case TokenType.closing_mark:
									if (emphasisMarks == null) {
										emphasisMarks = new List<Token>();
									}
									emphasisMarks.Add(token);
									break;
							}
						}
						break;

					case '`':
						token = ProcessCodeSpan();
						break;

					case '[':
					case '!': {
						// Process link reference
						int linkpos = Position;
						token = ProcessLinkOrImageOrFootnote();

						// Rewind if invalid syntax
						// (the '[' or '!' will be treated as a regular character and processed below)
						if (token == null) {
							Position = linkpos;
						}
						break;
					}

					case '<': {
						// Is it a valid html tag?
						int save = Position;
						HtmlTag tag = this.ParseHtml();
						if (tag != null) {
							if (!_Markdown.IsSafeMode || tag.IsSafe()) {
								// Yes, create a token for it
								token = CreateToken(TokenType.HtmlTag, save, Position - save);
							} else {
								// No, rewrite and encode it
								Position = save;
							}
						} else {
							// No, rewind and check if it's a valid autolink eg: <google.com>
							Position = save;
							token = ProcessAutoLink();

							if (token == null) {
								Position = save;
							}
						}
						break;
					}

					case '&': {
						// Is it a valid html entity
						int save = Position;
						string unused = null;
						if (SkipHtmlEntity(ref unused)) {
							// Yes, create a token for it
							token = CreateToken(TokenType.Html, save, Position - save);
						}

						break;
					}

					case ' ': {
						// Check for double space at end of a line
						if (CharAtOffset(1) == ' ' && IsLineEnd(CharAtOffset(2))) {
							// Yes, skip it
							SkipForward(2);

							// Don't put br's at the end of a paragraph
							if (!Eof) {
								SkipEol();
								token = CreateToken(TokenType.br, endTextToken, 0);
							}
						}
						break;
					}

					case '\\': {
						// Special handling for escaping <autolinks>
						/*
						if (CharAtOffset(1) == '<')
						{
							// Is it an autolink?
							int savepos = position;
							SkipForward(1);
							bool AutoLink = ProcessAutoLink() != null;
							position = savepos;

							if (AutoLink)
							{
								token = CreateToken(TokenType.Text, position + 1, 1);
								SkipForward(2);
							}
						}
						else
						 */
						{
							// Check followed by an escapable character
							if (Utils.IsEscapableChar(CharAtOffset(1), extraMode)) {
								token = CreateToken(TokenType.Text, Position + 1, 1);
								SkipForward(2);
							}
						}
						break;
					}
				}

				// Look for abbreviations.
				if (token == null && abbreviations != null && !char.IsLetterOrDigit(CharAtOffset(-1))) {
					int savepos = Position;
					foreach (Abbreviation abbr in abbreviations) {
						if (SkipString(abbr.Abbr) && !char.IsLetterOrDigit(Current)) {
							token = CreateToken(TokenType.abbreviation, abbr);
							break;
						}

						Position = savepos;
					}
				}

				// If token found, append any preceeding text and the new token to the token list
				if (token != null) {
					// Create a token for everything up to the special character
					if (endTextToken > startTextToken) {
						_Tokens.Add(CreateToken(TokenType.Text, startTextToken, endTextToken - startTextToken));
					}

					// Add the new token
					_Tokens.Add(token);

					// Remember where the next text token starts
					startTextToken = Position;
				} else {
					// Skip a single character and keep looking
					SkipForward(1);
				}
			}

			// Append a token for any trailing text after the last token.
			if (Position > startTextToken) {
				_Tokens.Add(CreateToken(TokenType.Text, startTextToken, Position - startTextToken));
			}

			// Do we need to resolve and emphasis marks?
			if (emphasisMarks != null) {
				ResolveEmphasisMarks(_Tokens, emphasisMarks);
			}

			// Done!
		}

		static bool IsEmphasisChar(char ch) => ch == '_' || ch == '*';

		/*
		 * Resolving emphasis tokens is a two part process
		 * 
		 * 1. Find all valid sequences of * and _ and create `mark` tokens for them
		 *		this is done by CreateEmphasisMarks during the initial character scan
		 *		done by Tokenize
		 *		
		 * 2. Looks at all these emphasis marks and tries to pair them up
		 *		to make the actual <em> and <strong> tokens
		 *		
		 * Any unresolved emphasis marks are rendered unaltered as * or _
		 */

		// Create emphasis mark for sequences of '*' and '_' (part 1)
		public Token CreateEmphasisMark() {
			// Capture current state
			char ch = Current;
			//char altch = ch == '*' ? '_' : '*';
			int savepos = Position;

			// Check for a consecutive sequence of just '_' and '*'
			if (Bof || char.IsWhiteSpace(CharAtOffset(-1))) {
				while (IsEmphasisChar(Current))
					SkipForward(1);

				if (Eof || char.IsWhiteSpace(Current)) {
					return new Token(TokenType.Html, savepos, Position - savepos);
				}

				// Rewind
				Position = savepos;
			}

			// Scan backwards and see if we have space before
			while (IsEmphasisChar(CharAtOffset(-1)))
				SkipForward(-1);
			bool bSpaceBefore = Bof || char.IsWhiteSpace(CharAtOffset(-1));
			Position = savepos;

			// Count how many matching emphasis characters
			while (Current == ch) {
				SkipForward(1);
			}
			int count = Position - savepos;

			// Scan forwards and see if we have space after
			while (IsEmphasisChar(CharAtOffset(1)))
				SkipForward(1);
			bool bSpaceAfter = Eof || char.IsWhiteSpace(Current);
			Position = savepos + count;

			// This should have been stopped by check above
			Debug.Assert(!bSpaceBefore || !bSpaceAfter);

			if (bSpaceBefore) {
				return CreateToken(TokenType.opening_mark, savepos, Position - savepos);
			}

			if (bSpaceAfter) {
				return CreateToken(TokenType.closing_mark, savepos, Position - savepos);
			}

			if (_Markdown.IsExtraMode && ch == '_' && (char.IsLetterOrDigit(Current))) {
				return null;
			}

			return CreateToken(TokenType.internal_mark, savepos, Position - savepos);
		}

		// Split mark token
		public Token SplitMarkToken(List<Token> tokens, List<Token> marks, Token token, int position) {
			// Create the new rhs token
			Token tokenRhs = CreateToken(token._Type, token._StartOffset + position, token._Length - position);

			// Adjust down the length of this token
			token._Length = position;

			// Insert the new token into each of the parent collections
			marks.Insert(marks.IndexOf(token) + 1, tokenRhs);
			tokens.Insert(tokens.IndexOf(token) + 1, tokenRhs);

			// Return the new token
			return tokenRhs;
		}

		// Resolve emphasis marks (part 2)
		public void ResolveEmphasisMarks(List<Token> tokens, List<Token> marks) {
			bool bContinue = true;
			while (bContinue) {
				bContinue = false;
				for (int i = 0; i < marks.Count; i++) {
					// Get the next opening or internal mark
					Token openingMark = marks[i];
					if (openingMark._Type != TokenType.opening_mark && openingMark._Type != TokenType.internal_mark) {
						continue;
					}

					// Look for a matching closing mark
					for (int j = i + 1; j < marks.Count; j++) {
						// Get the next closing or internal mark
						Token closingMark = marks[j];
						if (closingMark._Type != TokenType.closing_mark && closingMark._Type != TokenType.internal_mark) {
							break;
						}

						// Ignore if different type (ie: `*` vs `_`)
						if (Input[openingMark._StartOffset] != Input[closingMark._StartOffset]) {
							continue;
						}

						// strong or em?
						int style = Math.Min(openingMark._Length, closingMark._Length);

						// Triple or more on both ends?
						if (style >= 3) {
							style = (style%2) == 1 ? 1 : 2;
						}

						// Split the opening mark, keeping the RHS
						if (openingMark._Length > style) {
							openingMark = SplitMarkToken(tokens, marks, openingMark, openingMark._Length - style);
							i--;
						}

						// Split the closing mark, keeping the LHS
						if (closingMark._Length > style) {
							SplitMarkToken(tokens, marks, closingMark, style);
						}

						// Connect them
						openingMark._Type = style == 1 ? TokenType.open_em : TokenType.open_strong;
						closingMark._Type = style == 1 ? TokenType.close_em : TokenType.close_strong;

						// Remove the matched marks
						marks.Remove(openingMark);
						marks.Remove(closingMark);
						bContinue = true;

						break;
					}
				}
			}
		}

		// Resolve emphasis marks (part 2)
		public void ResolveEmphasisMarks_classic(List<Token> tokens, List<Token> marks) {
			// First pass, do <strong>
			for (int i = 0; i < marks.Count; i++) {
				// Get the next opening or internal mark
				Token openingMark = marks[i];
				if (openingMark._Type != TokenType.opening_mark && openingMark._Type != TokenType.internal_mark) {
					continue;
				}
				if (openingMark._Length < 2) {
					continue;
				}

				// Look for a matching closing mark
				for (int j = i + 1; j < marks.Count; j++) {
					// Get the next closing or internal mark
					Token closingMark = marks[j];
					if (closingMark._Type != TokenType.closing_mark && closingMark._Type != TokenType.internal_mark) {
						continue;
					}

					// Ignore if different type (ie: `*` vs `_`)
					if (Input[openingMark._StartOffset] != Input[closingMark._StartOffset]) {
						continue;
					}

					// Must be at least two
					if (closingMark._Length < 2) {
						continue;
					}

					// Split the opening mark, keeping the LHS
					if (openingMark._Length > 2) {
						SplitMarkToken(tokens, marks, openingMark, 2);
					}

					// Split the closing mark, keeping the RHS
					if (closingMark._Length > 2) {
						closingMark = SplitMarkToken(tokens, marks, closingMark, closingMark._Length - 2);
					}

					// Connect them
					openingMark._Type = TokenType.open_strong;
					closingMark._Type = TokenType.close_strong;

					// Continue after the closing mark
					i = marks.IndexOf(closingMark);
					break;
				}
			}

			// Second pass, do <em>
			for (int i = 0; i < marks.Count; i++) {
				// Get the next opening or internal mark
				Token openingMark = marks[i];
				if (openingMark._Type != TokenType.opening_mark && openingMark._Type != TokenType.internal_mark) {
					continue;
				}

				// Look for a matching closing mark
				for (int j = i + 1; j < marks.Count; j++) {
					// Get the next closing or internal mark
					Token closingMark = marks[j];
					if (closingMark._Type != TokenType.closing_mark && closingMark._Type != TokenType.internal_mark) {
						continue;
					}

					// Ignore if different type (ie: `*` vs `_`)
					if (Input[openingMark._StartOffset] != Input[closingMark._StartOffset]) {
						continue;
					}

					// Split the opening mark, keeping the LHS
					if (openingMark._Length > 1) {
						SplitMarkToken(tokens, marks, openingMark, 1);
					}

					// Split the closing mark, keeping the RHS
					if (closingMark._Length > 1) {
						closingMark = SplitMarkToken(tokens, marks, closingMark, closingMark._Length - 1);
					}

					// Connect them
					openingMark._Type = TokenType.open_em;
					closingMark._Type = TokenType.close_em;

					// Continue after the closing mark
					i = marks.IndexOf(closingMark);
					break;
				}
			}
		}

		// Process '*', '**' or '_', '__'
		// This is horrible and probably much better done through regex, but I'm stubborn.
		// For normal cases this routine works as expected.  For unusual cases (eg: overlapped
		// strong and emphasis blocks), the behaviour is probably not the same as the original
		// markdown scanner.
		/*
		public Token ProcessEmphasisOld(ref Token prev_single, ref Token prev_double)
		{
			// Check whitespace before/after
			bool bSpaceBefore = !bof && IsLineSpace(CharAtOffset(-1));
			bool bSpaceAfter = IsLineSpace(CharAtOffset(1));

			// Ignore if surrounded by whitespace
			if (bSpaceBefore && bSpaceAfter)
			{
				return null;
			}

			// Save the current character and skip it
			char ch = current;
			Skip(1);

			// Do we have a previous matching single star?
			if (!bSpaceBefore && prev_single != null)
			{
				// Yes, match them...
				prev_single.type = TokenType.open_em;
				prev_single = null;
				return CreateToken(TokenType.close_em, position - 1, 1);
			}

			// Is this a double star/under
			if (current == ch)
			{
				// Skip second character
				Skip(1);

				// Space after?
				bSpaceAfter = IsLineSpace(current);

				// Space both sides?
				if (bSpaceBefore && bSpaceAfter)
				{
					// Ignore it
					return CreateToken(TokenType.Text, position - 2, 2);
				}

				// Do we have a previous matching double
				if (!bSpaceBefore && prev_double != null)
				{
					// Yes, match them
					prev_double.type = TokenType.open_strong;
					prev_double = null;
					return CreateToken(TokenType.close_strong, position - 2, 2);
				}

				if (!bSpaceAfter)
				{
					// Opening double star
					prev_double = CreateToken(TokenType.Text, position - 2, 2);
					return prev_double;
				}

				// Ignore it
				return CreateToken(TokenType.Text, position - 2, 2);
			}

			// If there's a space before, we can open em
			if (!bSpaceAfter)
			{
				// Opening single star
				prev_single = CreateToken(TokenType.Text, position - 1, 1);
				return prev_single;
			}

			// Ignore
			Skip(-1);
			return null;
		}
		 */

		// Process auto links eg: <google.com>
		Token ProcessAutoLink() {
			if (DisableLinks) {
				return null;
			}

			// Skip the angle bracket and remember the start
			SkipForward(1);
			Mark();

			bool isExtraMode = _Markdown.IsExtraMode;

			// Allow anything up to the closing angle, watch for escapable characters
			while (!Eof) {
				char ch = Current;

				// No whitespace allowed
				if (char.IsWhiteSpace(ch)) {
					break;
				}

				// End found?
				if (ch == '>') {
					string url = Utils.UnescapeString(Extract(), isExtraMode);

					LinkInfo li = null;
					if (Utils.IsEmailAddress(url)) {
						string linkText;
						if (url.StartsWith("mailto:")) {
							linkText = url.Substring(7);
						} else {
							linkText = url;
							url = "mailto:" + url;
						}

						li = new LinkInfo(new LinkDefinition("auto", url), linkText);
					} else if (Utils.IsWebAddress(url)) {
						li = new LinkInfo(new LinkDefinition("auto", url), url);
					}

					if (li != null) {
						SkipForward(1);
						return CreateToken(TokenType.link, li);
					}

					return null;
				}

				this.SkipEscapableChar(isExtraMode);
			}

			// Didn't work
			return null;
		}

		// Process [link] and ![image] directives
		Token ProcessLinkOrImageOrFootnote() {
			// Link or image?
			TokenType tokenType = SkipChar('!') ? TokenType.img : TokenType.link;

			// Opening '['
			if (!SkipChar('[')) {
				return null;
			}

			// Is it a foonote?
			int savepos = Position;
			if (_Markdown.IsExtraMode && tokenType == TokenType.link && SkipChar('^')) {
				SkipLinespace();

				// Parse it
				if (SkipFootnoteId(out var id) && SkipChar(']')) {
					// Look it up and create footnote reference token
					int footnoteIndex = _Markdown.ClaimFootnote(id);
					if (footnoteIndex >= 0) {
						// Yes it's a footnote
						return CreateToken(TokenType.footnote, new FootnoteReference(footnoteIndex, id));
					}
				}

				// Rewind
				Position = savepos;
			}

			if (DisableLinks && tokenType == TokenType.link) {
				return null;
			}

			bool extraMode = _Markdown.IsExtraMode;

			// Find the closing square bracket, allowing for nesting, watching for 
			// escapable characters
			Mark();
			int depth = 1;
			while (!Eof) {
				char ch = Current;
				if (ch == '[') {
					depth++;
				} else if (ch == ']') {
					depth--;
					if (depth == 0) {
						break;
					}
				}

				this.SkipEscapableChar(extraMode);
			}

			// Quit if end
			if (Eof) {
				return null;
			}

			// Get the link text and unescape it
			string linkText = Utils.UnescapeString(Extract(), extraMode);

			// The closing ']'
			SkipForward(1);

			// Save position in case we need to rewind
			savepos = Position;

			// Inline links must follow immediately
			if (SkipChar('(')) {
				// Extract the url and title
				LinkDefinition linkDef = LinkDefinition.ParseLinkTarget(this, null, _Markdown.IsExtraMode);
				if (linkDef == null) {
					return null;
				}

				// Closing ')'
				SkipWhitespace();
				if (!SkipChar(')')) {
					return null;
				}

				// Create the token
				return CreateToken(tokenType, new LinkInfo(linkDef, linkText));
			}

			// Optional space or tab
			if (!SkipChar(' ')) {
				SkipChar('\t');
			}

			// If there's line end, we're allow it and as must line space as we want
			// before the link id.
			if (Eol) {
				SkipEol();
				SkipLinespace();
			}

			// Reference link?
			string linkId = null;
			if (Current == '[') {
				// Skip the opening '['
				SkipForward(1);

				// Find the start/end of the id
				Mark();
				if (!Find(']')) {
					return null;
				}

				// Extract the id
				linkId = Extract();

				// Skip closing ']'
				SkipForward(1);
			} else {
				// Rewind to just after the closing ']'
				Position = savepos;
			}

			// Link id not specified?
			if (string.IsNullOrEmpty(linkId)) {
				// Use the link text (implicit reference link)
				linkId = Utils.NormalizeLineEnds(linkText);

				// If the link text has carriage returns, normalize
				// to spaces
				if (!ReferenceEquals(linkId, linkText)) {
					while (linkId.Contains(" \n"))
						linkId = linkId.Replace(" \n", "\n");
					linkId = linkId.Replace("\n", " ");
				}
			}

			// Find the link definition abort if not defined
			LinkDefinition def = _Markdown.GetLinkDefinition(linkId);
			if (def == null) {
				return null;
			}

			// Create a token
			return CreateToken(tokenType, new LinkInfo(def, linkText));
		}

		// Process a ``` code span ```
		Token ProcessCodeSpan() {
			int start = Position;

			// Count leading ticks
			int tickcount = 0;
			while (SkipChar('`')) {
				tickcount++;
			}

			// Skip optional leading space...
			SkipWhitespace();

			// End?
			if (Eof) {
				return CreateToken(TokenType.Text, start, Position - start);
			}

			int startofcode = Position;

			// Find closing ticks
			if (!Find(Substring(start, tickcount))) {
				return CreateToken(TokenType.Text, start, Position - start);
			}

			// Save end position before backing up over trailing whitespace
			int endpos = Position + tickcount;
			while (char.IsWhiteSpace(CharAtOffset(-1)))
				SkipForward(-1);

			// Create the token, move back to the end and we're done
			Token ret = CreateToken(TokenType.code_span, startofcode, Position - startofcode);
			Position = endpos;
			return ret;
		}

		#region Token Pooling

		// CreateToken - create or re-use a token object
		readonly Stack<Token> _SpareTokens = new();

		internal Token CreateToken(TokenType type, int startOffset, int length) {
			if (_SpareTokens.Count != 0) {
				Token t = _SpareTokens.Pop();
				t._Type = type;
				t._StartOffset = startOffset;
				t._Length = length;
				t._Data = null;
				return t;
			}
			return new Token(type, startOffset, length);
		}

		// CreateToken - create or re-use a token object
		internal Token CreateToken(TokenType type, object data) {
			if (_SpareTokens.Count != 0) {
				Token t = _SpareTokens.Pop();
				t._Type = type;
				t._Data = data;
				return t;
			}
			return new Token(type, data);
		}

		// FreeToken - return a token to the spare token pool
		internal void FreeToken(Token token) {
			token._Data = null;
			_SpareTokens.Push(token);
		}

		#endregion
	}
}