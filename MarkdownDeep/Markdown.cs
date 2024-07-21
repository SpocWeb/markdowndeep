using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MarkdownDeep {

	/// <summary>  public API to MarkdownDeep; Transform method, configurable options and object pooling. </summary>
	public class Markdown {

		public string AsHtml(string markDown) => AsHtml(markDown, out var defs);

		// Transform a string
		public string AsHtml(string markDown, out Dictionary<string, LinkDefinition> definitions) {
			// Build blocks
			List<Block> blocks = ProcessBlocks(markDown);
			definitions = _LinkDefinitions;

			// Sort abbreviations by length, longest to shortest
			if (_AbbreviationMap != null) {
				_AbbreviationList = new List<Abbreviation>();
				_AbbreviationList.AddRange(_AbbreviationMap.Values);
				_AbbreviationList.Sort((a, b) => b.Abbr.Length - a.Abbr.Length);
			}

			return Render(blocks);
		}

		#region RenderHtml

		public bool DoRenderMarkDown;

		string Render(IList<Block> blocks) {
			StringBuilder sb = _StringBuilderFinal;
			sb.Length = 0;
			if (SummaryLength > 0) {
				return RenderAllBlocksPlain(blocks, sb);
			}
			int iSection = -1;
			// Leading section (ie: plain text before first heading)
			if (blocks.Count > 0 && !blocks[0].IsSectionHeader()) {
				iSection = 0;
				OnSectionHeader(sb, 0);
				OnSectionHeadingSuffix(sb, 0);
			}
			foreach (var block in blocks) {
				iSection = RenderBlock(block, iSection, sb);
			}
			// Finish final section
			if (blocks.Count > 0) {
				OnSectionFooter(sb, iSection);
			}
			RenderFootnotes(sb);
			return sb.ToString();
		}

		string RenderAllBlocksPlain(IEnumerable<Block> blocks, StringBuilder sb) {
			foreach (var block in blocks) {
				block.RenderPlain(this, sb);
				if (sb.Length > SummaryLength) {
					break;
				}
			}
			return sb.ToString();
		}

		int RenderBlock(Block b, int iSection, StringBuilder sb) {
// New section?
			if (!b.IsSectionHeader()) { // Regular section
				b.Render(this, sb, IncludeMarkup);
				return iSection;
			}
			// Finish the previous section
			if (iSection >= 0) {
				OnSectionFooter(sb, iSection);
			}

			// Work out next section index
			iSection = iSection < 0 ? 1 : iSection + 1;

			// Section header
			OnSectionHeader(sb, iSection);

			// Section Heading
			b.Render(this, sb, IncludeMarkup);

			// Section Heading suffix
			OnSectionHeadingSuffix(sb, iSection);
			return iSection;
		}

		void RenderFootnotes(StringBuilder sb) {
			if (_UsedFootnotes.Count > 0) {
				sb.Append("\n<div class=\"");
				sb.Append(HtmlClassFootnotes);
				sb.Append("\">\n");
				sb.Append("<hr />\n");
				sb.Append("<ol>\n");
				for (int i = 0; i < _UsedFootnotes.Count; i++) {
					Block fn = _UsedFootnotes[i];

					sb.Append("<li id=\"fn:");
					sb.Append((string) fn._Data); // footnote id
					sb.Append("\">\n");


					// We need to get the return link appended to the last paragraph
					// in the footnote
					string strReturnLink = $"<a href=\"#fnref:{fn._Data}\" rev=\"footnote\">&#8617;</a>";

					// Get the last child of the footnote
					Block child = fn._Children[fn._Children.Count - 1];
					if (child._BlockType == BlockType.p) {
						child._BlockType = BlockType.p_footnote;
						child._Data = strReturnLink;
					} else {
						child = CreateBlock();
						child._ContentLen = 0;
						child._BlockType = BlockType.p_footnote;
						child._Data = strReturnLink;
						fn._Children.Add(child);
					}


					fn.Render(this, sb, IncludeMarkup);

					sb.Append("</li>\n");
				}
				sb.Append("</ol>\n");
				sb.Append("</div>\n");
			}
		}

		#endregion RenderHtml

		// Constructor
		readonly Dictionary<string, Block> _Footnotes = new();
		readonly Dictionary<string, LinkDefinition> _LinkDefinitions = new(StringComparer.CurrentCultureIgnoreCase);
		readonly StringBuilder _StringBuilder = new();
		readonly StringBuilder _StringBuilderFinal = new();
		internal readonly StringScanner _StringScanner = new();
		readonly List<Block> _UsedFootnotes = new();
		readonly Dictionary<string, bool> _UsedHeaderIDs = new();

		#region Providers 
		public Func<Markdown, string, string> FormatCodeBlock;
		internal Func<ImageInfo, bool> GetImageSize;
		internal Func<HtmlTag, bool, bool> PrepareImage;
		internal Func<HtmlTag, bool> PrepareLink;
		internal Func<string, string>? QualifyUrl;
		#endregion Providers

		internal bool RenderingTitledImage = false;
		List<Abbreviation>? _AbbreviationList;
		Dictionary<string, Abbreviation>? _AbbreviationMap;

		public Markdown() {
			HtmlClassFootnotes = "footnotes";
			SpanFormatter = new SpanFormatter(this);
		}

		#region Public Settings

		public int SummaryLength { get; set; }

		/// <summary> Set to true to only allow whitelisted safe html tags </summary>
		public bool IsSafeMode { get; set; }

		/// <summary>
		///  Set to true to enable ExtraMode, which enables the same set of features as implemented by PHP Markdown Extra.
		///  - Markdown in html (eg: <div markdown="1"/> or <div markdown="deep"/> )
		///  - Header ID attributes
		///  - Fenced code blocks
		///  - Definition lists
		///  - Footnotes
		///  - Abbreviations
		///  - Simple tables
		/// </summary>
		public bool IsExtraMode { get; set; }

		/// <summary>
		/// When set, all html block level elements automatically support markdown syntax within them. 
		/// (Similar to Pandoc's handling of markdown in html)
		/// </summary>
		public bool AllowMarkdownInHtmlBlock { get; set; }

		/// <summary> When set, all Markup is added to the html </summary>
		public bool IncludeMarkdownInHtmlBlock = true;

		/// <summary> When set, all headings will have an auto generated ID attribute based on the heading text (uses the same algorithm as Pandoc) </summary>
		public bool GenerateHeadingIDs { get; set; }

		/// <summary> When set, all non-qualified urls (links and images) will be qualified using this location as the base. </summary>
		/// <remarks>
		/// Useful when rendering RSS feeds that require fully qualified urls.
		/// </remarks>
		public string? UrlBaseLocation { get; set; }

        /*/ When set, all non-qualified urls (links and images) begining with a slash
		// will qualified by prefixing with this string.
		// Useful when rendering RSS feeds that require fully qualified urls.
		string UrlRootLocation { get; set; }
		*/
        /// <summary> When true, all fully qualified urls will be give `target="_blank"` attribute
        /// causing them to appear in a separate browser window/tab
        /// ie: relative links open in same window, qualified links open externally
        /// </summary>
        public bool UseNewWindowForExternalLinks { get; set; }

		/// <summary> When true, all urls (qualified or not) will get target="_blank" attribute (useful for preview mode on posts) </summary>
		public bool UseNewWindowForLocalLinks { get; set; }

		/// <summary> Local file system location of the Web root.  Used to locate image files that start with slash. </summary>
		/// <remarks>
		/// When set, will try to determine the width/height for local images by searching
		/// for an appropriately named file relative to the specified location
		/// </remarks>
		/// <example>c:\inetpub\www\wwwroot</example>
		public string DocumentRoot { get; set; }

		/// <summary> Local file system location of the current document.  Used to locate relative path images for image size. </summary>
		/// <example>c:\inetpub\www\wwwroot\subfolder</example>
		public string DocumentLocation { get; set; }

		/// <summary> Limit the width of images (0 for no limit) </summary>
		public int MaxImageWidth { get; set; }

		/// <summary> Sets rel="nofollow" on all links </summary>
		public bool GenerateNoFollowLinks { get; set; }

		/// <summary>Add the NoFollow attribute to all external links.</summary>
		public bool GenerateNoFollowExternalLinks { get; set; }

		// Set the html class for the footnotes div
		// (defaults to "footnotes")
		// btw fyi: you can use css to disable the footnotes horizontal rule. eg:
		// div.footnotes hr { display:none }
		public string HtmlClassFootnotes { get; set; }

		// Callback to format a code block (ie: apply syntax highlighting)
		// string FormatCodeBlock(code)
		// Code = code block content (ie: the code to format)
		// Return the formatted code, including <pre> and <code> tags

		/// <summary> when set to true, will remove head blocks and make content available as HeadBlockContent </summary>
		public bool DoExtractHeadBlocks { get; set; }

		/// <summary> Retrieve extracted head block content </summary>
		public string HeadBlockContent { get; internal set; }

		/// <summary> Treats "===" as a user section break </summary>
		public bool AllowUserBreaks { get; set; }

		/// <summary> Set the classname for titled images </summary>
		/// <remarks>
		/// A titled image is defined as a paragraph that contains an image and nothing else.
		/// If not set (the default), this features is disabled, otherwise the output is:
		/// 
		/// <div class="{%=this.HtmlClassTitledImags%>">
		///	<img src="image.png" />
		///	<p>Alt text goes here</p>
		/// </div>
		///
		/// Use CSS to style the figure and the caption
		/// </remarks>
		public string? HtmlClassTitledImages { get; set; }

		/// <summary> Set a format string to be rendered before headings </summary>
		/// <remarks>
		/// {0} = section number
		/// (useful for rendering links that can lead to a page that edits that section)
		/// (eg: "<a href='/edit/page?section={0}' />"
		/// </remarks>
		public string? SectionHeader { get; set; }

		/// <summary> Set a format string to be rendered after each section heading </summary>
		public string? SectionHeadingSuffix { get; set; }

		/// <summary> Set a format string to be rendered after the section content  </summary>
		/// <remarks>
		/// (ie: before the next section heading, or at the end of the document).
		/// </remarks>
		public string? SectionFooter { get; set; }

		#endregion Public Settings

		internal SpanFormatter SpanFormatter { get; }

		#region Block Pooling

		// We cache and re-use blocks for Performance

		readonly Stack<Block> _SpareBlocks = new();
		public bool IncludeMarkup;

		internal Block CreateBlock() {
			if (_SpareBlocks.Count != 0) {
				return _SpareBlocks.Pop();
			}
			return new Block();
		}

		internal void FreeBlock(Block b) => _SpareBlocks.Push(b);

		#endregion

		internal List<Block> ProcessBlocks(string str) {
			// Reset the list of link definitions
			_LinkDefinitions.Clear();
			_Footnotes.Clear();
			_UsedFootnotes.Clear();
			_UsedHeaderIDs.Clear();
			_AbbreviationMap = null;
			_AbbreviationList = null;

			// Process blocks
			return new BlockParser(this, AllowMarkdownInHtmlBlock, IncludeMarkdownInHtmlBlock).Process(str);
		}

		#region polymorphic Callbacks

		protected virtual string OnQualifyUrl(string url) {
			string q = QualifyUrl?.Invoke(url);
			if (q != null) {
				return url;
			}

			// Quit if we don't have a base location
			if (string.IsNullOrEmpty(UrlBaseLocation)) {
				return url;
			}

			// Is the url a fragment?
			if (url.StartsWith("#")) {
				return url;
			}

			// Is the url already fully qualified?
			if (Utils.IsUrlFullyQualified(url)) {
				return url;
			}

			if (url.StartsWith("/")) {
				/*if (!string.IsNullOrEmpty(UrlRootLocation)) {
					return UrlRootLocation + url;
				}*/

				// Need to find domain root
				int pos = UrlBaseLocation.IndexOf("://", StringComparison.Ordinal);
				if (pos == -1) {
					pos = 0;
				} else {
					pos += 3;
				}

				// Find the first slash after the protocol separator
				pos = UrlBaseLocation.IndexOf('/', pos);

				// Get the domain name
				string strDomain = pos < 0 ? UrlBaseLocation : UrlBaseLocation.Substring(0, pos);

				// Join em
				return strDomain + url;
			}
			if (!UrlBaseLocation.EndsWith("/")) {
				return UrlBaseLocation + "/" + url;
			}
			return UrlBaseLocation + url;
		}

		protected virtual bool OnGetImageSize(string url, bool titledImage, out int width, out int height) {
			if (GetImageSize != null) {
				var info = new ImageInfo {Url = url, IsTitledImage = titledImage};
				if (GetImageSize(info)) {
					width = info.Width;
					height = info.Height;
					return true;
				}
			}

			width = 0;
			height = 0;

			if (Utils.IsUrlFullyQualified(url)) {
				return false;
			}

			// Work out base location
			string str = url.StartsWith("/") ? DocumentRoot : DocumentLocation;
			if (string.IsNullOrEmpty(str)) {
				return false;
			}

			// Work out file location
			if (str.EndsWith("/") || str.EndsWith("\\")) {
				str = str.Substring(0, str.Length - 1);
			}

			if (url.StartsWith("/")) {
				url = url.Substring(1);
			}

			str = str + "\\" + url.Replace("/", "\\");


			// 

			//Create an image object from the uploaded file
			try {
				Image img = Image.FromFile(str);
				width = img.Width;
				height = img.Height;

				if (MaxImageWidth != 0 && width > MaxImageWidth) {
					height = (int) (height*(double) MaxImageWidth/width);
					width = MaxImageWidth;
				}

				return true;
			} catch (Exception) {
				return false;
			}
		}

		public virtual void OnPrepareLink(HtmlTag tag) {
			if (PrepareLink != null) {
				if (PrepareLink(tag)) {
					return;
				}
			}

			string url = tag.Attributes["href"];

			// No follow?
			if (GenerateNoFollowLinks) {
				tag.Attributes["rel"] = "nofollow";
			}

			// No follow external links only
			if (GenerateNoFollowExternalLinks) {
				if (Utils.IsUrlFullyQualified(url)) {
					tag.Attributes["rel"] = "nofollow";
				}
			}


			// New window?
			if ((UseNewWindowForExternalLinks && Utils.IsUrlFullyQualified(url)) ||
				(UseNewWindowForLocalLinks && !Utils.IsUrlFullyQualified(url))) {
				tag.Attributes["target"] = "_blank";
			}

			// Qualify url
			tag.Attributes["href"] = OnQualifyUrl(url);
		}

		public virtual void OnPrepareImage(HtmlTag tag, bool titledImage) {
			if (PrepareImage != null) {
				if (PrepareImage(tag, titledImage)) {
					return;
				}
			}

			// Try to determine width and height
			if (OnGetImageSize(tag.Attributes["src"], titledImage, out var width, out var height)) {
				tag.Attributes["width"] = width + "";
				tag.Attributes["height"] = height + "";
			}

			// Now qualify the url
			tag.Attributes["src"] = OnQualifyUrl(tag.Attributes["src"]);
		}

		internal virtual void OnSectionHeader(StringBuilder dest, int index) {
			if (SectionHeader != null) {
				dest.AppendFormat(SectionHeader, index);
			}
		}

		internal virtual void OnSectionHeadingSuffix(StringBuilder dest, int index) {
			if (SectionHeadingSuffix != null) {
				dest.AppendFormat(SectionHeadingSuffix, index);
			}
		}

		internal virtual void OnSectionFooter(StringBuilder dest, int index) {
			if (SectionFooter != null) {
				dest.AppendFormat(SectionFooter, index);
			}
		}

		#endregion polymorphic Callbacks

		// Add a link definition
		internal void AddLinkDefinition(LinkDefinition link) =>
			// Store it
			_LinkDefinitions[link.Id] = link;

		internal void AddFootnote(Block footnote) => _Footnotes[(string) footnote._Data] = footnote;

		// Look up a footnote, claim it and return it's index (or -1 if not found)
		internal int ClaimFootnote(string id) {
			if (_Footnotes.TryGetValue(id, out var footnote)) {
				// Move the foot note to the used footnote list
				_UsedFootnotes.Add(footnote);
				_Footnotes.Remove(id);

				// Return it's display index
				return _UsedFootnotes.Count - 1;
			}
			return -1;
		}

		// Get a link definition
		public LinkDefinition GetLinkDefinition(string id) {
			if (_LinkDefinitions.TryGetValue(id, out var link)) {
				return link;
			}
			return null;
		}

		internal void AddAbbreviation(string abbr, string title) {
			if (_AbbreviationMap == null) {
				// First time
				_AbbreviationMap = new Dictionary<string, Abbreviation>();
			} else if (_AbbreviationMap.ContainsKey(abbr)) {
				// Remove previous
				_AbbreviationMap.Remove(abbr);
			}

			// Store abbreviation
			_AbbreviationMap.Add(abbr, new Abbreviation(abbr, title));
		}

		internal List<Abbreviation>? GetAbbreviations() => _AbbreviationList;

		// HtmlEncode a range in a string to a specified string builder
		internal void HtmlEncode(StringBuilder dest, string str, int start, int len) {
			_StringScanner.Reset(str, start, len);
			StringScanner p = _StringScanner;
			while (!p.Eof) {
				char ch = p.Current;
				switch (ch) {
					case '&': dest.Append("&amp;"); break;
					case '<': dest.Append("&lt;"); break;
					case '>': dest.Append("&gt;"); break;
					case '\"': dest.Append("&quot;"); break;
					default:
						dest.Append(ch);
						break;
				}
				p.SkipForward(1);
			}
		}

		internal string MakeUniqueHeaderId(string strHeaderText) => MakeUniqueHeaderId(strHeaderText, 0, strHeaderText.Length);

		internal string MakeUniqueHeaderId(string strHeaderText, int startOffset, int length) {
			if (!GenerateHeadingIDs) {
				return null;
			}

			// Extract a pandoc style cleaned header id from the header text
			string strBase = SpanFormatter.MakeId(strHeaderText, startOffset, length);

			// If nothing left, use "section"
			if (string.IsNullOrEmpty(strBase)) {
				strBase = "section";
			}

			// Make sure it's unique by append -n counter
			string strWithSuffix = strBase;
			int counter = 1;
			while (_UsedHeaderIDs.ContainsKey(strWithSuffix)) {
				strWithSuffix = strBase + "-" + counter;
				counter++;
			}

			// Store it
			_UsedHeaderIDs.Add(strWithSuffix, true);

			// Return it
			return strWithSuffix;
		}


		internal StringBuilder GetStringBuilder() {
			_StringBuilder.Length = 0;
			return _StringBuilder;
		}
	}

}