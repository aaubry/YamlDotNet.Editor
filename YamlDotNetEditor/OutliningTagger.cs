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
	internal class OutliningTagger : ITagger<IOutliningRegionTag>
	{
		private readonly ITextBuffer _textBuffer;
		private readonly TextBufferParser _parser;
		private List<Region> _regions;

		public OutliningTagger(ITextBuffer textBuffer, TextBufferParser parser)
		{
			_textBuffer = textBuffer;
			_parser = parser;
			_parser.ParseTreeChanged += (s, e) => { Reparse(); OnTagsChanged(e); };
			Reparse();
		}

		private void Reparse()
		{
			var regionExtractor = new RegionExtractor();
			_regions = regionExtractor.GetRegions(_parser.GetAllTokens()).ToList();
		}

		public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var start = spans[0].Start.Position;
			var end = spans[spans.Count - 1].End.Position;

			foreach (var region in _regions)
			{
				if (region.Start.Index <= end && region.End.Index >= start)
				{
					var endLine = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(region.End.Line - 2 /* GetLineFromLineNumber is zero-based, and we want to subtract 1 from the current line, because the token ends where the next one starts */);
					var startPoint = new SnapshotPoint(_textBuffer.CurrentSnapshot, region.Start.Index);
					var span = new SnapshotSpan(startPoint, endLine.End);

					var text = _textBuffer.CurrentSnapshot.GetText(span);
					var ellipsis = text.Split(new[] { '\r', '\n' }, 2)[0] + " ...";
					yield return new TagSpan<IOutliningRegionTag>(
						span,
						new OutliningRegionTag(false, false, ellipsis, text)
					);
				}
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
