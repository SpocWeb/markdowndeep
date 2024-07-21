using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;
using MarkdownDeep;
using org.SpocWeb.root.Data.xmls.htmls.xamls;

namespace MarkDownWpfGui {

	/// <summary> MarkDownWindow </summary>
	public partial class MainWindow {

		readonly Markdown _Markdown = new Markdown {
			AllowMarkdownInHtmlBlock = true, 
			//IncludeMarkup = true, 
			IsExtraMode = true
		};

		public MainWindow() {
			InitializeComponent();
			_TxtMarkdown.Text = File.ReadAllText("MarkdownSyntax.md");
			_TxtMarkdown.SelectionStart = _TxtMarkdown.Text.Length;
		}

		void TxtMarkdown_OnTextChanged(object sender = null, TextChangedEventArgs e = null) {
			var html = "<html>" + _Markdown.AsHtml(_TxtMarkdown.Text) + "</html>"; //.Replace("\n", "\r\n");
			var hOffset = _TxtHtml.HorizontalOffset;
			var vOffset = _TxtHtml.VerticalOffset;
			_TxtHtml.Text = html;
			_TxtHtml.ScrollToHorizontalOffset(hOffset);
			_TxtHtml.ScrollToVerticalOffset(vOffset);
		}

		void _TxtHtml_OnTextChanged(object sender = null, TextChangedEventArgs e = null) {
			try {
				string xaml = _TxtHtml.Text.AsXaml(true); 
				var hOffset = _TxtXaml.HorizontalOffset;
				var vOffset = _TxtXaml.VerticalOffset;
				_TxtXaml.Text = xaml;
				_TxtXaml.ScrollToHorizontalOffset(hOffset);
				_TxtXaml.ScrollToVerticalOffset(vOffset);
			} catch (XmlException x) {
				//x.LineNumber;
				//x.LinePosition;
			}
		}

		void _TxtXaml_OnTextChanged(object sender, TextChangedEventArgs e) {
			try {
				var document = XamlReader.Parse(_TxtXaml.Text);
				var flowDocument = document as FlowDocument;
				//if (flowDocument != null)
				//	SubscribeToAllHyperlinks(flowDocument);
				var hOffset = _RichTextBox.HorizontalOffset;
				var vOffset = _RichTextBox.VerticalOffset;
				_RichTextBox.Document = flowDocument;
				_RichTextBox.ScrollToHorizontalOffset(hOffset);
				_RichTextBox.ScrollToVerticalOffset(vOffset);
			} catch (XamlParseException x) {
				//x.LineNumber;
				//x.LinePosition;
			}
		}
	}
}
