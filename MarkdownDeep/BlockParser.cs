using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MarkdownDeep {

	/// <summary> Parses the input text from a StringScanner into a tree of blocks. </summary>
	public class BlockParser : StringScanner {

		/// <summary> Flag whether Markdown is allowed in HTML </summary>
		readonly bool _IsMarkdownInHtml;
		/// <summary> Flag to include the Markdown in the HTML </summary>
		readonly bool _IncludeMarkdownInHtml;
		readonly Markdown _Markdown;
		readonly BlockType _ParentType;
		public const string LineBreak = "<br/>";

		public BlockParser(Markdown m, bool allowMarkdownInHtml, bool includeMarkdownInHtml) 
			: this(m, allowMarkdownInHtml, includeMarkdownInHtml, BlockType.Blank) {
		}

		internal BlockParser(Markdown m, bool markdownInHtml, bool includeMarkdownInHtml, BlockType parentType) {
			_Markdown = m;
			_IsMarkdownInHtml = markdownInHtml;
			_ParentType = parentType;
			_IncludeMarkdownInHtml = includeMarkdownInHtml;
		}

		internal List<Block> Process(string str) => ScanLines(str);

		internal List<Block> ScanLines(string str) {
			// Reset string scanner
			Reset(str);
			return ScanLines();
		}

		internal List<Block> ScanLines(string str, int start, int len) {
			Reset(str, start, len);
			return ScanLines();
		}

		internal bool StartTable(TableSpec spec, List<Block> lines) {
			// Mustn't have more than 1 preceeding line
			if (lines.Count > 1) {
				return false;
			}

			// Rewind, parse the header row then fast forward back to current pos
			if (lines.Count == 1) {
				int savepos = Position;
				Position = lines[0]._LineStart;
				spec.Headers = spec.ParseRow(this);
				if (spec.Headers == null) {
					return false;
				}
				Position = savepos;
				lines.Clear();
			}

			// Parse all rows
			for(;;) {
				int savepos = Position;

				List<string> row = spec.ParseRow(this);
				if (row != null) {
					spec.Rows.Add(row);
					continue;
				}

				Position = savepos;
				break;
			}

			return true;
		}

		internal List<Block> ScanLines() {
			// The final set of blocks will be collected here
			var blocks = new List<Block>();

			// The current paragraph/list/codeblock etc will be accumulated here
			// before being collapsed into a block and store in above `blocks` list
			var lines = new List<Block>();

			// Add all blocks
			var prevBlockType = BlockType.unsafe_html;
			while (!Eof) {
				// Remember if the previous line was blank
				bool bPreviousBlank = prevBlockType == BlockType.Blank;

				// Get the next block
				Block b = EvaluateLine();
				prevBlockType = b._BlockType;

				// For dd blocks, we need to know if it was preceeded by a blank line
				// so store that fact as the block's data.
				if (b._BlockType == BlockType.dd) {
					b._Data = bPreviousBlank;
				}

				// Underlined SeText header?
				if (b._BlockType == BlockType.post_h1 || 
					b._BlockType == BlockType.post_h2) {
					if (lines.Count > 0) {
						// Remove the previous line and collapse the current paragraph
						Block prevline = lines.Pop();
						CollapseLines(blocks, lines);

						// If previous line was blank, 
						if (prevline._BlockType != BlockType.Blank) {
							// Convert the previous line to a heading and add to block list
							prevline.RevertToPlain();
							prevline._BlockType = b._BlockType == BlockType.post_h1 
								? BlockType.h1 : BlockType.h2;
							if (_IncludeMarkdownInHtml) {
								prevline._Buf = prevline._Buf.Insert(b._MarkupStart, LineBreak);
								b._MarkupLen += LineBreak.Length;
								prevline._MarkupLen = b.MarkupEnd - prevline._MarkupStart;
							}
							blocks.Add(prevline);
							continue;
						}
					}

					// Couldn't apply SeText header to a previous line
					if (b._BlockType == BlockType.post_h1) {
						// `===` gets converted to normal paragraph
						b.RevertToPlain();
						lines.Add(b);
					} else {
						// `---` gets converted to hr
						if (b._ContentLen >= 3) {
							b._BlockType = BlockType.hr;
							blocks.Add(b);
						} else {
							b.RevertToPlain();
							lines.Add(b);
						}
					}
					continue;
				}

				// Work out the current paragraph type
				BlockType currentBlockType = lines.Count > 0 ? lines[0]._BlockType : BlockType.Blank;

				// Starting a table?
				if (b._BlockType == BlockType.table_spec) {
					// Get the table spec, save position
					var spec = (TableSpec) b._Data;
					int savepos = Position;
					if (!StartTable(spec, lines)) {
						// Not a table; revert the tablespec row to plain,
						// fast forward back to where we were up to 
						// and continue on as if nothing happened
						Position = savepos;
						b.RevertToPlain();
					} else {
						blocks.Add(b);
						continue;
					}
				}

				// Process this line
				switch (b._BlockType) {
					case BlockType.Blank: ProcessBlankLineBlock(currentBlockType, b, blocks, lines); break;
					case BlockType.p: ProcessParagraph(currentBlockType, lines, b, blocks); break;
					case BlockType.indent: ProcessIndentedLine(currentBlockType, lines, b, blocks); break;
					case BlockType.quote: ProcessBlockQuote(currentBlockType, blocks, lines, b); break;
					case BlockType.ol_li:
					case BlockType.ul_li: ProcessListItem(currentBlockType, lines, b, blocks); break;
					case BlockType.dd:
					case BlockType.footnote: ProcessFootNoteDefinition(currentBlockType, blocks, lines, b); break;
					default:
						CollapseLines(blocks, lines);
						blocks.Add(b);
						break;
				}
			}
			CollapseLines(blocks, lines);
			if (_Markdown.IsExtraMode) {
				BuildDefinitionLists(blocks);
			}
			return blocks;
		}

		#region Block Type Processors

		void ProcessFootNoteDefinition(BlockType currentBlockType, List<Block> blocks, List<Block> lines, Block b) {
			switch (currentBlockType) {
				case BlockType.Blank:
				case BlockType.p:
				case BlockType.dd:
				case BlockType.footnote:
					CollapseLines(blocks, lines);
					lines.Add(b);
					break;

				default:
					b.RevertToPlain();
					lines.Add(b);
					break;
			}
		}

		void ProcessListItem(BlockType currentBlockType, List<Block> lines, Block b, List<Block> blocks) {
			switch (currentBlockType) {
				case BlockType.Blank:
					lines.Add(b);
					break;

				case BlockType.p:
				case BlockType.quote:
					Block prevline = lines.Last();
					if (prevline._BlockType == BlockType.Blank || _ParentType == BlockType.ol_li || _ParentType == BlockType.ul_li ||
						_ParentType == BlockType.dd) {
						// List starting after blank line after paragraph or quote
						CollapseLines(blocks, lines);
						lines.Add(b);
					} else {
						// List's can't start in middle of a paragraph
						b.RevertToPlain();
						lines.Add(b);
					}
					break;

				case BlockType.ol_li:
				case BlockType.ul_li:
					if (b._BlockType != BlockType.ol_li && b._BlockType != BlockType.ul_li) {
						CollapseLines(blocks, lines);
					}
					lines.Add(b);
					break;

				case BlockType.dd:
				case BlockType.footnote:
					if (b._BlockType != currentBlockType) {
						CollapseLines(blocks, lines);
					}
					lines.Add(b);
					break;

				case BlockType.indent:
					// List after code block
					CollapseLines(blocks, lines);
					lines.Add(b);
					break;
			}
		}

		void ProcessBlockQuote(BlockType currentBlockType, List<Block> blocks, List<Block> lines, Block b) {
			if (currentBlockType != BlockType.quote) {
				CollapseLines(blocks, lines);
			}
			lines.Add(b);
		}

		void ProcessIndentedLine(BlockType currentBlockType, List<Block> lines, Block b, List<Block> blocks) {
			switch (currentBlockType) {
				case BlockType.Blank:
					// Start a code block
					lines.Add(b);
					break;

				case BlockType.p:
				case BlockType.quote:
					Block prevline = lines.Last();
					if (prevline._BlockType == BlockType.Blank) {
						// Start a code block after a paragraph
						CollapseLines(blocks, lines);
						lines.Add(b);
					} else {
						// indented line in paragraph, just continue it
						b.RevertToPlain();
						lines.Add(b);
					}
					break;


				case BlockType.ol_li:
				case BlockType.ul_li:
				case BlockType.dd:
				case BlockType.footnote:
				case BlockType.indent:
					lines.Add(b);
					break;

				default:
					Debug.Assert(false);
					break;
			}
		}

		void ProcessParagraph(BlockType currentBlockType, List<Block> lines, Block b, List<Block> blocks) {
			switch (currentBlockType) {
				case BlockType.Blank:
				case BlockType.p:
					lines.Add(b);
					break;

				case BlockType.quote:
				case BlockType.ol_li:
				case BlockType.ul_li:
				case BlockType.dd:
				case BlockType.footnote:
					Block prevline = lines.Last();
					if (prevline._BlockType == BlockType.Blank) {
						CollapseLines(blocks, lines);
						lines.Add(b);
					} else {
						lines.Add(b);
					}
					break;

				case BlockType.indent:
					CollapseLines(blocks, lines);
					lines.Add(b);
					break;

				default:
					Debug.Assert(false);
					break;
			}
		}

		void ProcessBlankLineBlock(BlockType currentBlockType, Block b, List<Block> blocks, List<Block> lines) {
			switch (currentBlockType) {
				case BlockType.Blank:
					FreeBlock(b);
					break;

				case BlockType.p:
					CollapseLines(blocks, lines);
					FreeBlock(b);
					break;

				case BlockType.quote:
				case BlockType.ol_li:
				case BlockType.ul_li:
				case BlockType.dd:
				case BlockType.footnote:
				case BlockType.indent:
					lines.Add(b);
					break;

				default:
					Debug.Assert(false);
					break;
			}
		}

		#endregion Block Type Processors

		internal Block CreateBlock() => _Markdown.CreateBlock();

		internal void FreeBlock(Block b) => _Markdown.FreeBlock(b);

		internal void FreeBlocks(List<Block> blocks) {
			foreach (Block b in blocks)
				FreeBlock(b);
			blocks.Clear();
		}

		internal string RenderLines(List<Block> lines) {
			StringBuilder b = _Markdown.GetStringBuilder();
			foreach (Block l in lines) {
				b.Append(l._Buf, l._ContentStart, l._ContentLen);
				b.Append('\n');
			}
			return b.ToString();
		}

		internal void CollapseLines(List<Block> blocks, List<Block> lines) {
			// Remove trailing blank lines
			while (lines.Count > 0 && lines.Last()._BlockType == BlockType.Blank) {
				FreeBlock(lines.Pop());
			}
			// Quit if empty
			if (lines.Count == 0) {
				return;
			}
			// What sort of block?
			switch (lines[0]._BlockType) {
				case BlockType.p: {
					// Collapse all lines into a single paragraph
					Block para = CreateBlock();
					para._BlockType = BlockType.p;
					para._Buf = lines[0]._Buf;
					para._ContentStart = lines[0]._ContentStart;
					para.ContentEnd = lines.Last().ContentEnd;
					blocks.Add(para);
					FreeBlocks(lines);
					break;
				}

				case BlockType.quote: {
					// Create a new quote block
					var quote = new Block(BlockType.quote) {
						_Children = new BlockParser(_Markdown, _IsMarkdownInHtml, _IncludeMarkdownInHtml, BlockType.quote).Process(RenderLines(lines))
					};
					FreeBlocks(lines);
					blocks.Add(quote);
					break;
				}

				case BlockType.ol_li:
				case BlockType.ul_li:
					blocks.Add(BuildList(lines));
					break;

				case BlockType.dd:
					if (blocks.Count > 0) {
						Block prev = blocks[blocks.Count - 1];
						switch (prev._BlockType) {
							case BlockType.p:
								prev._BlockType = BlockType.dt;
								break;

							case BlockType.dd:
								break;

							default:
								Block wrapper = CreateBlock();
								wrapper._BlockType = BlockType.dt;
								wrapper._Children = new List<Block> {prev};
								blocks.Pop();
								blocks.Add(wrapper);
								break;
						}
					}
					blocks.Add(BuildDefinition(lines));
					break;

				case BlockType.footnote:
					_Markdown.AddFootnote(BuildFootnote(lines));
					break;

				case BlockType.indent: {
					var codeblock = new Block(BlockType.codeblock) {_Children = new List<Block>()};
					/*
					if (m_markdown.FormatCodeBlockAttributes != null)
					{
						// Does the line first line look like a syntax specifier
						var firstline = lines[0].Content;
						if (firstline.StartsWith("{{") && firstline.EndsWith("}}"))
						{
							codeblock.data = firstline.Substring(2, firstline.Length - 4);
							lines.RemoveAt(0);
						}
					}
					 */
					codeblock._Children.AddRange(lines);
					blocks.Add(codeblock);
					lines.Clear();
					break;
				}
			}
		}


		Block EvaluateLine() {
			// Create a block
			Block b = CreateBlock();

			// Store line start
			b._LineStart = b._MarkupStart = Position;
			b._Buf = Input;

			// Scan the line
			b._ContentStart = Position;
			b._ContentLen = b._MarkupLen = - 1;
			b._BlockType = EvaluateLine(b);

			// If end of line not returned, do it automatically
			if (b._ContentLen < 0) {
				// Move to end of line
				SkipToEol();
				b._ContentLen = Position - b._ContentStart;
			}

			// Setup line length
			b._LineLen = b._MarkupLen = Position - b._LineStart;

			// Next line
			SkipEol();

			// Create block
			return b;
		}

		BlockType EvaluateLine(Block b) {
			// Empty line?
			if (Eol) {
				return BlockType.Blank;
			}

			// Save start of line position
			int lineStart = Position;

			// ## Heading ##		
			char ch = Current;
			if (ch == '#') {
				// Work out heading level
				int level = 1;
				SkipForward(1);
				while (Current == '#') {
					level++;
					SkipForward(1);
				}

				// Limit of 6
				if (level > 6) {
					level = 6;
				}

				// Skip any whitespace
				SkipLinespace();

				// Save start position
				b._ContentStart = Position;

				// Jump to end
				SkipToEol();

				// In extra mode, check for a trailing HTML ID
				if (_Markdown.IsExtraMode && !_Markdown.IsSafeMode) {
					int end = Position;
					string strId = Utils.StripHtmlId(Input, b._ContentStart, ref end);
					if (strId != null) {
						b._Data = strId;
						Position = end;
					}
				}

				// Rewind over trailing hashes
				while (Position > b._ContentStart && CharAtOffset(-1) == '#') {
					SkipForward(-1);
				}

				// Rewind over trailing spaces
				while (Position > b._ContentStart && char.IsWhiteSpace(CharAtOffset(-1))) {
					SkipForward(-1);
				}

				// Create the heading block
				b.ContentEnd = Position;

				SkipToEol();
				return BlockType.h1 + (level - 1);
			}

			// Check for entire line as - or = for setext h1 and h2
			if (ch == '-' || ch == '=') {
				// Skip all matching characters
				char chType = ch;
				while (Current == chType) {
					SkipForward(1);
				}

				// Trailing whitespace allowed
				SkipLinespace();

				// If not at eol, must have found something other than setext header
				if (Eol) {
					return chType == '=' ? BlockType.post_h1 : BlockType.post_h2;
				}

				Position = lineStart;
			}

			// MarkdownExtra Table row indicator?
			if (_Markdown.IsExtraMode) {
				TableSpec spec = TableSpec.Parse(this);
				if (spec != null) {
					b._Data = spec;
					return BlockType.table_spec;
				}

				Position = lineStart;
			}

			// Fenced code blocks?
			if (_Markdown.IsExtraMode && (ch == '~' || ch == '`')) {
				if (ProcessFencedCodeBlock(b)) {
					return b._BlockType;
				}

				// Rewind
				Position = lineStart;
			}

			// Scan the leading whitespace, remembering how many spaces and where the first tab is
			int tabPos = -1;
			int leadingSpaces = 0;
			while (!Eol) {
				if (Current == ' ') {
					if (tabPos < 0) {
						leadingSpaces++;
					}
				} else if (Current == '\t') {
					if (tabPos < 0) {
						tabPos = Position;
					}
				} else {
					// Something else, get out
					break;
				}
				SkipForward(1);
			}

			// Blank line?
			if (Eol) {
				b.ContentEnd = b._ContentStart;
				return BlockType.Blank;
			}

			// 4 leading spaces?
			if (leadingSpaces >= 4) {
				b._ContentStart = lineStart + 4;
				return BlockType.indent;
			}

			// Tab in the first 4 characters?
			if (tabPos >= 0 && tabPos - lineStart < 4) {
				b._ContentStart = tabPos + 1;
				return BlockType.indent;
			}

			// Treat start of line as after leading whitespace
			b._ContentStart = Position;

			// Get the next character
			ch = Current;

			// Html block?
			if (ch == '<') {
				// Scan html block
				if (ScanHtml(b)) {
					return b._BlockType;
				}

				// Rewind
				Position = b._ContentStart;
			}

			// Block quotes start with '>' and have one space or one tab following
			if (ch == '>') {
				// Block quote followed by space
				if (IsLineSpace(CharAtOffset(1))) {
					// Skip it and create quote block
					SkipForward(2);
					b._ContentStart = Position;
					return BlockType.quote;
				}

				SkipForward(1);
				b._ContentStart = Position;
				return BlockType.quote;
			}

			// Horizontal rule - a line consisting of 3 or more '-', '_' or '*' with optional spaces and nothing else
			if (ch == '-' || ch == '_' || ch == '*') {
				int count = 0;
				while (!Eol) {
					if (Current == ch) {
						count++;
						SkipForward(1);
						continue;
					}

					if (IsLineSpace(Current)) {
						SkipForward(1);
						continue;
					}

					break;
				}

				if (Eol && count >= 3) {
					if (_Markdown.AllowUserBreaks) {
						return BlockType.user_break;
					}
					return BlockType.hr;
				}

				// Rewind
				Position = b._ContentStart;
			}

			// Abbreviation definition?
			if (_Markdown.IsExtraMode && ch == '*' && CharAtOffset(1) == '[') {
				SkipForward(2);
				SkipLinespace();

				Mark();
				while (!Eol && Current != ']') {
					SkipForward(1);
				}

				string abbr = Extract().Trim();
				if (Current == ']' && CharAtOffset(1) == ':' && !string.IsNullOrEmpty(abbr)) {
					SkipForward(2);
					SkipLinespace();

					Mark();

					SkipToEol();

					string title = Extract();

					_Markdown.AddAbbreviation(abbr, title);

					return BlockType.Blank;
				}

				Position = b._ContentStart;
			}

			// Unordered list
			if ((ch == '*' || ch == '+' || ch == '-') && IsLineSpace(CharAtOffset(1))) {
				// Skip it
				SkipForward(1);
				SkipLinespace();
				b._ContentStart = Position;
				return BlockType.ul_li;
			}

			// Definition
			if (ch == ':' && _Markdown.IsExtraMode && IsLineSpace(CharAtOffset(1))) {
				SkipForward(1);
				SkipLinespace();
				b._ContentStart = Position;
				return BlockType.dd;
			}

			// Ordered list
			if (char.IsDigit(ch)) {
				// Ordered list?  A line starting with one or more digits, followed by a '.' and a space or tab

				// Skip all digits
				SkipForward(1);
				while (char.IsDigit(Current))
					SkipForward(1);

				if (SkipChar('.') && SkipLinespace()) {
					b._ContentStart = Position;
					return BlockType.ol_li;
				}

				Position = b._ContentStart;
			}

			// Reference link definition?
			if (ch == '[') {
				// Footnote definition?
				if (_Markdown.IsExtraMode && CharAtOffset(1) == '^') {
					int savepos = Position;

					SkipForward(2);

					if (SkipFootnoteId(out var id) && SkipChar(']') && SkipChar(':')) {
						SkipLinespace();
						b._ContentStart = Position;
						b._Data = id;
						return BlockType.footnote;
					}

					Position = savepos;
				}

				// Parse a link definition
				LinkDefinition l = LinkDefinition.ParseLinkDefinition(this, _Markdown.IsExtraMode);
				if (l != null) {
					_Markdown.AddLinkDefinition(l);
					return BlockType.Blank;
				}
			}

			// Nothing special
			return BlockType.p;
		}

		internal MarkdownInHtmlMode GetMarkdownMode(HtmlTag tag) {
			// Get the markdown attribute
			if (!_Markdown.IsExtraMode || !tag.Attributes.TryGetValue("markdown", out var strMarkdownMode)) {
				if (_IsMarkdownInHtml) {
					return MarkdownInHtmlMode.Deep;
				}
				return MarkdownInHtmlMode.Na;
			}

			// Remove it
			tag.Attributes.Remove("markdown");

			// Parse mode
			if (strMarkdownMode == "1") {
				return (tag.Flags & HtmlTagFlags.ContentAsSpan) != 0 ? MarkdownInHtmlMode.Span : MarkdownInHtmlMode.Block;
			}

			if (strMarkdownMode == "block") {
				return MarkdownInHtmlMode.Block;
			}

			if (strMarkdownMode == "deep") {
				return MarkdownInHtmlMode.Deep;
			}

			if (strMarkdownMode == "span") {
				return MarkdownInHtmlMode.Span;
			}

			return MarkdownInHtmlMode.Off;
		}

		internal bool ProcessMarkdownEnabledHtml(Block b, HtmlTag openingTag, MarkdownInHtmlMode mode) {
			// Current position is just after the opening tag

			// Scan until we find matching closing tag
			int innerPos = Position;
			int depth = 1;
			bool bHasUnsafeContent = false;
			while (!Eof) {
				// Find next angle bracket
				if (!Find('<')) {
					break;
				}

				// Is it a html tag?
				int tagpos = Position;
				HtmlTag tag = this.ParseHtml();
				if (tag == null) {
					// Nope, skip it 
					SkipForward(1);
					continue;
				}

				// In markdown off mode, we need to check for unsafe tags
				if (_Markdown.IsSafeMode && mode == MarkdownInHtmlMode.Off && !bHasUnsafeContent) {
					if (!tag.IsSafe()) {
						bHasUnsafeContent = true;
					}
				}

				// Ignore self closing tags
				if (tag.IsClosed) {
					continue;
				}

				// Same tag?
				if (tag.Name == openingTag.Name) {
					if (tag.IsClosing) {
						depth--;
						if (depth == 0) {
							// End of tag?
							SkipLinespace();
							SkipEol();

							b._BlockType = BlockType.HtmlTag;
							b._Data = openingTag;
							b.ContentEnd = Position;

							switch (mode) {
								case MarkdownInHtmlMode.Span: {
									Block span = CreateBlock();
									span._Buf = Input;
									span._BlockType = BlockType.span;
									span._ContentStart = innerPos;
									span._ContentLen = tagpos - innerPos;

									b._Children = new List<Block> {span};
									break;
								}

								case MarkdownInHtmlMode.Block:
								case MarkdownInHtmlMode.Deep: {
									// Scan the internal content
									var bp = new BlockParser(_Markdown, mode == MarkdownInHtmlMode.Deep, _IncludeMarkdownInHtml);
									b._Children = bp.ScanLines(Input, innerPos, tagpos - innerPos);
									break;
								}

								case MarkdownInHtmlMode.Off: {
									if (bHasUnsafeContent) {
										b._BlockType = BlockType.unsafe_html;
										b.ContentEnd = Position;
									} else {
										Block span = CreateBlock();
										span._Buf = Input;
										span._BlockType = BlockType.html;
										span._ContentStart = innerPos;
										span._ContentLen = tagpos - innerPos;

										b._Children = new List<Block> {span};
									}
									break;
								}
							}


							return true;
						}
					} else {
						depth++;
					}
				}
			}

			// Missing closing tag(s).  
			return false;
		}

		// Scan from the current position to the end of the html section
		internal bool ScanHtml(Block b) {
			// Remember start of html
			int posStartPiece = Position;

			// Parse a HTML tag
			HtmlTag openingTag = this.ParseHtml();
			if (openingTag == null) {
				return false;
			}

			// Closing tag?
			if (openingTag.IsClosing) {
				return false;
			}

			// Safe mode?
			bool hasUnsafeContent = _Markdown.IsSafeMode && !openingTag.IsSafe();

			HtmlTagFlags flags = openingTag.Flags;

			// Is it a block level tag?
			if ((flags & HtmlTagFlags.Block) == 0) {
				return false;
			}

			// Closed tag, hr or comment?
			if ((flags & HtmlTagFlags.NoClosing) != 0 || openingTag.IsClosed) {
				SkipLinespace();
				SkipEol();

				b.ContentEnd = Position;
				b._BlockType = hasUnsafeContent ? BlockType.unsafe_html : BlockType.html;
				return true;
			}

			// Can it also be an inline tag?
			if ((flags & HtmlTagFlags.Inline) != 0) {
				// Yes, opening tag must be on a line by itself
				SkipLinespace();
				if (!Eol) {
					return false;
				}
			}

			// Head block extraction?
			bool isHeadBlock = _Markdown.DoExtractHeadBlocks && 
				"head".Equals(openingTag.Name, System.StringComparison.OrdinalIgnoreCase);
			int headStart = Position;

			// Work out the markdown mode for this element
			if (!isHeadBlock && _Markdown.IsExtraMode) {
				MarkdownInHtmlMode markdownMode = GetMarkdownMode(openingTag);
				if (markdownMode != MarkdownInHtmlMode.Na) {
					return ProcessMarkdownEnabledHtml(b, openingTag, markdownMode);
				}
			}

			List<Block> childBlocks = null;

			// Now capture everything up to the closing tag and put it all in a single HTML block
			int depth = 1;

			while (!Eof) {
				// Find next angle bracket
				if (!Find('<')) {
					break;
				}

				// Save position of current tag
				int posStartCurrentTag = Position;

				// Is it a html tag?
				HtmlTag tag = this.ParseHtml();
				if (tag == null) {
					// Nope, skip it 
					SkipForward(1);
					continue;
				}

				// Safe mode checks
				if (_Markdown.IsSafeMode && !tag.IsSafe()) {
					hasUnsafeContent = true;
				}

				// Ignore self closing tags
				if (tag.IsClosed) {
					continue;
				}

				// Markdown enabled content?
				if (!isHeadBlock && !tag.IsClosing && _Markdown.IsExtraMode && !hasUnsafeContent) {
					MarkdownInHtmlMode markdownMode = GetMarkdownMode(tag);
					if (markdownMode != MarkdownInHtmlMode.Na) {
						Block markdownBlock = CreateBlock();
						if (ProcessMarkdownEnabledHtml(markdownBlock, tag, markdownMode)) {
							if (childBlocks == null) {
								childBlocks = new List<Block>();
							}

							// Create a block for everything before the markdown tag
							if (posStartCurrentTag > posStartPiece) {
								Block htmlBlock = CreateBlock();
								htmlBlock._Buf = Input;
								htmlBlock._BlockType = BlockType.html;
								htmlBlock._ContentStart = posStartPiece;
								htmlBlock._ContentLen = posStartCurrentTag - posStartPiece;

								childBlocks.Add(htmlBlock);
							}

							// Add the markdown enabled child block
							childBlocks.Add(markdownBlock);

							// Remember start of the next piece
							posStartPiece = Position;

							continue;
						}
						FreeBlock(markdownBlock);
					}
				}

				// Same tag?
				if (tag.Name == openingTag.Name) {
					if (tag.IsClosing) {
						depth--;
						if (depth == 0) {
							// End of tag?
							SkipLinespace();
							SkipEol();

							// If anything unsafe detected, just encode the whole block
							if (hasUnsafeContent) {
								b._BlockType = BlockType.unsafe_html;
								b.ContentEnd = Position;
								return true;
							}

							// Did we create any child blocks
							if (childBlocks != null) {
								// Create a block for the remainder
								if (Position > posStartPiece) {
									Block htmlBlock = CreateBlock();
									htmlBlock._Buf = Input;
									htmlBlock._BlockType = BlockType.html;
									htmlBlock._ContentStart = posStartPiece;
									htmlBlock._ContentLen = Position - posStartPiece;

									childBlocks.Add(htmlBlock);
								}

								// Return a composite block
								b._BlockType = BlockType.Composite;
								b.ContentEnd = Position;
								b._Children = childBlocks;
								return true;
							}

							// Extract the head block content
							if (isHeadBlock) {
								string content = Substring(headStart, posStartCurrentTag - headStart);
								_Markdown.HeadBlockContent = (_Markdown.HeadBlockContent ?? "") + content.Trim() + "\n";
								b._BlockType = BlockType.html;
								b._ContentStart = Position;
								b.ContentEnd = Position;
								b._LineStart = b._MarkupStart = Position;
								return true;
							}

							// Straight html block
							b._BlockType = BlockType.html;
							b.ContentEnd = Position;
							return true;
						}
					} else {
						depth++;
					}
				}
			}

			// Rewind to just after the tag
			return false;
		}

		/*
		 * Spacing
		 * 
		 * 1-3 spaces - Promote to indented if more spaces than original item
		 * 
		 */

		/* 
		 * BuildList - build a single <ol> or <ul> list
		 */

		Block BuildList(List<Block> lines) {
			// What sort of list are we dealing with
			BlockType listType = lines[0]._BlockType;
			Debug.Assert(listType == BlockType.ul_li || listType == BlockType.ol_li);

			// Preprocess
			// 1. Collapse all plain lines (ie: handle hardwrapped lines)
			// 2. Promote any unindented lines that have more leading space 
			//    than the original list item to indented, including leading 
			//    special chars
			int leadingSpace = lines[0].LeadingSpaces;
			for (int i = 1; i < lines.Count; i++) {
				// Join plain paragraphs
				if ((lines[i]._BlockType == BlockType.p) &&
					(lines[i - 1]._BlockType == BlockType.p || lines[i - 1]._BlockType == BlockType.ul_li ||
					 lines[i - 1]._BlockType == BlockType.ol_li)) {
					lines[i - 1].ContentEnd = lines[i].ContentEnd;
					FreeBlock(lines[i]);
					lines.RemoveAt(i);
					i--;
					continue;
				}

				if (lines[i]._BlockType != BlockType.indent && lines[i]._BlockType != BlockType.Blank) {
					int thisLeadingSpace = lines[i].LeadingSpaces;
					if (thisLeadingSpace > leadingSpace) {
						// Change line to indented, including original leading chars 
						// (eg: '* ', '>', '1.' etc...)
						lines[i]._BlockType = BlockType.indent;
						int saveend = lines[i].ContentEnd;
						lines[i]._ContentStart = lines[i]._LineStart + thisLeadingSpace;
						lines[i].ContentEnd = saveend;
					}
				}
			}


			// Create the wrapping list item
			var list = new Block(listType == BlockType.ul_li ? BlockType.ul : BlockType.ol) {_Children = new List<Block>()};

			// Process all lines in the range		
			for (int i = 0; i < lines.Count; i++) {
				Debug.Assert(
					lines[i]._BlockType == BlockType.ul_li || 
					lines[i]._BlockType == BlockType.ol_li);

				// Find start of item, including leading blanks
				int startOfLi = i;
				while (startOfLi > 0 && lines[startOfLi - 1]._BlockType == BlockType.Blank)
					startOfLi--;

				// Find end of the item, including trailing blanks
				int endOfLi = i;
				while (endOfLi < lines.Count - 1 
					&& lines[endOfLi + 1]._BlockType != BlockType.ul_li 
					&& lines[endOfLi + 1]._BlockType != BlockType.ol_li)
					endOfLi++;

				// Is this a simple or complex list item?
				if (startOfLi == endOfLi) { // It's a simple, single line item item
					Debug.Assert(startOfLi == i);
					list._Children.Add(CreateBlock().CopyFrom(lines[i]));
				} else {
					// Build a new string containing all child items
					bool bAnyBlanks = false;
					StringBuilder sb = _Markdown.GetStringBuilder();
					for (int j = startOfLi; j <= endOfLi; j++) {
						Block l = lines[j];
						sb.Append(l._Buf, l._ContentStart, l._ContentLen);
						sb.Append('\n');
						if (lines[j]._BlockType == BlockType.Blank) {
							bAnyBlanks = true;
						}
					}
					// Create the item and process child blocks
					var item = new Block(BlockType.li) {
						_Children = new BlockParser(_Markdown, _IsMarkdownInHtml, _IncludeMarkdownInHtml, listType).Process(sb.ToString())
					};
					// If no blank lines, change all contained paragraphs to plain text
					if (!bAnyBlanks) {
						foreach (Block child in item._Children) {
							if (child._BlockType == BlockType.p) {
								child._BlockType = BlockType.span;
							}
						}
					}
					// Add the complex item
					list._Children.Add(item);
				}

				// Continue processing from end of li
				i = endOfLi;
			}

			FreeBlocks(lines);
			lines.Clear();

			// Continue processing after this item
			return list;
		}

		/* 
		 * BuildDefinition - build a single <dd> item
		 */

		Block BuildDefinition(List<Block> lines) {
			// Collapse all plain lines (ie: handle hardwrapped lines)
			for (int i = 1; i < lines.Count; i++) {
				// Join plain paragraphs
				if ((lines[i]._BlockType == BlockType.p) &&
					(lines[i - 1]._BlockType == BlockType.p || lines[i - 1]._BlockType == BlockType.dd)) {
					lines[i - 1].ContentEnd = lines[i].ContentEnd;
					FreeBlock(lines[i]);
					lines.RemoveAt(i);
					i--;
				}
			}

			// Single line definition
			var bPreceededByBlank = (bool) lines[0]._Data;
			if (lines.Count == 1 && !bPreceededByBlank) {
				Block ret = lines[0];
				lines.Clear();
				return ret;
			}

			// Build a new string containing all child items
			StringBuilder sb = _Markdown.GetStringBuilder();
			for (int i = 0; i < lines.Count; i++) {
				Block l = lines[i];
				sb.Append(l._Buf, l._ContentStart, l._ContentLen);
				sb.Append('\n');
			}

			// Create the item and process child blocks
			Block item = CreateBlock();
			item._BlockType = BlockType.dd;
			item._Children = new BlockParser(_Markdown, _IsMarkdownInHtml, _IncludeMarkdownInHtml, BlockType.dd).Process(sb.ToString());

			FreeBlocks(lines);
			lines.Clear();

			// Continue processing after this item
			return item;
		}

		void BuildDefinitionLists(IList<Block> blocks) {
			Block currentList = null;
			for (int i = 0; i < blocks.Count; i++) {
				switch (blocks[i]._BlockType) {
					case BlockType.dt:
					case BlockType.dd:
						if (currentList == null) {
							currentList = CreateBlock();
							currentList._BlockType = BlockType.dl;
							currentList._Children = new List<Block>();
							blocks.Insert(i, currentList);
							i++;
						}

						currentList._Children.Add(blocks[i]);
						blocks.RemoveAt(i);
						i--;
						break;

					default:
						currentList = null;
						break;
				}
			}
		}

		Block BuildFootnote(List<Block> lines) {
			// Collapse all plain lines (ie: handle hardwrapped lines)
			for (int i = 1; i < lines.Count; i++) {
				// Join plain paragraphs
				if ((lines[i]._BlockType == BlockType.p) &&
					(lines[i - 1]._BlockType == BlockType.p || lines[i - 1]._BlockType == BlockType.footnote)) {
					lines[i - 1].ContentEnd = lines[i].ContentEnd;
					FreeBlock(lines[i]);
					lines.RemoveAt(i);
					i--;
				}
			}

			// Build a new string containing all child items
			StringBuilder sb = _Markdown.GetStringBuilder();
			for (int i = 0; i < lines.Count; i++) {
				Block l = lines[i];
				sb.Append(l._Buf, l._ContentStart, l._ContentLen);
				sb.Append('\n');
			}

			// Create the item and process child blocks
			Block item = CreateBlock();
			item._BlockType = BlockType.footnote;
			item._Data = lines[0]._Data;
			item._Children = new BlockParser(_Markdown, _IsMarkdownInHtml, _IncludeMarkdownInHtml, BlockType.footnote).Process(sb.ToString());

			FreeBlocks(lines);
			lines.Clear();

			// Continue processing after this item
			return item;
		}

		bool ProcessFencedCodeBlock(Block b) {
			char delim = Current;

			// Extract the fence
			Mark();
			while (Current == delim)
				SkipForward(1);
			string strFence = Extract();

			// Must be at least 3 long
			if (strFence.Length < 3) {
				return false;
			}

			// Rest of line must be blank
			SkipLinespace();
			if (!Eol) {
				return false;
			}

			// Skip the eol and remember start of code
			SkipEol();
			int startCode = Position;

			// Find the end fence
			if (!Find(strFence)) {
				return false;
			}

			// Character before must be a eol char
			if (!IsLineEnd(CharAtOffset(-1))) {
				return false;
			}

			int endCode = Position;

			// Skip the fence
			SkipForward(strFence.Length);

			// Whitespace allowed at end
			SkipLinespace();
			if (!Eol) {
				return false;
			}

			// Create the code block
			b._BlockType = BlockType.codeblock;
			b._Children = new List<Block>();

			// Remove the trailing line end
			if (Input[endCode - 1] == '\r' && Input[endCode - 2] == '\n') {
				endCode -= 2;
			} else if (Input[endCode - 1] == '\n' && Input[endCode - 2] == '\r') {
				endCode -= 2;
			} else {
				endCode--;
			}

			// Create the child block with the entire content
			Block child = CreateBlock();
			child._BlockType = BlockType.indent;
			child._Buf = Input;
			child._ContentStart = startCode;
			child.ContentEnd = endCode;
			b._Children.Add(child);

			return true;
		}

		internal enum MarkdownInHtmlMode {
			/// <summary> No markdown attribute on the tag </summary>
			Na, 
			/// <summary> markdown=1 or markdown=block </summary>
			Block, 
			/// <summary> markdown=1 or markdown=span </summary>
			Span, 
			/// <summary> markdown=deep - recursive block mode </summary>
			Deep, 
			/// <summary> Markdown="something else" </summary>
			Off, 
		}
	}
}