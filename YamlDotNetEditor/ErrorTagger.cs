//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) Antoine Aubry and contributors

//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Editor.Lib;

namespace YamlDotNetEditor
{
	internal class ErrorTagger : ITagger<IErrorTag>
	{
		private readonly ITextBuffer _textBuffer;
		private readonly TextBufferParser _parser;
		private readonly Dictionary<string, int> _anchors = new Dictionary<string, int>();

		public ErrorTagger(ITextBuffer textBuffer, TextBufferParser parser)
		{
			_textBuffer = textBuffer;
			_parser = parser;
			_parser.ParseTreeChanged += (s, e) => { Reparse(); OnTagsChanged(e); };
			Reparse();
		}

		private void Reparse()
		{
			_anchors.Clear();

			foreach (var token in _parser.GetAllTokens().OfType<Anchor>())
			{
				int count;
				_anchors.TryGetValue(token.Value, out count);
				_anchors[token.Value] = count + 1;
			}
		}

		public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var alreadyReportedSyntaxErrors = new HashSet<string>();

			foreach (var span in spans)
			{
				foreach (var token in _parser.GetTokensBetween(span.Start.Position, span.End.Position))
				{
					var syntaxError = token as SyntaxErrorToken;
					if (syntaxError != null && alreadyReportedSyntaxErrors.Add(syntaxError.Exception.Message))
					{
						yield return CreateErrorTag(span, token, "syntax error", syntaxError.Exception.Message);
						continue;
					}

					var anchor = token as Anchor;
					if (anchor != null && _anchors[anchor.Value] > 1)
					{
						yield return CreateErrorTag(span, token, "syntax error", "Duplicate anchor");
						continue;
					}

					var anchorAlias = token as AnchorAlias;
					if (anchorAlias != null && !_anchors.ContainsKey(anchorAlias.Value))
					{
						yield return CreateErrorTag(span, token, "syntax error", "Undefined anchor");
						continue;
					}
				}
			}
		}

		private TagSpan<ErrorTag> CreateErrorTag(SnapshotSpan span, Token token, string type, string message)
		{
			var start = Math.Max(token.Start.Index, span.Start.Position);
			var end = Math.Min(token.End.Index, span.End.Position + 1);

			return new TagSpan<ErrorTag>(
				new SnapshotSpan(
					span.Snapshot,
					new Span(start, end - start)
				),
				new ErrorTag(type, message)
			);
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		protected virtual void OnTagsChanged(SnapshotSpanEventArgs e)
		{
			if (TagsChanged != null)
			{
				TagsChanged(this, e);
			}
		}
	}
}
