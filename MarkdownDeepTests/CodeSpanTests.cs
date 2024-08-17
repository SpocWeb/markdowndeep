using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	public class CodeSpanTests {
		[SetUp]
		public void SetUp() => f = new SpanFormatter(new Markdown());

		SpanFormatter f;

		[Test]
		public void ContentEncoded() {
			ClassicAssert.AreEqual("pre <code>&lt;div&gt;</code> post",
				f.Format("pre ```` <div> ```` post"));
			ClassicAssert.AreEqual("pre <code>&amp;amp;</code> post",
				f.Format("pre ```` &amp; ```` post"));
		}

		[Test]
		public void MultiTick() => ClassicAssert.AreEqual("pre <code>code span</code> post",
				f.Format("pre ````code span```` post"));

		[Test]
		public void MultiTickWithEmbeddedTicks() => ClassicAssert.AreEqual("pre <code>`code span`</code> post",
				f.Format("pre ```` `code span` ```` post"));

		[Test]
		public void SingleTick() => ClassicAssert.AreEqual("pre <code>code span</code> post",
				f.Format("pre `code span` post"));

		[Test]
		public void SingleTickWithSpaces() => ClassicAssert.AreEqual("pre <code>code span</code> post",
				f.Format("pre ` code span ` post"));
	}
}