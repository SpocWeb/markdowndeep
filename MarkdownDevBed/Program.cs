using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MarkdownDeep;

namespace MarkdownDevBed {

	class Program {

		public static Regex _RxExtractLanguage = new Regex("^({{(.+)}}[\r\n])", RegexOptions.Compiled);

		/// <summary>returns the root path of the currently executing assembly</summary>
		static string ExecutingAssemblyPath {
			get {
				string path = Assembly.GetExecutingAssembly().Location;
				// removes executable part
				path = Path.GetDirectoryName(path);
				// we're typically in \bin\debug or bin\release so move up two folders
// ReSharper disable once AssignNullToNotNullAttribute
				path = Path.Combine(path, "..");
				path = Path.Combine(path, "..");
				return path;
			}
		}

		public static string FormatCodePrettyPrint(Markdown m, string code) {
			Match match = _RxExtractLanguage.Match(code);
			string language = null;

			if (match.Success) {
				// Save the language
				Group g = match.Groups[2];
				language = g.ToString();

				// Remove the first line
				code = code.Substring(match.Groups[1].Length);
			}


			if (language == null) {
				LinkDefinition d = m.GetLinkDefinition("default_syntax");
				if (d != null) {
					language = d.Title;
				}
			}
			if (language == "C#") {
				language = "csharp";
			}
			if (language == "C++") {
				language = "cpp";
			}

			if (string.IsNullOrEmpty(language)) {
				return string.Format("<pre><code>{0}</code></pre>\n", code);
			}
			return string.Format("<pre class=\"prettyprint lang-{0}\"><code>{1}</code></pre>\n", language.ToLowerInvariant(),
				code);
		}


		static void Main(string[] args) {
			var m = new Markdown {
				IsSafeMode = false,
				IsExtraMode = true,
				GenerateHeadingIDs = true,
				FormatCodeBlock = FormatCodePrettyPrint,
				DoExtractHeadBlocks = true,
				AllowUserBreaks = true,

				//SectionHeader = "<div class=\"header\">{0}</div>\n",
				//SectionHeader = "\n<div class=\"section_links\"><a href=\"/edit?section={0}\">Edit</a></div>\n";
				//SectionHeadingSuffix = "<div class=\"heading\">{0}</div>\n",
				//SectionFooter = "<div class=\"footer\">{0}</div>\n\n",
				//HtmlClassTitledImages = "figure",
				//DocumentRoot = "C:\\users\\bradr\\desktop",
				//DocumentLocation = "C:\\users\\bradr\\desktop\\100D5000",
				//MaxImageWidth = 500
			};

			string markdown = FileContents("input.txt");
			string str = m.AsHtml(markdown);
			Console.Write(str);

			List<string> sections = XMarkdown.SplitUserSections(markdown);
			for (int i = 0; i < sections.Count; i++) {
				Console.WriteLine("---- Section {0} ----", i);
				Console.Write(sections[i]);
				Console.Write("\n");
			}
			Console.WriteLine("------------------");

			Console.WriteLine("------Joined-------");
			Console.WriteLine(XMarkdown.JoinUserSections(sections));
			Console.WriteLine("------Joined-------");

			Console.WriteLine("------start head block-------");
			Console.WriteLine(m.HeadBlockContent);
			Console.WriteLine("------end head block-------");
		}


		/// <summary>returns the contents of the specified file as a string assumes the file is relative to the root of the project</summary>
		static string FileContents(string filename) {
			try {
				return File.ReadAllText(Path.Combine(ExecutingAssemblyPath, filename));
			} catch (FileNotFoundException) {
				return "";
			}
		}
	}
}