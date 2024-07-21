namespace MarkdownDeep {

	/// <summary> single element in a tokenized span. Used by the SpanFormatter class to represent the internal structure of a span of text. </summary>
	/// <remarks>
	/// * Token is used to mark out various special parts of a string being formatted by SpanFormatter.
	/// * Strings aren't actually stored in the token - just their offset and length in the input string.
	/// * For performance, Token's are pooled and reused. See SpanFormatter.CreateToken()
	/// </remarks>
	internal class Token {
		// Constructor
		public object? _Data;
		public int _Length;
		public int _StartOffset;
		public TokenType _Type;

		public Token(TokenType type, int startOffset, int length) {
			_Type = type;
			_StartOffset = startOffset;
			_Length = length;
		}

		// Constructor
		public Token(TokenType type, object data) {
			_Type = type;
			_Data = data;
		}

		public override string ToString() => string.Format("{0} - {1} - {2}", _Type, _StartOffset, _Length);//return string.Format("{0} - {1} - {2} -> {3}", type.ToString(), startOffset, length, data.ToString());
	}
}