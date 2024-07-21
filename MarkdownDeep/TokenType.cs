namespace MarkdownDeep {

	internal enum TokenType {
		// ReSharper disable InconsistentNaming
		Text, // Plain text, should be htmlencoded
		HtmlTag, // Valid html tag, write out directly but escape &amps;
		Html, // Valid html, write out directly
		open_em, // <em>
		close_em, // </em>
		open_strong, // <strong>
		close_strong, // </strong>
		code_span, // <code></code>
		br, // <br />

		link, // <a href>, data = LinkInfo
		img, // <img>, data = LinkInfo
		footnote, // Footnote reference
		abbreviation, // An abbreviation, data is a reference to Abbrevation instance

		// These are used during construction of <em> and <strong> tokens
		opening_mark, // opening '*' or '_'
		closing_mark, // closing '*' or '_'
		internal_mark, // internal '*' or '_'
	}
}