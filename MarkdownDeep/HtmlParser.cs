namespace MarkdownDeep {

	/// <summary> static methods for parsing an HTML tag out of StringScanner. </summary>
	public static class HtmlParser {

		public static HtmlTag ParseHtml(this string str, ref int pos) {
			var sp = new StringScanner(str, pos);
			HtmlTag ret = ParseHtml(sp);
			if (ret == null) {
				return null;
			}
			pos = sp.Position;
			return ret;
		}

		public static HtmlTag ParseHtml(this StringScanner p) {
			// Save position
			int savepos = p.Position;

			// Parse it
			HtmlTag ret = ParseHelper(p);
			if (ret != null) {
				return ret;
			}

			// Rewind if failed
			p.Position = savepos;
			return null;
		}

		static HtmlTag ParseHelper(StringScanner p) {
			// Does it look like a tag?
			if (p.Current != '<') {
				return null;
			}

			// Skip '<'
			p.SkipForward(1);

			// Is it a comment?
			if (p.SkipString("!--")) {
				p.Mark();

				if (p.Find("-->")) {
					var t = new HtmlTag("!");
					t.AddAttribute("content", p.Extract());
					t.IsClosed = true;
					p.SkipForward(3);
					return t;
				}
			}
			bool isClosing = p.SkipChar('/');

			// Get the tag name
			string tagName = null;
			if (!p.SkipIdentifier(ref tagName)) {
				return null;
			}

			// Probably a tag, create the HtmlTag object now
			var tag = new HtmlTag(tagName) {IsClosing = isClosing};

			// If it's a closing tag, no attributes
			if (isClosing) {
				if (p.Current != '>') {
					return null;
				}

				p.SkipForward(1);
				return tag;
			}


			while (!p.Eof) {
				// Skip whitespace
				p.SkipWhitespace();

				// Check for closed tag eg: <hr />
				if (p.SkipString("/>")) {
					tag.IsClosed = true;
					return tag;
				}

				// End of tag?
				if (p.SkipChar('>')) {
					return tag;
				}

				// attribute name
				string attributeName = null;
				if (!p.SkipIdentifier(ref attributeName)) {
					return null;
				}

				// Skip whitespace
				p.SkipWhitespace();

				// Skip equal sign
				if (p.SkipChar('=')) {
					// Skip whitespace
					p.SkipWhitespace();

					// Optional quotes
					char sep;
					if (p.SkipChar(sep = '"') ||
						p.SkipChar(sep = '\'')) {
						p.Mark();
						if (!p.Find(sep)) {
							return null;
						}
						tag.AddAttribute(attributeName, p.Extract());
						p.SkipForward(1); //closing quote
					} else {
						p.Mark();
						while (!p.Eof
							&& !char.IsWhiteSpace(p.Current)
							&& p.Current != '>'
							&& p.Current != '/') {
							p.SkipForward(1);
						}
						if (!p.Eof) {
							tag.AddAttribute(attributeName, p.Extract());
						}
					}
				} else {
					tag.AddAttribute(attributeName, "");
				}
			}
			return null;
		}

	}
}