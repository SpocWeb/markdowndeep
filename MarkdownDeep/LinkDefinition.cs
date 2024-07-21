using System.Text;

namespace MarkdownDeep {

	/// <summary> Link or image definition  </summary>
	/// <remarks>
	/// Either parsed from a Markdown reference style link definition, or an inline link definition. 
	/// Has static methods for parsing a link definition or link target from a StringScanner. 
	/// A LinkDefinition doesn't include the link text - <see cref="LinkInfo"/> for that.
	/// </remarks>
	public class LinkDefinition {

		public LinkDefinition(string id, string url = null, string title = null) {
			Id = id;
			Url = url;
			Title = title;
		}

		public string Id { get; set; }

		public string Url { get; set; }

		public string Title { get; set; }

		internal void RenderLink(Markdown m, StringBuilder b, string linkText) {
			if (Url.StartsWith("mailto:")) {
				b.Append("<a href=\"");
				Utils.HtmlRandomize(b, Url);
				b.Append('\"');
				if (!string.IsNullOrEmpty(Title)) {
					b.Append(" title=\"");
					Utils.SmartHtmlEncodeAmpsAndAngles(b, Title);
					b.Append('\"');
				}
				b.Append('>');
				Utils.HtmlRandomize(b, linkText);
				b.Append("</a>");
			} else {
				var tag = new HtmlTag("a");

				// encode url
				StringBuilder sb = m.GetStringBuilder();
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, Url);
				tag.Attributes["href"] = sb.ToString();

				// encode title
				if (!string.IsNullOrEmpty(Title)) {
					sb.Length = 0;
					Utils.SmartHtmlEncodeAmpsAndAngles(sb, Title);
					tag.Attributes["title"] = sb.ToString();
				}

				// Do user processing
				m.OnPrepareLink(tag);

				// Render the opening tag
				tag.RenderOpening(b);

				b.Append(linkText); // Link text already escaped by SpanFormatter
				b.Append("</a>");
			}
		}

		internal void RenderImg(Markdown m, StringBuilder b, string altText) {
			var tag = new HtmlTag("img");

			// encode url
			StringBuilder sb = m.GetStringBuilder();
			Utils.SmartHtmlEncodeAmpsAndAngles(sb, Url);
			tag.Attributes["src"] = sb.ToString();

			// encode alt text
			if (!string.IsNullOrEmpty(altText)) {
				sb.Length = 0;
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, altText);
				tag.Attributes["alt"] = sb.ToString();
			}

			// encode title
			if (!string.IsNullOrEmpty(Title)) {
				sb.Length = 0;
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, Title);
				tag.Attributes["title"] = sb.ToString();
			}

			tag.IsClosed = true;

			m.OnPrepareImage(tag, m.RenderingTitledImage);

			tag.RenderOpening(b);
		}


		// Parse a link definition from a string (used by test cases)
		internal static LinkDefinition ParseLinkDefinition(string str, bool extraMode) {
			var p = new StringScanner(str);
			return ParseLinkDefinitionInternal(p, extraMode);
		}

		// Parse a link definition
		internal static LinkDefinition ParseLinkDefinition(StringScanner p, bool extraMode) {
			int savepos = p.Position;
			LinkDefinition l = ParseLinkDefinitionInternal(p, extraMode);
			if (l == null) {
				p.Position = savepos;
			}
			return l;
		}

		internal static LinkDefinition ParseLinkDefinitionInternal(StringScanner p, bool extraMode) {
			// Skip leading white space
			p.SkipWhitespace();

			// Must start with an opening square bracket
			if (!p.SkipChar('[')) {
				return null;
			}

			// Extract the id
			p.Mark();
			if (!p.Find(']')) {
				return null;
			}
			string id = p.Extract();
			if (id.Length == 0) {
				return null;
			}
			if (!p.SkipString("]:")) {
				return null;
			}

			// Parse the url and title
			LinkDefinition link = ParseLinkTarget(p, id, extraMode);

			// and trailing whitespace
			p.SkipLinespace();

			// Trailing crap, not a valid link reference...
			if (!p.Eol) {
				return null;
			}

			return link;
		}

		// Parse just the link target
		// For reference link definition, this is the bit after "[id]: thisbit"
		// For inline link, this is the bit in the parens: [link text](thisbit)
		internal static LinkDefinition ParseLinkTarget(StringScanner p, string id, bool extraMode) {
			// Skip whitespace
			p.SkipWhitespace();

			// End of string?
			if (p.Eol) {
				return null;
			}

			// Create the link definition
			var r = new LinkDefinition(id);

			// Is the url enclosed in angle brackets
			if (p.SkipChar('<')) {
				// Extract the url
				p.Mark();

				// Find end of the url
				while (p.Current != '>') {
					if (p.Eof) {
						return null;
					}
					p.SkipEscapableChar(extraMode);
				}

				string url = p.Extract();
				if (!p.SkipChar('>')) {
					return null;
				}

				// Unescape it
				r.Url = Utils.UnescapeString(url.Trim(), extraMode);

				// Skip whitespace
				p.SkipWhitespace();
			} else {
				// Find end of the url
				p.Mark();
				int parenDepth = 1;
				while (!p.Eol) {
					char ch = p.Current;
					if (char.IsWhiteSpace(ch)) {
						break;
					}
					if (id == null) {
						if (ch == '(') {
							parenDepth++;
						} else if (ch == ')') {
							parenDepth--;
							if (parenDepth == 0) {
								break;
							}
						}
					}

					p.SkipEscapableChar(extraMode);
				}

				r.Url = Utils.UnescapeString(p.Extract().Trim(), extraMode);
			}

			p.SkipLinespace();

			// End of inline target
			if (p.DoesMatch(')')) {
				return r;
			}

			bool bOnNewLine = p.Eol;
			int posLineEnd = p.Position;
			if (p.Eol) {
				p.SkipEol();
				p.SkipLinespace();
			}

			// Work out what the title is delimited with
			char delim;
			switch (p.Current) {
				case '\'':
				case '\"':
					delim = p.Current;
					break;

				case '(':
					delim = ')';
					break;

				default:
					if (bOnNewLine) {
						p.Position = posLineEnd;
						return r;
					}
					return null;
			}

			// Skip the opening title delimiter
			p.SkipForward(1);

			// Find the end of the title
			p.Mark();
			for(;;) {
				if (p.Eol) {
					return null;
				}

				if (p.Current == delim) {
					if (delim != ')') {
						int savepos = p.Position;

						// Check for embedded quotes in title

						// Skip the quote and any trailing whitespace
						p.SkipForward(1);
						p.SkipLinespace();

						// Next we expect either the end of the line for a link definition
						// or the close bracket for an inline link
						if ((id == null && p.Current != ')') ||
							(id != null && !p.Eol)) {
							continue;
						}

						p.Position = savepos;
					}

					// End of title
					break;
				}

				p.SkipEscapableChar(extraMode);
			}

			// Store the title
			r.Title = Utils.UnescapeString(p.Extract(), extraMode);

			// Skip closing quote
			p.SkipForward(1);

			// Done!
			return r;
		}
	}
}