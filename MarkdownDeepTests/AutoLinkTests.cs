using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class AutoLinkTests {
		[SetUp]
		public void SetUp() {
			m = new Markdown();
			s = new SpanFormatter(m);
		}

		Markdown m;
		SpanFormatter s;

		[Test]
		public void ftp() => ClassicAssert.AreEqual("pre <a href=\"ftp://url.com\">ftp://url.com</a> post",
				s.Format("pre <ftp://url.com> post"));

		[Test]
		public void http() => ClassicAssert.AreEqual("pre <a href=\"http://url.com\">http://url.com</a> post",
				s.Format("pre <http://url.com> post"));

		[Test]
		public void https() => ClassicAssert.AreEqual("pre <a href=\"https://url.com\">https://url.com</a> post",
				s.Format("pre <https://url.com> post"));
	}
}