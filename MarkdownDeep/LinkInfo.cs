namespace MarkdownDeep {

	/// <summary> link text and a reference to the associated <see cref="LinkDefinition"/> </summary>
	///
	/// <example>
	/// <code language="yaml">
	/// pass: 2
	/// mtime: 2023-06-25T16:41:12Z
	/// digest: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
	/// </code>
	/// </example>
	internal class LinkInfo {
		public LinkDefinition _Def;
		public string _LinkText;

		public LinkInfo(LinkDefinition def, string linkText) {
			_Def = def;
			_LinkText = linkText;
		}
	}
}