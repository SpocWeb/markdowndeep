using MarkdownDeep;
using NUnit.Framework;

namespace MarkdownDeepTests {

	[TestFixture]
	public class AutoHeaderIDTests {

		[SetUp]
		public void SetUp() => _Md = new Markdown { GenerateHeadingIDs = true, IsExtraMode = true };

		Markdown _Md;

		[Test]
		public void Duplicates() {
			Assert.AreEqual(@"heading",
				_Md.MakeUniqueHeaderId(@"heading"));
			Assert.AreEqual(@"heading-1",
				_Md.MakeUniqueHeaderId(@"heading"));
			Assert.AreEqual(@"heading-2",
				_Md.MakeUniqueHeaderId(@"heading"));
		}

		[Test]
		public void RevertToSection() => Assert.AreEqual(@"section",
				_Md.MakeUniqueHeaderId(@"!!!"));

		/* Tests for pandoc style header ID generation */
		/* Tests are based on the examples in the pandoc documentation */

		[Test]
		public void Simple() => Assert.AreEqual(@"header-identifiers-in-html",
				_Md.MakeUniqueHeaderId(@"Header identifiers in HTML"));

		[Test]
		public void WithLeadingNumbers() => Assert.AreEqual(@"applications",
				_Md.MakeUniqueHeaderId(@"3. Applications"));

		[Test]
		public void WithLinks() => Assert.AreEqual(@"html-s5-rtf",
				_Md.MakeUniqueHeaderId(@"[HTML](#html), [S5](#S5), [RTF](#rtf)"));

		[Test]
		public void WithPunctuation() => Assert.AreEqual(@"dogs--in-my-house",
				_Md.MakeUniqueHeaderId(@"Dogs?--in *my* house?"));
	}
}