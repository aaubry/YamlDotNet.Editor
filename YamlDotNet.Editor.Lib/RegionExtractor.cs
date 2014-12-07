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

using System.Collections.Generic;
using YamlDotNet.Core.Tokens;

namespace YamlDotNet.Editor.Lib
{
	public class RegionExtractor
	{
		public IEnumerable<Region> GetRegions(IEnumerable<Token> tokens)
		{
			using (var tokenEnumerator = tokens.GetEnumerator())
			{
				var regions = new List<Region>();
				ParseRegions(new BufferedTokenEnumerator(tokenEnumerator), regions);
				return regions;
			}
		}

		private class BufferedTokenEnumerator
		{
			private readonly IEnumerator<Token> _enumerator;

			public BufferedTokenEnumerator(IEnumerator<Token> enumerator)
			{
				_enumerator = enumerator;
			}

			public Token Previous { get; private set; }
			public Token Current { get { return _enumerator.Current; } }

			public bool MoveNext()
			{
				Previous = _enumerator.Current;
				return _enumerator.MoveNext();
			}
		}

		private void ParseBlockMapping(BufferedTokenEnumerator tokenEnumerator, List<Region> regions)
		{
			Token currentValueEntry = null;
			while (tokenEnumerator.MoveNext())
			{
				var token = tokenEnumerator.Current;
				var isBlockEnd = token is BlockEnd;
				if (isBlockEnd || token is Key)
				{
					if (currentValueEntry != null && currentValueEntry.End.Line != tokenEnumerator.Previous.End.Line)
					{
						regions.Add(new Region(currentValueEntry.End, tokenEnumerator.Previous.End));
					}

					if (isBlockEnd)
					{
						return;
					}
				}
				else if (token is Value)
				{
					currentValueEntry = token;
				}
				else if (token is BlockSequenceStart)
				{
					ParseBlockSequence(tokenEnumerator, regions);
				}
				else if (token is BlockMappingStart)
				{
					ParseBlockMapping(tokenEnumerator, regions);
				}
			}
		}

		private void ParseBlockSequence(BufferedTokenEnumerator tokenEnumerator, List<Region> regions)
		{
			Token previousBlockEntry = null;
			while (tokenEnumerator.MoveNext())
			{
				var token = tokenEnumerator.Current;
				var isBlockEnd = token is BlockEnd;
				if (isBlockEnd || token is BlockEntry)
				{
					if (previousBlockEntry != null && previousBlockEntry.End.Line != tokenEnumerator.Previous.End.Line)
					{
						regions.Add(new Region(previousBlockEntry.End, tokenEnumerator.Previous.End));
					}

					if (isBlockEnd)
					{
						return;
					}

					previousBlockEntry = token;
				}
				else if (token is BlockSequenceStart)
				{
					ParseBlockSequence(tokenEnumerator, regions);
				}
				else if (token is BlockMappingStart)
				{
					ParseBlockMapping(tokenEnumerator, regions);
				}
			}
		}

		private void ParseRegions(BufferedTokenEnumerator tokenEnumerator, List<Region> regions)
		{
			while (tokenEnumerator.MoveNext())
			{
				var token = tokenEnumerator.Current;
				if (token is BlockSequenceStart)
				{
					ParseBlockSequence(tokenEnumerator, regions);
				}
				else if (token is BlockMappingStart)
				{
					ParseBlockMapping(tokenEnumerator, regions);
				}
			}
		}
	}
}