using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {

	[TestFixture]
	public class XssAttackTests {
		public bool IsTagReallySafe(HtmlTag tag) {
			switch (tag.Name) {
				case "B":
				case "UL":
				case "LI":
				case "I":
					return tag.Attributes.Count == 0;

				case "A":
				case "a":
					return tag.IsClosing && tag.Attributes.Count == 0;
			}

			return false;
		}


		public static IEnumerable<TestCaseData> GetTestsFromFile(string filename) {
			string tests = Utils.LoadTextResource("MarkdownDeepTests.testfiles.xsstests." + filename);

			// Split into lines
			string[] lines = tests.Replace("\r\n", "\n").Split('\n');

			// Join bac
			var strings = new List<string>();
			string str = null;
			foreach (string l in lines) {
				// Ignore
				if (l.StartsWith("////")) {
					continue;
				}

				// Terminator?
				if (l == "====== UNTESTED ======") {
					str = null;
					break;
				}

				// Blank line?
				if (String.IsNullOrEmpty(l.Trim())) {
					if (str != null) {
						strings.Add(str);
					}
					str = null;

					continue;
				}

				if (str == null) {
					str = l;
				} else {
					str = str + "\n" + l;
				}
			}

			if (str != null) {
				strings.Add(str);
			}


			return from s in strings select new TestCaseData(s);
		}

		public static IEnumerable<TestCaseData> GetAttacks() => GetTestsFromFile("xss_attacks.txt");


		public static IEnumerable<TestCaseData> GetAllowed() => GetTestsFromFile("non_attacks.txt");

		[Test, TestCaseSource(nameof(GetAttacks))]
		public void TestAttacksAreBlocked(string input) {
			var scanner = new StringScanner(input);
			while (!scanner.Eof) {
				HtmlTag tag = scanner.ParseHtml();
				if (tag == null) { // Next character
					scanner.SkipForward(1);
				} else {
					if (tag.IsSafe()) {
                        // There's a few tags that really are safe in the test data
                        ClassicAssert.IsTrue(IsTagReallySafe(tag));
					}
				}
			}
		}


		[Test, TestCaseSource(nameof(GetAllowed))]
		public void TestNonAttacksAreAllowed(string input) {
			var scanner = new StringScanner(input);

			while (!scanner.Eof) {
				HtmlTag tag = scanner.ParseHtml();
				if (tag != null) {
					ClassicAssert.IsTrue(tag.IsSafe());
				} else {
					// Next character
					scanner.SkipForward(1);
				}
			}
		}
	}
}