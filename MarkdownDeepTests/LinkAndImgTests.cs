using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class LinkAndImgTests {
		[SetUp]
		public void SetUp() {
			m = new Markdown();
			m.AddLinkDefinition(new LinkDefinition("link1", "url.com", "title"));
			m.AddLinkDefinition(new LinkDefinition("link2", "url.com"));
			m.AddLinkDefinition(new LinkDefinition("img1", "url.com/image.png", "title"));
			m.AddLinkDefinition(new LinkDefinition("img2", "url.com/image.png"));

			s = new SpanFormatter(m);
		}

		Markdown m;
		SpanFormatter s;

		[Test]
		public void Boundaries() {
			ClassicAssert.AreEqual("<a href=\"url.com\">link text</a>",
				s.Format("[link text](url.com)"));
			ClassicAssert.AreEqual("<a href=\"url.com\" title=\"title\">link text</a>",
				s.Format("[link text][link1]"));
		}

		[Test]
		public void ImageLink() => ClassicAssert.AreEqual("pre <a href=\"url.com\"><img src=\"url.com/image.png\" alt=\"alt text\" /></a> post",
				s.Format("pre [![alt text](url.com/image.png)](url.com) post"));

		[Test]
		public void ImplicitReferenceImgWithTitle() {
			ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"img1\" title=\"title\" /> post",
				s.Format("pre ![img1] post"));
			ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"img1\" title=\"title\" /> post",
				s.Format("pre ![img1][] post"));
		}

		[Test]
		public void ImplicitReferenceImgWithoutTitle() {
			ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"img2\" /> post",
				s.Format("pre ![img2] post"));
			ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"img2\" /> post",
				s.Format("pre ![img2][] post"));
		}

		[Test]
		public void ImplicitReferenceLinkWithTitle() {
			ClassicAssert.AreEqual("pre <a href=\"url.com\" title=\"title\">link1</a> post",
				s.Format("pre [link1] post"));
			ClassicAssert.AreEqual("pre <a href=\"url.com\" title=\"title\">link1</a> post",
				s.Format("pre [link1][] post"));
		}

		[Test]
		public void ImplicitReferenceLinkWithoutTitle() {
			ClassicAssert.AreEqual("pre <a href=\"url.com\">link2</a> post",
				s.Format("pre [link2] post"));
			ClassicAssert.AreEqual("pre <a href=\"url.com\">link2</a> post",
				s.Format("pre [link2][] post"));
		}

		[Test]
		public void InlineImgWithTitle() => ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"alt text\" title=\"title\" /> post",
				s.Format("pre ![alt text](url.com/image.png \"title\") post"));

		[Test]
		public void InlineImgWithoutTitle() => ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"alt text\" /> post",
				s.Format("pre ![alt text](url.com/image.png) post"));

		[Test]
		public void InlineLinkWithTitle() => ClassicAssert.AreEqual("pre <a href=\"url.com\" title=\"title\">link text</a> post",
				s.Format("pre [link text](url.com \"title\") post"));

		[Test]
		public void InlineLinkWithoutTitle() => ClassicAssert.AreEqual("pre <a href=\"url.com\">link text</a> post",
				s.Format("pre [link text](url.com) post"));

		[Test]
		public void MissingReferenceImg() => ClassicAssert.AreEqual("pre ![alt text][missing] post",
				s.Format("pre ![alt text][missing] post"));

		[Test]
		public void MissingReferenceLink() => ClassicAssert.AreEqual("pre [link text][missing] post",
				s.Format("pre [link text][missing] post"));


		[Test]
		public void ReferenceImgWithTitle() => ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"alt text\" title=\"title\" /> post",
				s.Format("pre ![alt text][img1] post"));

		[Test]
		public void ReferenceImgWithoutTitle() => ClassicAssert.AreEqual("pre <img src=\"url.com/image.png\" alt=\"alt text\" /> post",
				s.Format("pre ![alt text][img2] post"));

		[Test]
		public void ReferenceLinkIdsAreCaseInsensitive() => ClassicAssert.AreEqual("pre <a href=\"url.com\" title=\"title\">link text</a> post",
				s.Format("pre [link text][LINK1] post"));

		[Test]
		public void ReferenceLinkWithTitle() => ClassicAssert.AreEqual("pre <a href=\"url.com\" title=\"title\">link text</a> post",
				s.Format("pre [link text][link1] post"));

		[Test]
		public void ReferenceLinkWithoutTitle() => ClassicAssert.AreEqual("pre <a href=\"url.com\">link text</a> post",
				s.Format("pre [link text][link2] post"));
	}
}