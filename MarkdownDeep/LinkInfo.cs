namespace MarkdownDeep {

	/// <summary> link text and a reference to the associated <see cref="LinkDefinition"/> </summary>
	internal class LinkInfo {
		public LinkDefinition _Def;
		public string _LinkText;

		public LinkInfo(LinkDefinition def, string linkText) {
			_Def = def;
			_LinkText = linkText;
		}
	}
}