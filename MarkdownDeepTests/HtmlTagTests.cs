using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, true);
			ClassicAssert.AreEqual(tag.Attributes.Count, 0);
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void ClosedWithAttribs() {
			const string str = "<div x=1 y=2/>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, true);
			ClassicAssert.AreEqual(tag.Attributes.Count, 2);
			ClassicAssert.AreEqual(tag.Attributes["x"], "1");
			ClassicAssert.AreEqual(tag.Attributes["y"], "2");
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Closing() {
			const string str = "</div>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, true);
			ClassicAssert.AreEqual(tag.IsClosed, false);
			ClassicAssert.AreEqual(tag.Attributes.Count, 0);
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Comment() {
			const string str = "<!-- comment -->";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "!");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, true);
			ClassicAssert.AreEqual(tag.Attributes.Count, 1);
			ClassicAssert.AreEqual(tag.Attributes["content"], " comment ");
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Empty() {
			const string str = "<div>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, false);
			ClassicAssert.AreEqual(tag.Attributes.Count, 0);
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void NonValuedAttribute() {
			const string str = "<iframe y='2' allowfullscreen x='1' foo>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "iframe");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, false);
			ClassicAssert.AreEqual(tag.Attributes.Count, 4);
			ClassicAssert.AreEqual(tag.Attributes["allowfullscreen"], "");
			ClassicAssert.AreEqual(tag.Attributes["foo"], "");
			ClassicAssert.AreEqual(tag.Attributes["y"], "2");
			ClassicAssert.AreEqual(tag.Attributes["x"], "1");
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Quoted() {
			const string str = "<div x=\"1\" y=\"2\">";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, false);
			ClassicAssert.AreEqual(tag.Attributes.Count, 2);
			ClassicAssert.AreEqual(tag.Attributes["x"], "1");
			ClassicAssert.AreEqual(tag.Attributes["y"], "2");
			ClassicAssert.AreEqual(_Pos, str.Length);
		}

		[Test]
		public void Unquoted() {
			const string str = "<div x=1 y=2>";
			HtmlTag tag = str.ParseHtml(ref _Pos);

			ClassicAssert.AreEqual(tag.Name, "div");
			ClassicAssert.AreEqual(tag.IsClosing, false);
			ClassicAssert.AreEqual(tag.IsClosed, false);
			ClassicAssert.AreEqual(tag.Attributes.Count, 2);
			ClassicAssert.AreEqual(tag.Attributes["x"], "1");
			ClassicAssert.AreEqual(tag.Attributes["y"], "2");
			ClassicAssert.AreEqual(_Pos, str.Length);
		}
	}
}