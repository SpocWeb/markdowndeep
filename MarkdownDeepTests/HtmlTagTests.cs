using MarkdownDeep;
using NUnit.Framework;

namespace MarkdownDeepTests {

	[TestFixture]
	internal class HtmlTagTests {
		[SetUp]
		public void SetUp() => _Pos = 0;

		int _Pos;

		[Test]
		public void Closed() {
			const string str = "<div/>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, true);
			Assert.AreEqual(tag.Attributes.Count, 0);
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void ClosedWithAttribs() {
			const string str = "<div x=1 y=2/>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, true);
			Assert.AreEqual(tag.Attributes.Count, 2);
			Assert.AreEqual(tag.Attributes["x"], "1");
			Assert.AreEqual(tag.Attributes["y"], "2");
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Closing() {
			const string str = "</div>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, true);
			Assert.AreEqual(tag.IsClosed, false);
			Assert.AreEqual(tag.Attributes.Count, 0);
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Comment() {
			const string str = "<!-- comment -->";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "!");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, true);
			Assert.AreEqual(tag.Attributes.Count, 1);
			Assert.AreEqual(tag.Attributes["content"], " comment ");
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Empty() {
			const string str = "<div>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, false);
			Assert.AreEqual(tag.Attributes.Count, 0);
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void NonValuedAttribute() {
			const string str = "<iframe y='2' allowfullscreen x='1' foo>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "iframe");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, false);
			Assert.AreEqual(tag.Attributes.Count, 4);
			Assert.AreEqual(tag.Attributes["allowfullscreen"], "");
			Assert.AreEqual(tag.Attributes["foo"], "");
			Assert.AreEqual(tag.Attributes["y"], "2");
			Assert.AreEqual(tag.Attributes["x"], "1");
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Quoted() {
			const string str = "<div x=\"1\" y=\"2\">";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, false);
			Assert.AreEqual(tag.Attributes.Count, 2);
			Assert.AreEqual(tag.Attributes["x"], "1");
			Assert.AreEqual(tag.Attributes["y"], "2");
			Assert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Unquoted() {
			const string str = "<div x=1 y=2>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			Assert.AreEqual(tag.Name, "div");
			Assert.AreEqual(tag.IsClosing, false);
			Assert.AreEqual(tag.IsClosed, false);
			Assert.AreEqual(tag.Attributes.Count, 2);
			Assert.AreEqual(tag.Attributes["x"], "1");
			Assert.AreEqual(tag.Attributes["y"], "2");
			Assert.AreEqual(_Pos, str.Length);
		}
	}
}