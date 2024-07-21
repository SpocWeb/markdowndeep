using System.Collections.Generic;
using MarkdownDeep;
using NUnit.Framework;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class BlockProcessorTests {
		[SetUp]
		public void Setup() => _P = new BlockParser(new Markdown(), false, false);

		BlockParser _P;

		[Test]
		public void AtxHeaders() {
			List<Block> b = _P.Process("#heading#\nparagraph\n");
			Assert.AreEqual(2, b.Count);

			Assert.AreEqual(BlockType.h1, b[0]._BlockType);
			Assert.AreEqual("heading", b[0].Content);

			Assert.AreEqual(BlockType.p, b[1]._BlockType);
			Assert.AreEqual("paragraph", b[1].Content);
		}

		[Test]
		public void NestedBlocks() {
			List<Block> blocks = _P.Process(
@"*   [Miscellaneous](#misc)
    *   [Backslash Escapes](#backslash)
    *   [Automatic Links](#autolink)

**Note:** This document is itself writtn using Markdown; you
");
			Assert.AreEqual(2, blocks.Count);

			Assert.AreEqual(BlockType.ul, blocks[0]._BlockType);
			Assert.AreEqual(2, blocks[0]._Children.Count);

		}

		[Test]
		public void AtxHeadingInParagraph() {
			List<Block> b = _P.Process("p1\n## heading ##\np2\n");

			Assert.AreEqual(3, b.Count);

			Assert.AreEqual(BlockType.p, b[0]._BlockType);
			Assert.AreEqual("p1", b[0].Content);

			Assert.AreEqual(BlockType.h2, b[1]._BlockType);
			Assert.AreEqual("heading", b[1].Content);

			Assert.AreEqual(BlockType.p, b[2]._BlockType);
			Assert.AreEqual("p2", b[2].Content);
		}

		[Test]
		public void CodeBlock() {
			List<Block> b = _P.Process("\tcode1\n\t\tcode2\n\tcode3\nparagraph");
			Assert.AreEqual(2, b.Count);

			Block cb = b[0];
			Assert.AreEqual("code1\n\tcode2\ncode3\n", cb.Content);

			Assert.AreEqual(BlockType.p, b[1]._BlockType);
			Assert.AreEqual("paragraph", b[1].Content);
		}

		[Test]
		public void HorizontalRules() {
			List<Block> b = _P.Process("---\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("___\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("***\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process(" - - - \n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process("  _ _ _ \n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);

			b = _P.Process(" * * * \n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.hr, b[0]._BlockType);
		}

		[Test]
		public void HtmlBlock() {
			List<Block> b = _P.Process("<div>\n</div>\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.html, b[0]._BlockType);
			Assert.AreEqual("<div>\n</div>\n", b[0].Content);
		}

		[Test]
		public void HtmlCommentBlock() {
			List<Block> b = _P.Process("<!-- this is a\ncomments -->\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.html, b[0]._BlockType);
			Assert.AreEqual("<!-- this is a\ncomments -->\n", b[0].Content);
		}

		[Test]
		public void MultilineParagraph() {
			List<Block> b = _P.Process("l1\nl2\n\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.p, b[0]._BlockType);
			Assert.AreEqual("l1\nl2", b[0].Content);
		}

		[Test]
		public void SetExtH1() {
			List<Block> b = _P.Process("heading\n===\n\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.h1, b[0]._BlockType);
			Assert.AreEqual("heading", b[0].Content);
		}

		[Test]
		public void SetExtH2() {
			List<Block> b = _P.Process("heading\n---\n\n");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.h2, b[0]._BlockType);
			Assert.AreEqual("heading", b[0].Content);
		}

		[Test]
		public void SetExtHeadingInParagraph() {
			List<Block> b = _P.Process("p1\nheading\n---\np2\n");
			Assert.AreEqual(3, b.Count);

			Assert.AreEqual(BlockType.p, b[0]._BlockType);
			Assert.AreEqual("p1", b[0].Content);

			Assert.AreEqual(BlockType.h2, b[1]._BlockType);
			Assert.AreEqual("heading", b[1].Content);

			Assert.AreEqual(BlockType.p, b[2]._BlockType);
			Assert.AreEqual("p2", b[2].Content);
		}

		[Test]
		public void SingleLineParagraph() {
			List<Block> b = _P.Process("paragraph");
			Assert.AreEqual(1, b.Count);
			Assert.AreEqual(BlockType.p, b[0]._BlockType);
			Assert.AreEqual("paragraph", b[0].Content);
		}
	}
}