using MarkdownDeep;
using NUnit.Framework;

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

			Assert.IsNotNull(s);
			Assert.IsFalse(s.LeadingBar);
			Assert.IsFalse(s.TrailingBar);
			Assert.AreEqual(4, s.Columns.Count);
			Assert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			Assert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			Assert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			Assert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}

		[Test]
		public void LeadingTrailingBars() {
			TableSpec s = Parse("|--|:--|--:|:--:|");

			Assert.IsNotNull(s);
			Assert.IsTrue(s.LeadingBar);
			Assert.IsTrue(s.TrailingBar);
			Assert.AreEqual(4, s.Columns.Count);
			Assert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			Assert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			Assert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			Assert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}

		[Test]
		public void Simple() {
			TableSpec s = Parse("--|--");

			Assert.IsNotNull(s);
			Assert.IsFalse(s.LeadingBar);
			Assert.IsFalse(s.TrailingBar);
			Assert.AreEqual(2, s.Columns.Count);
			Assert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			Assert.AreEqual(ColumnAlignment.Na, s.Columns[1]);
		}


		[Test]
		public void Whitespace() {
			TableSpec s = Parse(" | -- | :-- | --: | :--: |  ");

			Assert.IsNotNull(s);
			Assert.IsTrue(s.LeadingBar);
			Assert.IsTrue(s.TrailingBar);
			Assert.AreEqual(4, s.Columns.Count);
			Assert.AreEqual(ColumnAlignment.Na, s.Columns[0]);
			Assert.AreEqual(ColumnAlignment.Left, s.Columns[1]);
			Assert.AreEqual(ColumnAlignment.Right, s.Columns[2]);
			Assert.AreEqual(ColumnAlignment.Center, s.Columns[3]);
		}
	}
}