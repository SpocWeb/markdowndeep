using MarkdownDeep;
using NUnit.Framework;

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

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "url.com");
			Assert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void DoubleQuoteTitle() {
			string str = "[id]: url.com \"my title\"";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "url.com");
			Assert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void Invalid() {
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]:", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \"title", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \'title", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> (title", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> \"title\" crap", false));
			Assert.IsNull(LinkDefinition.ParseLinkDefinition("[id]: <url> crap", false));
		}

		[Test]
		public void MultiLine() {
			string str = "[id]:\n\t     http://www.site.com \n\t      (my title)";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "http://www.site.com");
			Assert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void NoTitle() {
			string str = "[id]: url.com";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "url.com");
			Assert.AreEqual(r.Title, null);
		}

		[Test]
		public void ParenthesizedTitle() {
			string str = "[id]: url.com (my title)";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "url.com");
			Assert.AreEqual(r.Title, "my title");
		}

		[Test]
		public void SingleQuoteTitle() {
			string str = "[id]: url.com \'my title\'";
			r = LinkDefinition.ParseLinkDefinition(str, false);

			Assert.IsNotNull(r);
			Assert.AreEqual(r.Id, "id");
			Assert.AreEqual(r.Url, "url.com");
			Assert.AreEqual(r.Title, "my title");
		}
	}
}