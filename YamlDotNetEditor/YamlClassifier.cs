//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013, 2014 Antoine Aubry and contributors

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
using System.Diagnostics;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

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

		private ScannerBuffer _currentTokens;
		private int _currentTokensVersionNumber;

		internal YamlClassifier(IClassificationTypeRegistryService registry)
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
		}

		/// <summary>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
		/// </summary>
		/// <param name="trackingSpan">The span currently being classified</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			Rescan(span);

			var classifications = new List<ClassificationSpan>();

			var tokens = _currentTokens.GetTokensBetween(span.Start.Position, span.End.Position);

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

			//var text = span.GetText();



			//var match = Regex.Match(text, @"^( *(\t+))+");
			//if (match.Success)
			//{
			//	foreach (Capture capture in match.Groups[2].Captures)
			//	{
			//		classifications.Add(
			//			new ClassificationSpan(
			//				new SnapshotSpan(
			//					span.Snapshot,
			//					new Span(span.Start + capture.Index, capture.Length)
			//				),
			//				_tab
			//			)
			//		);
			//	}
			//}


			return classifications;
		}

		private class ScannerBuffer
		{
			private class TokenEndPositionComparer : IComparer<Token>
			{
				private TokenEndPositionComparer()
				{
				}

				public int Compare(Token x, Token y)
				{
					return x.End.Index.CompareTo(y.End.Index);
				}

				public static readonly TokenEndPositionComparer Instance = new TokenEndPositionComparer();
			}

			private readonly Scanner _scanner;
			private readonly List<Token> _bufferedTokens;
			private int _lastBufferedIndex = -1;
			private int _errorCount;

			public ScannerBuffer(string text)
			{
				_scanner = new Scanner(new StringReader(text), skipComments: false);
				_bufferedTokens = new List<Token>();
			}

			public IEnumerable<Token> GetTokensBetween(int start, int end)
			{
				EnsureReadUntil(end);

				// Dummy token used to perform the binary search
				var markerToken = new StreamStart(default(Mark), new Mark(start, 1, 1));

				var startIndex = _bufferedTokens.BinarySearch(markerToken, TokenEndPositionComparer.Instance);
				if (startIndex < 0)
				{
					startIndex = ~startIndex;
				}

				for (var i = startIndex; i < _bufferedTokens.Count; ++i)
				{
					var token = _bufferedTokens[i];
					if (token.Start.Index > end)
					{
						yield break;
					}

					yield return token;
				}
			}

			private void EnsureReadUntil(int end)
			{
				var lastPosition = _bufferedTokens.Count > 0
					? _bufferedTokens[_bufferedTokens.Count - 1].End
					: new Mark();

				// Give up after 100 syntax errors
				for (; _errorCount < 100; ++_errorCount)
				{
					try
					{
						while (lastPosition.Index <= end && _scanner.MoveNext())
						{
							_bufferedTokens.Add(_scanner.Current);
							lastPosition = _scanner.Current.End;
							_lastBufferedIndex = lastPosition.Index;
						}
						return;
					}
					catch (SyntaxErrorException ex)
					{
						var errorEnd = ex.End.Index == ex.Start.Index
							? new Mark(ex.End.Index + 1, ex.End.Line, ex.End.Column)
							: ex.End;

						_bufferedTokens.Add(new InvalidToken(ex.Start, errorEnd));
					}
				}
			}
		}

		private class InvalidToken : Token
		{
			public InvalidToken(Mark start, Mark end)
				: base(start, end)
			{
			}
		}

		private void Rescan(SnapshotSpan span)
		{
			var textSnapshot = span.Snapshot;
			if (_currentTokens == null || _currentTokensVersionNumber != textSnapshot.Version.VersionNumber)
			{
				_currentTokens = new ScannerBuffer(textSnapshot.GetText());
				_currentTokensVersionNumber = textSnapshot.Version.VersionNumber;

				if (ClassificationChanged != null)
				{
					ClassificationChanged(
						this,
						new ClassificationChangedEventArgs(
							new SnapshotSpan(
								textSnapshot,
								new Span(span.Start.Position, textSnapshot.Length - span.Start.Position)
							)
						)
					);
				}
			}
		}

#pragma warning disable 67
		// This event gets raised if a non-text change would affect the classification in some way,
		// for example typing /* would cause the classification to change in C# without directly
		// affecting the span.
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

		[Serializable]
		private class UpdatableStringReader : TextReader
		{
			public string Text { get; set; }
			private int position;

			public override int Read()
			{
				if (Text != null && position < Text.Length)
				{
					return Text[position++];
				}

				return -1;
			}
		}
	}
}