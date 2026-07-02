namespace MarkdownDeep {

	/// <summary> Footnote declaration Data; transforms into a small numbered Link </summary>
	///
	/// <example>
	/// <code language="yaml">
	/// pass: 2
	/// mtime: 2023-06-25T16:41:12Z
	/// digest: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
	/// </code>
	/// </example>
	internal class FootnoteReference {

		public string Id;
		public int Index;

		public FootnoteReference(int index, string id) {
			Index = index;
			Id = id;
		}
	}
}