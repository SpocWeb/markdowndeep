namespace MarkdownDeep {

	/// <summary> Footnote declaration Data; transforms into a small numbered Link </summary>
	internal class FootnoteReference {

		public string Id;
		public int Index;

		public FootnoteReference(int index, string id) {
			Index = index;
			Id = id;
		}
	}
}