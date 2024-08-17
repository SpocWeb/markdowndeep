using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class StringScannerTests {
		[Test]
		public void Tests() {
			var p = new StringScanner();

			p.Reset("This is a string with something [bracketed]");
			ClassicAssert.IsTrue(p.Bof);
			ClassicAssert.IsFalse(p.Eof);
			ClassicAssert.IsTrue(p.SkipString("This"));
			ClassicAssert.IsFalse(p.Bof);
			ClassicAssert.IsFalse(p.Eof);
			ClassicAssert.IsFalse(p.SkipString("huh?"));
			ClassicAssert.IsTrue(p.SkipLinespace());
			ClassicAssert.IsTrue(p.SkipChar('i'));
			ClassicAssert.IsTrue(p.SkipChar('s'));
			ClassicAssert.IsTrue(p.SkipWhitespace());
			ClassicAssert.IsTrue(p.DoesMatchAny(new[] {'r', 'a', 't'}));
			ClassicAssert.IsFalse(p.Find("Not here"));
			ClassicAssert.IsFalse(p.Find("WITH"));
			ClassicAssert.IsFalse(p.FindI("Not here"));
			ClassicAssert.IsTrue(p.FindI("WITH"));
            ClassicAssert.IsTrue(p.Find('['));
			p.SkipForward(1);
			p.Mark();
			ClassicAssert.IsTrue(p.Find(']'));
			ClassicAssert.AreEqual("bracketed", p.Extract());
			ClassicAssert.IsTrue(p.SkipChar(']'));
            ClassicAssert.IsTrue(p.Eof);
		}
	}
}