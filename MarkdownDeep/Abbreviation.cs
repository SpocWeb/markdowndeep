namespace MarkdownDeep {

	/// <summary> Abbreviation declaration Data, transforms into a ToolTip. </summary>
	internal class Abbreviation {
		public string Abbr;
		public string Title;

		public Abbreviation(string abbr, string title) {
			Abbr = abbr;
			Title = title;
		}
	}
}