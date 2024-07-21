using System;
using System.Collections.Generic;
using System.Text;

namespace MarkdownDeep {

	/// <summary> a HTML Element Tag, including name, attributes, closing tag info etc...  </summary>
	public class HtmlTag {

		public HtmlTag(string name) {
			Name = name;
		}

		/// <summary> The Element/Tag name eg: "div" </summary>
		public readonly string Name;

		/// <summary> Flag whether this Element directly closes again  eg; <br /></summary>
		public bool IsClosed;

		/// <summary> Flag whether this is a (separate) closing Tag  eg: {/div} </summary>
		public bool IsClosing;
		
		HtmlTagFlags _Flags = 0;

		// Get a dictionary of attribute values (no decoding done)
		public readonly Dictionary<string, string> Attributes  =
			new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

		public void AddAttribute(string key, string value) => Attributes.Add(key, value);

		#region Flags

		public HtmlTagFlags Flags {
			get {
				if (_Flags != 0) {
					return _Flags;
				}
				if (!_TAG_FLAGS.TryGetValue(Name.ToLower(), out _Flags)) {
					_Flags |= HtmlTagFlags.Inline;
				}
				return _Flags;
			}
		}

		static readonly Dictionary<string, HtmlTagFlags> _TAG_FLAGS = new Dictionary<string, HtmlTagFlags> {
			{"p", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"div", HtmlTagFlags.Block},
			{"h1", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"h2", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"h3", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"h4", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"h5", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"h6", HtmlTagFlags.Block | HtmlTagFlags.ContentAsSpan},
			{"blockquote", HtmlTagFlags.Block},
			{"pre", HtmlTagFlags.Block},
			{"table", HtmlTagFlags.Block},
			{"dl", HtmlTagFlags.Block},
			{"ol", HtmlTagFlags.Block},
			{"ul", HtmlTagFlags.Block},
			{"form", HtmlTagFlags.Block},
			{"fieldset", HtmlTagFlags.Block},
			{"iframe", HtmlTagFlags.Block},
			{"script", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"noscript", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"math", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"ins", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"del", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"img", HtmlTagFlags.Block | HtmlTagFlags.Inline},
			{"li", HtmlTagFlags.ContentAsSpan},
			{"dd", HtmlTagFlags.ContentAsSpan},
			{"dt", HtmlTagFlags.ContentAsSpan},
			{"td", HtmlTagFlags.ContentAsSpan},
			{"th", HtmlTagFlags.ContentAsSpan},
			{"legend", HtmlTagFlags.ContentAsSpan},
			{"address", HtmlTagFlags.ContentAsSpan},
			{"hr", HtmlTagFlags.Block | HtmlTagFlags.NoClosing},
			{"!", HtmlTagFlags.Block | HtmlTagFlags.NoClosing},
			{"head", HtmlTagFlags.Block},
		};

		#endregion Flags

		#region safe HTML

		/// <summary> Safe HTML Elements </summary>
		static readonly string[] _ALLOWED_TAGS = {
			"b", "blockquote", "code", "dd", "dt", "dl", "del", "em", "h1", "h2", "h3", "h4", "h5", "h6", "i", "kbd", "li", "ol",
			"ul",
			"p", "pre", "s", "sub", "sup", "strong", "strike", "img", "a"
		};

		/// <summary> Safe Attributes by HTML Element </summary>
		static readonly Dictionary<string, string[]> _ALLOWED_ATTRIBUTES = new Dictionary<string, string[]> {
			{"a", new[] {"href", "title", "class"}},
			{"img", new[] {"src", "width", "height", "alt", "title", "class"}},
		};

		// Check if this tag is safe
		public bool IsSafe() {
			string name_lower = Name.ToLowerInvariant();

			// Check if tag is in whitelist
			if (!Utils.IsInList(name_lower, _ALLOWED_TAGS)) {
				return false;
			}

			// Find allowed attributes
			if (!_ALLOWED_ATTRIBUTES.TryGetValue(name_lower, out var allowed_attributes)) {
				// No allowed attributes, check we don't have any
				return Attributes.Count == 0;
			}

			// Check all are allowed
			foreach (var i in Attributes) {
				if (!Utils.IsInList(i.Key.ToLowerInvariant(), allowed_attributes)) {
					return false;
				}
			}

			// Check href attribute is ok
			if (Attributes.TryGetValue("href", out var href)) {
				if (!Utils.IsSafeUrl(href)) {
					return false;
				}
			}

			if (Attributes.TryGetValue("src", out var src)) {
				if (!Utils.IsSafeUrl(src)) {
					return false;
				}
			}


			// Passed all white list checks, allow it
			return true;
		}

		#endregion safe HTML

		#region Rendering

		// Render opening tag (eg: <tag attr="value">
		public void RenderOpening(StringBuilder dest) {
			dest.Append("<");
			dest.Append(Name);
			foreach (var i in Attributes) {
				dest.Append(" ");
				dest.Append(i.Key);
				dest.Append("=\"");
				dest.Append(i.Value);
				dest.Append("\"");
			}

			if (IsClosed) {
				dest.Append(" />");
			} else {
				dest.Append(">");
			}
		}

		// Render closing tag (eg: </tag>)
		public void RenderClosing(StringBuilder dest) {
			dest.Append("</");
			dest.Append(Name);
			dest.Append(">");
		}

		#endregion Rendering

	}
}