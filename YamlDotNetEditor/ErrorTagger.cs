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
using YamlDotNet.Editor.Lib;

namespace YamlDotNetEditor
{
	internal class ErrorTagger : ITagger<IErrorTag>
	{
		private readonly ITextBuffer _textBuffer;
		private readonly TextBufferParser _parser;

		public ErrorTagger(ITextBuffer textBuffer, TextBufferParser parser)
		{
			_textBuffer = textBuffer;
			_parser = parser;
			_parser.ParseTreeChanged += (s, e) => OnTagsChanged(e);
		}

		public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var invalidTokens = spans
				.SelectMany(span => _parser
					.GetTokensBetween(span.Start.Position, span.End.Position)
					.OfType<InvalidToken>()
					.Select(token => new { span, token }))
				.GroupBy(p => new { p.token.Start, p.token.End, p.token.Exception.Message }, (k, t) => t.First());

			foreach (var pair in invalidTokens)
			{
				var start = Math.Max(pair.token.Start.Index, pair.span.Start.Position);
				var end = Math.Min(pair.token.End.Index, pair.span.End.Position + 1);

				yield return new TagSpan<ErrorTag>(
					new SnapshotSpan(
						pair.span.Snapshot,
						new Span(start, end - start)
					),
					new ErrorTag("Syntax Error", pair.token.Exception.Message)
				);
			}
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
