using System;
using System.IO;
using System.Windows.Forms;
using MarkdownDeep;

namespace MarkdownDeepGui {
	public partial class Form1 : Form {
		readonly Markdown _Markdown = new Markdown();

		public Form1() {
			InitializeComponent();
			txtMarkdown.Text = File.ReadAllText("MarkdownSyntax.md");
			//txtMarkdown.Text = @"# Welcome to Markdown #\r\n\r\nType markdown text above, see formatted text below!";
			txtMarkdown.SelectionStart = txtMarkdown.Text.Length;
			_Markdown.IsExtraMode = true;
		}

		void DoUpdate() {
			txtSource.Text = _Markdown.AsHtml(txtMarkdown.Text).Replace("\n", "\r\n");
			webPreview.DocumentText = txtSource.Text;
		}

		void txtMarkdown_TextChanged(object sender, EventArgs e) {
			DoUpdate();
		}


		void checkSafeMode_CheckedChanged(object sender, EventArgs e) {
			_Markdown.IsSafeMode = checkSafeMode.Checked;
			DoUpdate();
		}

		void checkExtraMode_CheckedChanged(object sender, EventArgs e) {
			_Markdown.IsExtraMode = checkExtraMode.Checked;
			DoUpdate();
		}
	}
}