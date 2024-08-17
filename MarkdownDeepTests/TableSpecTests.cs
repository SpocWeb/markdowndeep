using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {

	[TestFixture]
	internal class TableSpecTests {
		[SetUp]
		public void SetUp() {}

		TableSpec Parse(string str) {
			var s = new StringScanner(str);
			return TableSpec.Parse(s);
		}

		[Test]
		public void Alignment() {
			TableSpec s = Parse("--|:--|--:|:--:");

			ClassicAssert.IsNotNull(s);
			ClassicAssert.IsFalse(s.LeadingBar);
            ClassicAssert.IsFalse(s.TrailingBar);
			ClassicAssert.AreEqual(4, s.Columns.Count);
			ClassicAssert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			ClassicAssert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			ClassicAssert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			ClassicAssert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}

		[Test]
		public void LeadingTrailingBars() {
			TableSpec s = Parse("|--|:--|--:|:--:|");

			ClassicAssert.IsNotNull(s);
			ClassicAssert.IsTrue(s.LeadingBar);
            ClassicAssert.IsTrue(s.TrailingBar);
			ClassicAssert.AreEqual(4, s.Columns.Count);
			ClassicAssert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			ClassicAssert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			ClassicAssert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			ClassicAssert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}

		[Test]
		public void Simple() {
			TableSpec s = Parse("--|--");

			ClassicAssert.IsNotNull(s);
			ClassicAssert.IsFalse(s.LeadingBar);
            ClassicAssert.IsFalse(s.TrailingBar);
			ClassicAssert.AreEqual(2, s.Columns.Count);
			ClassicAssert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			ClassicAssert.AreEqual(ColumnAlignment.Na, s.Columns[1]);
		}


		[Test]
		public void Whitespace() {
			TableSpec s = Parse(" | -- | :-- | --: | :--: |  ");

			ClassicAssert.IsNotNull(s);
			ClassicAssert.IsTrue(s.LeadingBar);
            ClassicAssert.IsTrue(s.TrailingBar);
			ClassicAssert.AreEqual(4, s.Columns.Count);
			ClassicAssert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			ClassicAssert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			ClassicAssert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			ClassicAssert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}
	}
}