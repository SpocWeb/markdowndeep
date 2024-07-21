using System;

namespace MarkdownDeep {
	[Flags]
	public enum HtmlTagFlags {
		Block = 0x0001, // Block tag
		Inline = 0x0002, // Inline tag
		NoClosing = 0x0004, // No closing tag (eg: <hr> and <!-- -->)
		ContentAsSpan = 0x0008, // When markdown=1 treat content as span, not block
	};
}