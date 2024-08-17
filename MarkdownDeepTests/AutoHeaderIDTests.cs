using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {

	[TestFixture]
	public class AutoHeaderIDTests {

		[SetUp]
		public void SetUp() => _Md = new Markdown { GenerateHeadingIDs = true, IsExtraMode = true };

		Markdown _Md;

		[Test]
		public void Duplicates() {
			ClassicAssert.AreEqual(@"heading"  , _Md.MakeUniqueHeaderId(@"heading"));
			ClassicAssert.AreEqual(@"heading-1", _Md.MakeUniqueHeaderId(@"heading"));
			ClassicAssert.AreEqual(@"heading-2", _Md.MakeUniqueHeaderId(@"heading"));
		}

		[Test]
		public void RevertToSection() => ClassicAssert.AreEqual(@"section",
				_Md.MakeUniqueHeaderId(@"!!!"));

		/* Tests for pandoc style header ID generation */
		/* Tests are based on the examples in the pandoc documentation */

		[Test]
		public void Simple() => ClassicAssert.AreEqual(@"header-identifiers-in-html",
				_Md.MakeUniqueHeaderId(@"Header identifiers in HTML"));

		[Test]
		public void WithLeadingNumbers() => ClassicAssert.AreEqual(@"applications",
				_Md.MakeUniqueHeaderId(@"3. Applications"));

		[Test]
		public void WithLinks() => ClassicAssert.AreEqual(@"html-s5-rtf",
				_Md.MakeUniqueHeaderId(@"[HTML](#html), [S5](#S5), [RTF](#rtf)"));

		[Test]
		public void WithPunctuation() => ClassicAssert.AreEqual(@"dogs--in-my-house",
				_Md.MakeUniqueHeaderId(@"Dogs?--in *my* house?"));
	}
}