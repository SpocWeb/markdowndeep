namespace MarkdownDeep {

	/// <summary> Block types; Some are used only during block parsing, 
	/// some are only used during rendering and some are used during both
	/// </summary>
	internal enum BlockType {
		// ReSharper disable InconsistentNaming

		/// <summary> blank line (parse only) or <BR/>& nbsp;<BR/> </summary>
		Blank, 
		h1, // headings (render and parse)
		h2,
		h3,
		h4,
		h5,
		h6,
		
		/// <summary> === setext heading1 line (parse only) </summary>
		post_h1,
		
		/// <summary> --- setext heading2 line (parse only) </summary>
		post_h2,

		/// <summary> block quote (render and parse) </summary>
		quote,

		/// <summary> ordered list item 	(render and parse) </summary>
		ol_li,

		/// <summary> unordered list item (render and parse) </summary>
		ul_li, 
		
		/// <summary> paragraph (or plain line during parse) </summary>
		p, 
		
		/// <summary> an indented line (parse only) </summary>
		indent, 
		hr, // horizontal rule (render and parse)
		user_break, // user break
		html, // html content (render and parse)
		unsafe_html, // unsafe html that should be encoded
		span, // an undecorated span of text (used for simple list items 
		//			where content is not wrapped in paragraph tags
		codeblock, // a code block (render only)
		li, // a list item (render only)
		ol, // ordered list (render only)
		ul, // unordered list (render only)
		HtmlTag, // Data=(HtmlTag), children = content
		Composite, // Just a list of child blocks
		table_spec, // A table row specifier eg:  |---: | ---|	`data` = TableSpec reference
		/// <summary> definition (render and parse)	`data` = bool true if blank line before </summary>
		dd, // 
		/// <summary> footnote definition  eg: [^id]   `data` holds the footnote id </summary>
		footnote,
		dt, // render only
		dl, // render only
		p_footnote, // paragraph with footnote return link append.  Return link string is in `data`.
	}
}