using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	public class SpecialCharacterTests {
		[SetUp]
		public void SetUp() => f = new SpanFormatter(new Markdown());

		SpanFormatter f;

		[Test]
		public void AmpersandsInParagraphs() => ClassicAssert.AreEqual("pre this &amp; that post",
				f.Format("pre this & that post"));

		[Test]
		public void AmpersandsInUrls() => ClassicAssert.AreEqual("pre <a href=\"somewhere.html?arg1=a&amp;arg2=b\" target=\"_blank\">link</a> post",
				f.Format("pre <a href=\"somewhere.html?arg1=a&arg2=b\" target=\"_blank\">link</a> post"));

		[Test]
		public void EscapeChars() => ClassicAssert.AreEqual(@"\ ` * _ { } [ ] ( ) # + - . ! &gt;",
				f.Format(@"\\ \` \* \_ \{ \} \[ \] \( \) \# \+ \- \. \! \>"));

		[Test]
		public void HtmlEntities() {
			ClassicAssert.AreEqual("pre &amp; post",
				f.Format("pre &amp; post"));
			ClassicAssert.AreEqual("pre &#123; post",
				f.Format("pre &#123; post"));
			ClassicAssert.AreEqual("pre &#x1aF; post",
				f.Format("pre &#x1aF; post"));
		}

		[Test]
		public void NotATag() => ClassicAssert.AreEqual("pre a &lt; b post",
				f.Format("pre a < b post"));

		[Test]
		public void NotATag2() => ClassicAssert.AreEqual("pre a&lt;b post",
				f.Format("pre a<b post"));

		[Test]
		public void SimpleTag() => ClassicAssert.AreEqual(f.Format("pre <a> post"), "pre <a> post");

		[Test]
		public void TagWithAttributes() => ClassicAssert.AreEqual(f.Format("pre <a href=\"somewhere.html\" target=\"_blank\">link</a> post"),
				"pre <a href=\"somewhere.html\" target=\"_blank\">link</a> post");
	}
}