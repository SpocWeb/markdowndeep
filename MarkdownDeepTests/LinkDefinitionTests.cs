using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class LinkDefinitionTests {
		[SetUp]
		public void Setup() => r = null;

		LinkDefinition r;

		[Test]
		public void AngleBracketedUrl() {
			string str = "[id]: <url.com> (my title)";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "url.com");
			ClassicAssert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void DoubleQuoteTitle() {
			string str = "[id]: url.com \"my title\"";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "url.com");
			ClassicAssert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void Invalid() {
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]:", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \"title", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \'title", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> (title", false));
			ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \"title\" crap", false));
            ClassicAssert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> crap", false));
		}

		[Test]
		public void MultiLine() {
			string str = "[id]:\n\t     http://www.site.com \n\t      (my title)";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "http://www.site.com");
			ClassicAssert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void NoTitle() {
			string str = "[id]: url.com";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "url.com");
			ClassicAssert.AreEqual(r.Title, null);
		}

		[Test]
		public void ParenthesizedTitle() {
			string str = "[id]: url.com (my title)";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "url.com");
			ClassicAssert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void SingleQuoteTitle() {
			string str = "[id]: url.com \'my title\'";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			ClassicAssert.IsNotNull(r);
			ClassicAssert.AreEqual(r.Id, "id");
			ClassicAssert.AreEqual(r.Url, "url.com");
			ClassicAssert.AreEqual(r.Title, "my title");
		}
	}
}