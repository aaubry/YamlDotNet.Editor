using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Core;
using YamlDotNet.Editor.Lib;

namespace YamlDotNetEditor
{
	internal class YamlOutliningTagger : ITagger<IOutliningRegionTag>
	{
		private readonly ITextBuffer _textBuffer;
		private readonly TextBufferParser _parser;
		private List<Region> _regions;

		public YamlOutliningTagger(ITextBuffer textBuffer, TextBufferParser parser)
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
					yield return new TagSpan<IOutliningRegionTag>(
						span,
						new OutliningRegionTag(false, false, region.Ellipsis, region.Tooltip)
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
