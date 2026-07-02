namespace MarkdownDeep {

	/// <summary> Abbreviation declaration Data, transforms into a ToolTip. </summary>
	///
	/// <example>
	/// <code language="yaml">
	/// pass: 2
	/// mtime: 2023-06-25T16:41:12Z
	/// digest: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
	/// </code>
	/// </example>
	internal class Abbreviation {
		public string Abbr;
		public string Title;

		public Abbreviation(string abbr, string title) {
			Abbr = abbr;
			Title = title;
		}
	}
}