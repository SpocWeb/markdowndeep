using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	public class EscapeCharacterTests {
		[SetUp]
		public void SetUp() => f = new SpanFormatter(new Markdown());

		SpanFormatter f;

		[Test]
		public void AllEscapeCharacters() => ClassicAssert.AreEqual(@"pre \ ` * _ { } [ ] ( ) # + - . ! post",
				f.Format(@"pre \\ \` \* \_ \{ \} \[ \] \( \) \# \+ \- \. \! post"));

		[Test]
		public void BackslashWithGT() => ClassicAssert.AreEqual(@"backslash with \&gt; greater",
				f.Format(@"backslash with \\> greater"));

		[Test]
		public void BackslashWithTwoDashes() => ClassicAssert.AreEqual(@"backslash with \-- two dashes",
				f.Format(@"backslash with \\-- two dashes"));

		[Test]
		public void EscapeNotALink() => ClassicAssert.AreEqual(@"\[test](not a link)",
				f.Format(@"\\\[test](not a link)"));

		[Test]
		public void NoEmphasis() => ClassicAssert.AreEqual(@"\*no emphasis*",
				f.Format(@"\\\*no emphasis*"));

		[Test]
		public void SomeNonEscapableCharacters() => ClassicAssert.AreEqual(@"pre \q \% \? post",
				f.Format(@"pre \q \% \? post"));
	}
}