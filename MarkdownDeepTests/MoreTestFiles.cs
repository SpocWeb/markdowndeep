using System.Collections.Generic;
using NUnit.Framework;

namespace MarkdownDeepTests {

	[TestFixture]
	internal class MoreTestFiles {
		public static IEnumerable<TestCaseData> GetTests_mdtest11() => Utils.GetTests("mdtest11");


		public static IEnumerable<TestCaseData> GetTests_mdtest01() => Utils.GetTests("mdtest01");

		public static IEnumerable<TestCaseData> GetTests_phpmarkdown() => Utils.GetTests("phpmarkdown");


		[Test, TestCaseSource(nameof(GetTests_mdtest01))]
		public void Test_mdtest01(string resourceName) => Utils.RunResourceTest(resourceName);

		//[Test, TestCaseSource(nameof(GetTests_mdtest01))]
		//public void Test_mdtest01_js(string resourceName) => Utils.RunResourceTestJs(resourceName);

		[Test, TestCaseSource(nameof(GetTests_mdtest11))]
		public void Test_mdtest11(string resourceName) => Utils.RunResourceTest(resourceName);

		//[Test, TestCaseSource(nameof(GetTests_mdtest11))]
		//public void Test_mdtest11_js(string resourceName) => Utils.RunResourceTestJs(resourceName);

		/*
		 * Don't run the pandoc test's as they're basically a demonstration of things
		 * that are broken in markdown.
		 * 
		public static IEnumerable<TestCaseData> GetTests_pandoc()
		{
			return Utils.GetTests("pandoc");
		}


		[Test, TestCaseSource("GetTests_pandoc")]
		public void Test_pandoc(string resourceName)
		{
			Utils.RunResourceTest(resourceName);
		}
		 */


		[Test, TestCaseSource(nameof(GetTests_phpmarkdown))]
		public void Test_phpmarkdown(string resourceName) => Utils.RunResourceTest(resourceName);

		//[Test, TestCaseSource(nameof(GetTests_phpmarkdown))]
		//public void Test_phpmarkdown_js(string resourceName) {
		//	// Fake success for randomized link can't ever match
		//	if (resourceName.EndsWith("Email auto links.text")) {
		//		return;
		//	}

		//	Utils.RunResourceTestJs(resourceName);
		//}
	}
}