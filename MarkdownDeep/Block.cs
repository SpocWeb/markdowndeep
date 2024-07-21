using System.Collections.Generic;
using System.Text;

namespace MarkdownDeep {

	/// <summary> Recursive (Composite) block level structures of a Markdown document</summary>
	/// <remarks>
	/// Also used by the BlockProcessor to store information about a single line of input.
	/// </remarks>
	/// <example>paragraph, list, list item, blockquote, code block etc...  </example>
	internal class Block {

		internal BlockType _BlockType;
		internal string _Buf;
		internal List<Block> _Children;
		internal int _ContentLen;
		internal int _ContentStart;
		internal int _MarkupLen;
		internal int _MarkupStart;
		internal object _Data; // content depends on block type
		internal int _LineLen;
		internal int _LineStart;
		internal Block() {}

		internal Block(BlockType type) {
			_BlockType = type;
		}

		public Block CopyFrom(Block other) {
			_BlockType = other._BlockType;
			_Buf = other._Buf;
			_ContentStart = other._ContentStart;
			_ContentLen = other._ContentLen;
			_MarkupStart = other._MarkupStart;
			_MarkupLen = other._MarkupLen;
			_LineStart = other._LineStart;
			_LineLen = other._LineLen;
			return this;
		}

		public bool IsSectionHeader() => _BlockType >= BlockType.h1 && _BlockType <= BlockType.h3;

		public string Content {
			get {
				switch (_BlockType) {
					case BlockType.codeblock:
						var s = new StringBuilder();
						foreach (Block line in _Children) {
							s.Append(line.Content);
							s.Append('\n');
						}
						return s.ToString();
				}
				if (_Buf == null) {
					return null;
				}
				return _ContentStart == -1 ? _Buf : _Buf.Substring(_ContentStart, _ContentLen);
			}
		}

		public int LineStart => _LineStart == 0 ? _ContentStart : _LineStart;

		public int ContentEnd {
			get { return _ContentStart + _ContentLen; }
			set { _ContentLen = value - _ContentStart; }
		}

		public int MarkupEnd => _MarkupStart + _MarkupLen;

		// Count the leading spaces on a block
		// Used by list item evaluation to determine indent levels
		// irrespective of indent line type.
		public int LeadingSpaces {
			get {
				int count = 0;
				for (int i = _LineStart; i < _LineStart + _LineLen; i++) {
					if (_Buf[i] == ' ') {
						count++;
					} else {
						break;
					}
				}
				return count;
			}
		}

		internal string ResolveHeaderId(Markdown m) {
			// Already resolved?
			var s = _Data as string;
			if (s != null) {
				return s;
			}
			// Approach 1 - PHP Markdown Extra style header id
			int end = ContentEnd;
			string id = Utils.StripHtmlId(_Buf, _ContentStart, ref end);
			if (id != null) {
				ContentEnd = end;
			} else {
				// Approach 2 - pandoc style header id
				id = m.MakeUniqueHeaderId(_Buf, _ContentStart, _ContentLen);
			}

			_Data = id;
			return id;
		}

		#region rendering 

		internal void RenderChildren(Markdown m, StringBuilder b, bool includeMarkup) {
			foreach (Block block in _Children) {
				block.Render(m, b, includeMarkup);
			}
		}

		internal void RenderChildrenPlain(Markdown m, StringBuilder b) {
			foreach (Block block in _Children) {
				block.RenderPlain(m, b);
			}
		}

		internal void Render(Markdown m, StringBuilder b, bool includeMarkup) {
			switch (_BlockType) {
				case BlockType.Blank:
					return;

				case BlockType.p:
					m.SpanFormatter.FormatParagraph(b, _Buf, _ContentStart, _ContentLen);
					break;

				case BlockType.span:
					m.SpanFormatter.Format(b, _Buf, _ContentStart, _ContentLen);
					b.Append("\n");
					break;

				case BlockType.h1:
				case BlockType.h2:
				case BlockType.h3:
				case BlockType.h4:
				case BlockType.h5:
				case BlockType.h6:
					if (m.IsExtraMode && !m.IsSafeMode) {
						b.Append("<" + _BlockType);
						string id = ResolveHeaderId(m);
						if (!string.IsNullOrEmpty(id)) {
							b.Append(" id=\"");
							b.Append(id);
							b.Append("\">");
						} else {
							b.Append(">");
						}
					} else {
						b.Append("<" + _BlockType + ">");
					}
					m.SpanFormatter.Format(b, _Buf
						, includeMarkup ? _MarkupStart : _ContentStart
						, includeMarkup ? _MarkupLen : _ContentLen);
					b.Append("</" + _BlockType + ">\n");
					break;

				case BlockType.hr:
					b.Append("<hr />\n");
					return;

				case BlockType.user_break:
					return;

				case BlockType.ol_li:
				case BlockType.ul_li:
					b.Append("<li>");
					m.SpanFormatter.Format(b, _Buf, _ContentStart, _ContentLen);
					b.Append("</li>\n");
					break;

				case BlockType.dd:
					b.Append("<dd>");
					if (_Children != null) {
						b.Append("\n");
						RenderChildren(m, b, includeMarkup);
					} else {
						m.SpanFormatter.Format(b, _Buf, _ContentStart, _ContentLen);
					}
					b.Append("</dd>\n");
					break;

				case BlockType.dt: {
					if (_Children == null) {
						foreach (string l in Content.Split('\n')) {
							b.Append("<dt>");
							m.SpanFormatter.Format(b, l.Trim());
							b.Append("</dt>\n");
						}
					} else {
						b.Append("<dt>\n");
						RenderChildren(m, b, includeMarkup);
						b.Append("</dt>\n");
					}
					break;
				}

				case BlockType.dl:
					b.Append("<dl>\n");
					RenderChildren(m, b, includeMarkup);
					b.Append("</dl>\n");
					return;

				case BlockType.html:
					b.Append(_Buf, _ContentStart, _ContentLen);
					return;

				case BlockType.unsafe_html:
					m.HtmlEncode(b, _Buf, _ContentStart, _ContentLen);
					return;

				case BlockType.codeblock:
					if (m.FormatCodeBlock != null) {
						var sb = new StringBuilder();
						foreach (Block line in _Children) {
							HtmlEncodeAndConvertTabsToSpaces(m._StringScanner, sb, line._Buf, line._ContentStart, line._ContentLen);
							sb.Append("\n");
						}
						b.Append(m.FormatCodeBlock(m, sb.ToString()));
					} else {
						b.Append("<pre><code>");
						foreach (Block line in _Children) {
							HtmlEncodeAndConvertTabsToSpaces(m._StringScanner, b, line._Buf, line._ContentStart, line._ContentLen);
							b.Append("\n");
						}
						b.Append("</code></pre>\n\n");
					}
					return;

				case BlockType.quote:
					b.Append("<blockquote>\n");
					RenderChildren(m, b, includeMarkup);
					b.Append("</blockquote>\n");
					return;

				case BlockType.li:
					b.Append("<li>\n");
					RenderChildren(m, b, includeMarkup);
					b.Append("</li>\n");
					return;

				case BlockType.ol:
					b.Append("<ol>\n");
					RenderChildren(m, b, includeMarkup);
					b.Append("</ol>\n");
					return;

				case BlockType.ul:
					b.Append("<ul>\n");
					RenderChildren(m, b, includeMarkup);
					b.Append("</ul>\n");
					return;

				case BlockType.HtmlTag:
					var tag = (HtmlTag) _Data;

					// Prepare special tags
					string name = tag.Name.ToLowerInvariant();
					if (name == "a") {
						m.OnPrepareLink(tag);
					} else if (name == "img") {
						m.OnPrepareImage(tag, m.RenderingTitledImage);
					}

					tag.RenderOpening(b);
					b.Append("\n");
					RenderChildren(m, b, includeMarkup);
					tag.RenderClosing(b);
					b.Append("\n");
					return;

				case BlockType.Composite:
				case BlockType.footnote:
					RenderChildren(m, b, includeMarkup);
					return;

				case BlockType.table_spec:
					((TableSpec) _Data).Render(m, b);
					break;

				case BlockType.p_footnote:
					b.Append("<p>");
					if (_ContentLen > 0) {
						m.SpanFormatter.Format(b, _Buf, _ContentStart, _ContentLen);
						b.Append(NbSp);
					}
					b.Append((string) _Data);
					b.Append("</p>\n");
					break;

				default:
					b.Append("<" + _BlockType + ">");
					m.SpanFormatter.Format(b, _Buf, _ContentStart, _ContentLen);
					b.Append("</" + _BlockType + ">\n");
					break;
			}
		}

		public const string NbSp = "&160;";

		/// <summary> HtmlEncode a string, also converting tabs to spaces (used by CodeBlocks) </summary>
		internal static void HtmlEncodeAndConvertTabsToSpaces(StringScanner p, StringBuilder dest, string str, int start, int len) {
			p.Reset(str, start, len);
			int pos = 0;
			while (!p.Eof) {
				char ch = p.Current;
				switch (ch) {
					case '\t':
						dest.Append(' ');
						pos++;
						while ((pos % 4) != 0) {
							dest.Append(' ');
							pos++;
						}
						pos--; // Compensate for the pos++ below
						break;

					case '\r':
					case '\n':
						dest.Append('\n');
						pos = 0;
						p.SkipEol();
						continue;

					case '&': dest.Append("&amp;"); break;
					case '<': dest.Append("&lt;"); break;
					case '>': dest.Append("&gt;"); break;
					case '\"': dest.Append("&quot;"); break;
					default: dest.Append(ch); break;
				}
				p.SkipForward(1);
				pos++;
			}
		}

		internal void RenderPlain(Markdown m, StringBuilder b) {
			switch (_BlockType) {
				case BlockType.Blank: return;
				case BlockType.p:
				case BlockType.span:
					m.SpanFormatter.FormatPlain(b, _Buf, _ContentStart, _ContentLen);
					b.Append(" ");
					break;

				case BlockType.h1:
				case BlockType.h2:
				case BlockType.h3:
				case BlockType.h4:
				case BlockType.h5:
				case BlockType.h6:
					m.SpanFormatter.FormatPlain(b, _Buf, _ContentStart, _ContentLen);
					b.Append(" - ");
					break;


				case BlockType.ol_li:
				case BlockType.ul_li:
					b.Append("* ");
					m.SpanFormatter.FormatPlain(b, _Buf, _ContentStart, _ContentLen);
					b.Append(" ");
					break;

				case BlockType.dd:
					if (_Children != null) {
						b.Append("\n");
						RenderChildrenPlain(m, b);
					} else {
						m.SpanFormatter.FormatPlain(b, _Buf, _ContentStart, _ContentLen);
					}
					break;

				case BlockType.dt: {
					if (_Children == null) {
						foreach (string l in Content.Split('\n')) {
							string str = l.Trim();
							m.SpanFormatter.FormatPlain(b, str, 0, str.Length);
						}
					} else {
						RenderChildrenPlain(m, b);
					}
					break;
				}

				case BlockType.dl:
					RenderChildrenPlain(m, b);
					return;

				case BlockType.codeblock:
					foreach (Block line in _Children) {
						b.Append(line._Buf, line._ContentStart, line._ContentLen);
						b.Append(" ");
					}
					return;

				case BlockType.quote:
				case BlockType.li:
				case BlockType.ol:
				case BlockType.ul:
				case BlockType.HtmlTag:
					RenderChildrenPlain(m, b);
					return;
			}
		}

		public void RevertToPlain() {
			_BlockType = BlockType.p;
			_ContentStart = _LineStart;
			_ContentLen = _LineLen;
		}

		public override string ToString() {
			string c = Content;
			return _BlockType + " - " + (c ?? "<null>");
		}

		#endregion rendering

	}
}