using System.Collections.Generic;
using NUnit.Framework;

namespace MarkdownDeepTests {
	[TestFixture]
	internal class SafeModeTests {

		public static IEnumerable<TestCaseData> GetTests() => Utils.GetTests("safemode");

		[Test, TestCaseSource(nameof(GetTests))]
		public void Test(string resourceName) => Utils.RunResourceTest(resourceName);

		//[Test, TestCaseSource(nameof(GetTests))]
		//public void Test_js(string resourceName) => Utils.RunResourceTestJs(resourceName);

	}
}