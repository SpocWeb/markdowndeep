using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {
	[TestFixture]
	public class EmphasisTests {
		[SetUp]
		public void SetUp() => f = new SpanFormatter(new Markdown());

		SpanFormatter f;

		[Test]
		public void PlainText() => ClassicAssert.AreEqual("This is plain text",
				f.Format("This is plain text"));


		[Test]
		public void combined_1() => ClassicAssert.AreEqual("<strong><em>test test</em></strong>",
				f.Format("***test test***"));


		[Test]
		public void combined_10() => ClassicAssert.AreEqual("<em>test <strong>test</strong></em>",
				f.Format("_test __test___"));


		[Test]
		public void combined_11() => ClassicAssert.AreEqual("<strong>test <em>test</em></strong>",
				f.Format("__test _test___"));


		[Test]
		public void combined_12() => ClassicAssert.AreEqual("<strong><em>test</em> test</strong>",
				f.Format("___test_ test__"));


		[Test]
		public void combined_13() => ClassicAssert.AreEqual("<em><strong>test</strong> test</em>",
				f.Format("___test__ test_"));


		[Test]
		public void combined_14() => ClassicAssert.AreEqual("<strong><em>test</em> test</strong>",
				f.Format("___test_ test__"));


		[Test]
		public void combined_15() => ClassicAssert.AreEqual("<strong>test <em>test</em></strong>",
				f.Format("__test _test___"));


		[Test]
		public void combined_16() => ClassicAssert.AreEqual("<em>test <strong>test</strong></em>",
				f.Format("_test __test___"));

		[Test]
		public void combined_17() {
			var fExtra = new SpanFormatter(new Markdown {IsExtraMode = true});
			ClassicAssert.AreEqual("<strong>Bold</strong> <em>Italic</em>",
				fExtra.Format("__Bold__ _Italic_"));
		}

		[Test]
		public void combined_18() {
			var fExtra = new SpanFormatter(new Markdown {IsExtraMode = true});
			ClassicAssert.AreEqual("<em>Emphasis</em>, trailing",
				fExtra.Format("_Emphasis_, trailing"));
		}

		[Test]
		public void combined_2() => ClassicAssert.AreEqual("<strong><em>test test</em></strong>",
				f.Format("___test test___"));


		[Test]
		public void combined_3() => ClassicAssert.AreEqual("<em>test <strong>test</strong></em>",
				f.Format("*test **test***"));


		[Test]
		public void combined_4() => ClassicAssert.AreEqual("<strong>test <em>test</em></strong>",
				f.Format("**test *test***"));


		[Test]
		public void combined_5() => ClassicAssert.AreEqual("<strong><em>test</em> test</strong>",
				f.Format("***test* test**"));


		[Test]
		public void combined_6() => ClassicAssert.AreEqual("<em><strong>test</strong> test</em>",
				f.Format("***test** test*"));


		[Test]
		public void combined_7() => ClassicAssert.AreEqual("<strong><em>test</em> test</strong>",
				f.Format("***test* test**"));


		[Test]
		public void combined_8() => ClassicAssert.AreEqual("<strong>test <em>test</em></strong>",
				f.Format("**test *test***"));


		[Test]
		public void combined_9() => ClassicAssert.AreEqual("<em>test <strong>test</strong></em>",
				f.Format("*test **test***"));

		[Test]
		public void em_in_word() => ClassicAssert.AreEqual("un<em>frigging</em>believable",
				f.Format("un*frigging*believable"));

		[Test]
		public void em_simple() {
			ClassicAssert.AreEqual("This is <em>em</em> text",
				f.Format("This is *em* text"));
			ClassicAssert.AreEqual("This is <em>em</em> text",
				f.Format("This is _em_ text"));
		}

		[Test]
		public void em_strong_lead_tail() {
			ClassicAssert.AreEqual("<strong>strong</strong>",
				f.Format("__strong__"));
			ClassicAssert.AreEqual("<strong>strong</strong>",
				f.Format("**strong**"));
			ClassicAssert.AreEqual("<em>em</em>",
				f.Format("_em_"));
			ClassicAssert.AreEqual("<em>em</em>",
				f.Format("*em*"));
		}

		[Test]
		public void no_strongem_if_spaces() {
			ClassicAssert.AreEqual("pre * notem *",
				f.Format("pre * notem *"));
			ClassicAssert.AreEqual("pre ** notstrong **",
				f.Format("pre ** notstrong **"));
			ClassicAssert.AreEqual("pre *Apples *Bananas *Oranges",
				f.Format("pre *Apples *Bananas *Oranges"));
		}

		[Test]
		public void strong_in_word() => ClassicAssert.AreEqual("un<strong>frigging</strong>believable",
				f.Format("un**frigging**believable"));

		[Test]
		public void strong_simple() {
			ClassicAssert.AreEqual("This is <strong>strong</strong> text",
				f.Format("This is **strong** text"));
			ClassicAssert.AreEqual("This is <strong>strong</strong> text",
				f.Format("This is __strong__ text"));
		}

		[Test]
		public void strongem() {
			ClassicAssert.AreEqual("<strong><em>strongem</em></strong>",
				f.Format("***strongem***"));
			ClassicAssert.AreEqual("<strong><em>strongem</em></strong>",
				f.Format("___strongem___"));
		}
	}
}