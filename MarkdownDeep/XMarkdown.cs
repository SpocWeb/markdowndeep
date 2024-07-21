using System.Collections.Generic;
using System.Text;

namespace MarkdownDeep {

	/// <summary>
	/// http://pandoc.org (Haskell) can convert many Markups into another: 
	/// markdown, reStructuredText, textile, HTML, DocBook, LaTeX, MediaWiki markup
	/// , TWiki markup, OPML, Emacs Org-Mode, Txt2Tags, Microsoft Word docx, EPUB, or Haddock markup to
	/// * HTML formats: XHTML, HTML5, and HTML slide shows using Slidy, reveal.js, Slideous, S5, or DZSlides.
	/// * Word processor formats: Microsoft Word docx, OpenOffice/LibreOffice ODT, OpenDocument XML
	/// * Ebooks: EPUB version 2 or 3, FictionBook2
	/// * Documentation formats: DocBook, GNU TexInfo, Groff man pages, Haddock markup
	/// * Page layout formats: InDesign ICML
	/// * Outline formats: OPML
	/// * TeX formats: LaTeX, ConTeXt, LaTeX Beamer slides
	/// * PDF via LaTeX
	/// * Lightweight markup formats: Markdown, reStructuredText, AsciiDoc, MediaWiki markup, DokuWiki markup, Emacs Org-Mode, Textile
	/// * Custom formats: custom writers can be written in lua.
	/// </summary>
	/// <remarks>
	/// MarkdownDeep takes a different approach to most other Markdown processors. 
	/// Instead of using a series of Regular Expression replacements (like AvalonDoc), 
	/// MarkdownDeep builds a document hierarchy which it then renders.
	/// 
	/// Block processing is the process of parsing the input document into a hierarchial tree 
	/// representing the structure of the document.
	/// 
	/// The input text is loaded into a StringScanner which is then passed to a BlockProcessor.
	/// The BlockProcessor evaluates the input text creating a Block for each line.
	/// Sequences of line blocks are then assessed and related lines are merged 
	/// to form the actual block structure of the document. 
	/// (eg: consecutive indent blocks are folded into code blocks, consecutive list items into lists etc...).
	/// 
	/// Nested structures (eg: blockquotes inside blockquotes) are also handled by the BlockProcessor 
	/// by creating a new input string containing the nested input 
	/// and recusively passing that through a another BlockProcessor instance.
	/// 
	/// Once the block structure of the document has been parsed, 
	/// those blocks are recursively rendered to a StringBuilder to produce the final output. 
	/// As part of the rendering process, spans of text are formatted using a SpanFormatter:
	/// 
	/// Each span of text is tokenized into a series of Tokens representing the internal content of the text span.
	/// Balanced tokens such as emphasis and strong are matched against each other.
	/// Tokens are rendered to a StringBuilder to produce the final output.
	/// 
	/// This approach has its pros and cons:
	/// 
	/// The C# implementation is significantly faster as the input text is scanned far fewer times.
	/// The code base is larger, but can also be debugged and extended more easily.
	/// The performance of the Javascript implementation depends heavily on the browser. 
	/// For Chrome, Firefox and Safari it performs 
	/// as well if not better than other regular expression based Javascript implementations. 
	/// For Internet Explorer, it performs considerably worse - but still acceptible for typical use. 
	/// Initial testing in IE9 Technology Preview shows performance comparable to the other browsers.
	/// 
	/// Ambiguous Markdown Input
	/// There are cases where Markdown input can be interpreted in multiple ways. 
	/// In these cases MarkdownDeep favours performance, code maintainability and correct mark-up 
	/// in preference to compatibility with other implementations and and may generate different output.
	/// 
	/// For example, when dealing with nested emphasis and bold indicators such as this:
	/// ***test** test*									many implementations of Markdown will generate this:
	/// <p><strong>{em}test</strong> test{/em}</p>		whereas MarkdownDeep will produce the more correct:
	/// <p><em><strong>test</strong> test</em></p>
	/// 
	/// In all cases where the Markdown syntax is unambiguous, MarkdownDeep generates equivalent output.
	/// 
	/// Whitespace in Output
	/// MarkdownDeep makes no effort to generate output that matches the whitespace output of other implementations, 
	/// nor does it maintain the whitespace of the input text 
	/// except where that whitespace affects the finally rendered page (eg: code blocks).
	/// 
	/// At some point an option may be added to do pretty formatted (indented) output.
	/// 
	/// Use of HTML Entities
	/// MarkdownDeep makes no promises on the use of HTML entities in it's output 
	/// and may generate different (but equivalent) output to other Markdown processors. 
	/// For example: MarkdownDeep transforms > into &gt; where as some other Markdown processors do not.
	/// 
	/// There are a number of cases where MarkdownDeep needs to handle special cases:
	/// Reverting setext headings to horizontal rules or normal paragraphs
	///		In line parsing lines that look like === or --- are marked as BlockType.post_h1 and BlockType.post_h2. 
	///		If these can't be matched to a preceeding line to made into a paragraph, 
	///		they're reverted to either a horizontal rule for the --- or a normal paragraph for the ===.
	/// 
	/// Reverting list items to normal paragraphs.
	///		When a line starts with a * or 1. style list item indicator, it's marked as a list item. 
	///		If that line immediately follows a normal paragraph line 
	///		the line needs to be considered part of the paragraph and not a list item. 
	///		In this case the list item is reverted to a plain paragraph including the leading * or 1. prefix.
	/// 
	/// Leading spaces on list items
	///		List item levels can be increased by spaces rather than the normal 4 character or tab indent mechanism. 
	///		In determining list levels, the normal indent classification of a block can't be relied on. 
	///		In this case, when building the list blocks, the leading space on list items is examined 
	///		and any lines that have more leading space than the first item in the list 
	///		are promoted from list item blocks to indent blocks.
	/// 
	/// Html Block Processing
	///		Normally the block processor works on a line by line bases. 
	///		In the case where it detects a block HTML tag, 
	///		it processes the entire multi-line structure as a single block immediately, 
	///		rather than processing it on a line by line basis.
	/// 
	/// Matching of <strong/> and <em/> tokens
	///		The algorithm for matching emphasis markers is documented in the span formatter class.
	/// 
	/// Titled Figures
	///		In order to implement titled figures, what would normally be rendered as a p tag 
	///		needs to be replaced with a div tag when the paragraph contains only an image. 
	///		Since we don't know about the content of the paragraph 
	///		until after the paragraph has been tokenized by the SpanFormatter, 
	///		there's a special method on the SpanFormatter called FormatParagraph 
	///		that is used when rendering p blocks. 
	///		It tokenizes the paragraph content and then renders either the normal p or the titled figure div tag.
	/// </remarks>
	public static class XMarkdown {

		/// <summary> Split the markdown into sections, one section for each top level heading </summary>
		public static List<string> SplitUserSections(string markdown) {
			// Build blocks
			var md = new Markdown {AllowUserBreaks = true};

			// Process blocks
			List<Block> blocks = md.ProcessBlocks(markdown);

			// Create sections
			var sections = new List<string>();
			int iPrevSectionOffset = 0;
			for (int i = 0; i < blocks.Count; i++) {
				Block b = blocks[i];
				if (b._BlockType == BlockType.user_break) {
					// Get the offset of the section
					int iSectionOffset = b.LineStart;

					// Add section
					sections.Add(markdown.Substring(iPrevSectionOffset, iSectionOffset - iPrevSectionOffset).Trim());

					// Next section starts on next line
					if (i + 1 < blocks.Count) {
						iPrevSectionOffset = blocks[i + 1].LineStart;
						if (iPrevSectionOffset == 0) {
							iPrevSectionOffset = blocks[i + 1]._ContentStart;
						}
					} else {
						iPrevSectionOffset = markdown.Length;
					}
				}
			}

			// Add the last section
			if (markdown.Length > iPrevSectionOffset) {
				sections.Add(markdown.Substring(iPrevSectionOffset).Trim());
			}

			return sections;
		}

		// Join previously split sections back into one document
		public static string JoinUserSections(List<string> sections) {
			var sb = new StringBuilder();
			for (int i = 0; i < sections.Count; i++) {
				if (i > 0) {
					// For subsequent sections, need to make sure we
					// have a line break after the previous section.
					string strPrev = sections[sections.Count - 1];
					if (strPrev.Length > 0 && !strPrev.EndsWith("\n") && !strPrev.EndsWith("\r")) {
						sb.Append("\n");
					}

					sb.Append("\n===\n\n");
				}

				sb.Append(sections[i]);
			}

			return sb.ToString();
		}

		/// <summary> Split the markdown into sections, one section for each top level heading </summary>
		static List<string> SplitSections(string markdown) {
			// Build blocks
			var md = new Markdown();

			// Process blocks
			List<Block> blocks = md.ProcessBlocks(markdown);

			// Create sections
			var sections = new List<string>();
			int iPrevSectionOffset = 0;
			for (int i = 0; i < blocks.Count; i++) {
				Block b = blocks[i];
				if (b.IsSectionHeader()) {
					// Get the offset of the section
					int iSectionOffset = b.LineStart;

					// Add section
					sections.Add(markdown.Substring(iPrevSectionOffset, iSectionOffset - iPrevSectionOffset));

					iPrevSectionOffset = iSectionOffset;
				}
			}

			// Add the last section
			if (markdown.Length > iPrevSectionOffset) {
				sections.Add(markdown.Substring(iPrevSectionOffset));
			}

			return sections;
		}

		/// <summary> Join previously split sections back into one document </summary>
		static string JoinSections(IList<string> sections) {
			var sb = new StringBuilder();
			for (int i = 0; i < sections.Count; i++) {
				if (i > 0) {
					// For subsequent sections, need to make sure we
					// have a line break after the previous section.
					string strPrev = sections[sections.Count - 1];
					if (strPrev.Length > 0 && !strPrev.EndsWith("\n") && !strPrev.EndsWith("\r")) {
						sb.Append("\n");
					}
				}

				sb.Append(sections[i]);
			}

			return sb.ToString();
		}

	}
}