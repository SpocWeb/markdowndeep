using System.Reflection;
using System.Text;
using MarkdownDeep;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace MarkdownDeepTests {

	public static class Utils {

		public static IEnumerable<TestCaseData> GetTests(string foldername) {
			string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

			return from name in names
				where
					name.StartsWith("MarkdownDeepTests.testfiles." + foldername + ".") &&
					(name.EndsWith(".txt") || name.EndsWith(".text"))
				select new TestCaseData(name);
		}

		public static string LoadTextResource(string name) {
			// get a reference to the current assembly
			Assembly a = Assembly.GetExecutingAssembly();
			var r = new StreamReader(a.GetManifestResourceStream(name));
			string str = r.ReadToEnd();
			r.Close();

			return str;
		}


		public static string strip_redundant_whitespace(string str) {
			var sb = new StringBuilder();

			str = str.Replace("\r\n", "\n");

			int i = 0;
			while (i < str.Length) {
				char ch = str[i];
				switch (ch) {
					case ' ':
					case '\t':
					case '\r':
					case '\n':
						// Store start of white space
						i++;

						// Find end of whitespace
						while (i < str.Length) {
							ch = str[i];
							if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') {
								break;
							}

							i++;
						}


						// Replace with a single space
						if (i < str.Length && str[i] != '<') {
							sb.Append(' ');
						}

						break;

					case '>':
						sb.Append("> ");
						i++;
						while (i < str.Length) {
							ch = str[i];
							if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') {
								break;
							}

							i++;
						}
						break;

					case '<':
						if (i + 5 < str.Length && str.Substring(i, 5) == "<pre>") {
							sb.Append(" ");

							// Special handling for pre blocks

							// Find end
							int end = str.IndexOf("</pre>", i, StringComparison.Ordinal);
							if (end < 0) {
								end = str.Length;
							}

							// Append the pre block
							sb.Append(str, i, end - i);
							sb.Append(" ");

							// Jump to end
							i = end;
						} else {
							sb.Append(" <");
							i++;
						}
						break;

					default:
						sb.Append(ch);
						i++;
						break;
				}
			}

			return sb.ToString().Trim();
		}

		public static void RunResourceTest(string resourceName) {
			string input = LoadTextResource(resourceName);
			string expected = LoadTextResource(Path.ChangeExtension(resourceName, "html"));

			var md = new Markdown
            {
                IsSafeMode = resourceName.IndexOf("(SafeMode)", StringComparison.Ordinal) >= 0,
                IsExtraMode = resourceName.IndexOf("(ExtraMode)", StringComparison.Ordinal) >= 0,
                AllowMarkdownInHtmlBlock = resourceName.IndexOf("(MarkdownInHtml)", StringComparison.Ordinal) >= 0,
                GenerateHeadingIDs = resourceName.IndexOf("(AutoHeadingIDs)", StringComparison.Ordinal) >= 0
            };

            string actual = md.AsHtml(input);
			string actual_clean = strip_redundant_whitespace(actual);
			string expected_clean = strip_redundant_whitespace(expected);

			string sep = new string('-', 30) + "\n";

			Console.WriteLine("Input:\n" + sep + input);
			Console.WriteLine("Actual:\n" + sep + actual);
			Console.WriteLine("Expected:\n" + sep + expected);

			ClassicAssert.AreEqual(expected_clean, actual_clean);
		}

        /*public static string TransformUsingJS(string inputText, bool SafeMode, bool ExtraMode, bool MarkdownInHtml,
			bool AutoHeadingIDs) {
			// Find test page
			string url = Assembly.GetExecutingAssembly().CodeBase;
			url = Path.GetDirectoryName(url);
			url = url.Replace("file:\\", "file:\\\\");
			url = url.Replace("\\", "/");
			url = url + "/JSTestResources/JSHost.html";

			// Create browser, navigate and wait
			var browser = new WebBrowser();
			browser.Navigate(url);
			browser.ScriptErrorsSuppressed = true;

			while (browser.ReadyState != WebBrowserReadyState.Complete) {
				Application.DoEvents();
			}

			object o = browser.Document.InvokeScript("transform",
				new object[] {inputText, SafeMode, ExtraMode, MarkdownInHtml, AutoHeadingIDs});

			var result = o as string;

			// Clean up
			browser.Dispose();

			return result;
		}

		public static void RunTestJS(string input, bool SafeMode, bool ExtraMode, bool MarkdownInHtml, bool AutoHeadingIDs) {
			string normalized_input = input.Replace("\r\n", "\n").Replace("\r", "\n");

			// Work out the expected output using C# implementation
			var md = new Markdown();
			md.IsSafeMode = SafeMode;
			md.IsExtraMode = ExtraMode;
			md.AllowMarkdownInHtmlBlock = MarkdownInHtml;
			md.GenerateHeadingIDs = AutoHeadingIDs;
			string expected = md.AsHtml(normalized_input);

			// Transform using javascript implementation
			string actual = TransformUsingJS(input, SafeMode, ExtraMode, MarkdownInHtml, AutoHeadingIDs);

			actual = actual.Replace("\r", "");
			expected = expected.Replace("\r", "");

			string sep = new string('-', 30) + "\n";

			Console.WriteLine("Input:\n" + sep + input);
			Console.WriteLine("Actual:\n" + sep + actual);
			Console.WriteLine("Expected:\n" + sep + expected);

			// Check it
			ClassicAssert.AreEqual(expected, actual);
		}

		public static void RunResourceTestJs(string resourceName) {
			bool safeMode = resourceName.IndexOf("(SafeMode)") >= 0;
			bool extraMode = resourceName.IndexOf("(ExtraMode)") >= 0;
			bool markdownInHtml = resourceName.IndexOf("(MarkdownInHtml)") >= 0;
			bool autoHeadingIDs = resourceName.IndexOf("(AutoHeadingIDs)") >= 0;

			// Get the input script
			string input = LoadTextResource(resourceName);
			RunTestJS(input, safeMode, extraMode, markdownInHtml, autoHeadingIDs);
		}*/
    }
}