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

using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Editor.Lib;

namespace YamlDotNetEditor
{
	internal class YamlClassifier : IClassifier
	{
		private readonly IClassificationType _comment;
		private readonly IClassificationType _anchor;
		private readonly IClassificationType _alias;
		private readonly IClassificationType _key;
		private readonly IClassificationType _value;
		private readonly IClassificationType _tag;
		private readonly IClassificationType _symbol;
		private readonly IClassificationType _directive;
		private readonly IClassificationType _invalid;

		private readonly TextBufferParser _parser;

		internal YamlClassifier(IClassificationTypeRegistryService registry, TextBufferParser parser)
		{
			_comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
			_anchor = registry.GetClassificationType("YamlAnchor");
			_alias = registry.GetClassificationType("YamlAlias");
			_key = registry.GetClassificationType("YamlKey");
			_value = registry.GetClassificationType("YamlValue");
			_tag = registry.GetClassificationType("YamlTag");
			_symbol = registry.GetClassificationType("YamlSymbol");
			_directive = registry.GetClassificationType("YamlDirective");
			_invalid = registry.GetClassificationType("YamlInvalid");

			_parser = parser;
			_parser.ParseTreeChanged += (s, e) => OnClassificationChanged(new ClassificationChangedEventArgs(e.Span));
		}

		/// <summary>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
		/// </summary>
		/// <param name="trackingSpan">The span currently being classified</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			var classifications = new List<ClassificationSpan>();

			var tokens = _parser.GetTokensBetween(span.Start.Position, span.End.Position);

			Type previousTokenType = null;
			foreach (var token in tokens)
			{
				IClassificationType classificationType = null;

				var currentTokenType = token.GetType();

				if (currentTokenType == typeof(Anchor))
				{
					classificationType = _anchor;
				}
				else if (currentTokenType == typeof(AnchorAlias))
				{
					classificationType = _alias;
				}
				else if (currentTokenType == typeof(Scalar))
				{
					classificationType = previousTokenType == typeof(Key) ? _key : _value;
				}
				else if (currentTokenType == typeof(Tag))
				{
					classificationType = _tag;
				}
				else if (currentTokenType == typeof(TagDirective))
				{
					classificationType = _directive;
				}
				else if (currentTokenType == typeof(VersionDirective))
				{
					classificationType = _directive;
				}
				else if (currentTokenType == typeof(Comment))
				{
					classificationType = _comment;
				}
				else if (currentTokenType == typeof(InvalidToken))
				{
					classificationType = _invalid;
				}
				else if (token.End.Index > token.Start.Index)
				{
					classificationType = _symbol;
				}

				previousTokenType = currentTokenType;

				if (classificationType != null)
				{
					var start = Math.Max(token.Start.Index, span.Start.Position);
					var end = Math.Min(token.End.Index, span.End.Position + 1);

					classifications.Add(
						new ClassificationSpan(
							new SnapshotSpan(
								span.Snapshot,
								new Span(start, end - start)
							),
							classificationType
						)
					);
				}
			}

			return classifications;
		}

#pragma warning disable 67
		// This event gets raised if a non-text change would affect the classification in some way,
		// for example typing /* would cause the classification to change in C# without directly
		// affecting the span.
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		protected virtual void OnClassificationChanged(ClassificationChangedEventArgs e)
		{
			if (ClassificationChanged != null)
			{
				ClassificationChanged(this, e);
			}
		}
#pragma warning restore 67
	}
}