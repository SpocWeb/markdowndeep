using System.Collections.Generic;
using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class BlockProcessorTests {
		[SetUp]
		public void Setup() => _P = new BlockParser(new Markdown(), false, false);

		BlockParser _P;

		[Test]
		public void AtxHeaders() {
			List<Block> b = _P.Process("#heading#\nparagraph\n");
			ClassicAssert.AreEqual(2, b.Count);

			ClassicAssert.AreEqual(BlockType.h1, b[0]._BlockType);
			ClassicAssert.AreEqual("heading", b[0].Content);

			ClassicAssert.AreEqual(BlockType.p, b[1]._BlockType);
			ClassicAssert.AreEqual("paragraph", b[1].Content);
		}

		[Test]
		public void NestedBlocks() {
			List<Block> blocks = _P.Process(
@"*   [Miscellaneous](#misc)
    *   [Backslash Escapes](#backslash)
    *   [Automatic Links](#autolink)

**Note:** This document is itself writtn using Markdown; you
");
			ClassicAssert.AreEqual(2, blocks.Count);

			ClassicAssert.AreEqual(BlockType.ul, blocks[0]._BlockType);
			ClassicAssert.AreEqual(2, blocks[0]._Children.Count);

		}

		[Test]
		public void AtxHeadingInParagraph() {
			List<Block> b = _P.Process("p1\n## heading ##\np2\n");

			ClassicAssert.AreEqual(3, b.Count);

			ClassicAssert.AreEqual(BlockType.p, b[0]._BlockType);
			ClassicAssert.AreEqual("p1", b[0].Content);

			ClassicAssert.AreEqual(BlockType.h2, b[1]._BlockType);
			ClassicAssert.AreEqual("heading", b[1].Content);

			ClassicAssert.AreEqual(BlockType.p, b[2]._BlockType);
			ClassicAssert.AreEqual("p2", b[2].Content);
		}

		[Test]
		public void CodeBlock() {
			List<Block> b = _P.Process("\tcode1\n\t\tcode2\n\tcode3\nparagraph");
			ClassicAssert.AreEqual(2, b.Count);

			Block cb = b[0];
			ClassicAssert.AreEqual("code1\n\tcode2\ncode3\n", cb.Content);

			ClassicAssert.AreEqual(BlockType.p, b[1]._BlockType);
			ClassicAssert.AreEqual("paragraph", b[1].Content);
		}

		[Test]
		public void HorizontalRules() {
			List<Block> b = _P.Process("---\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("___\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("***\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process(" - - - \n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("  _ _ _ \n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process(" * * * \n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.hr, b[0]._BlockType);
		}

		[Test]
		public void HtmlBlock() {
			List<Block> b = _P.Process("<div>\n</div>\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.html, b[0]._BlockType);
			ClassicAssert.AreEqual("<div>\n</div>\n", b[0].Content);
		}

		[Test]
		public void HtmlCommentBlock() {
			List<Block> b = _P.Process("<!-- this is a\ncomments -->\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.html, b[0]._BlockType);
			ClassicAssert.AreEqual("<!-- this is a\ncomments -->\n", b[0].Content);
		}

		[Test]
		public void MultilineParagraph() {
			List<Block> b = _P.Process("l1\nl2\n\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.p, b[0]._BlockType);
			ClassicAssert.AreEqual("l1\nl2", b[0].Content);
		}

		[Test]
		public void SetExtH1() {
			List<Block> b = _P.Process("heading\n===\n\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.h1, b[0]._BlockType);
			ClassicAssert.AreEqual("heading", b[0].Content);
		}

		[Test]
		public void SetExtH2() {
			List<Block> b = _P.Process("heading\n---\n\n");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.h2, b[0]._BlockType);
			ClassicAssert.AreEqual("heading", b[0].Content);
		}

		[Test]
		public void SetExtHeadingInParagraph() {
			List<Block> b = _P.Process("p1\nheading\n---\np2\n");
			ClassicAssert.AreEqual(3, b.Count);

			ClassicAssert.AreEqual(BlockType.p, b[0]._BlockType);
			ClassicAssert.AreEqual("p1", b[0].Content);

			ClassicAssert.AreEqual(BlockType.h2, b[1]._BlockType);
			ClassicAssert.AreEqual("heading", b[1].Content);

			ClassicAssert.AreEqual(BlockType.p, b[2]._BlockType);
			ClassicAssert.AreEqual("p2", b[2].Content);
		}

		[Test]
		public void SingleLineParagraph() {
			List<Block> b = _P.Process("paragraph");
			ClassicAssert.AreEqual(1, b.Count);
			ClassicAssert.AreEqual(BlockType.p, b[0]._BlockType);
			ClassicAssert.AreEqual("paragraph", b[0].Content);
		}
	}
}