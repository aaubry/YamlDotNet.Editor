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
				ParseBlocks(tokenEnumerator, null, 0, regions);
				return regions;
			}
		}

		private void ParseBlocks(IEnumerator<Token> tokenEnumerator, Token currentBlockStart, int depth, List<Region> regions)
		{
			while (tokenEnumerator.MoveNext())
			{
				var token = tokenEnumerator.Current;
				if (token is BlockSequenceStart)
				{
					ParseBlocks(tokenEnumerator, token, depth + 1, regions);
				}
				else if (token is BlockMappingStart)
				{
					ParseBlocks(tokenEnumerator, token, depth + 1, regions);
				}
				else if (token is DocumentStart || token is StreamStart)
				{
					ParseBlocks(tokenEnumerator, null, depth + 1, regions);
				}
				else if (token is DocumentEnd || token is StreamEnd)
				{
					break;
				}
				else if (token is BlockEnd)
				{
					if (currentBlockStart != null)
					{
						regions.Add(new Region(currentBlockStart.Start, token.End, "...", "..."));
					}
					break;
				}
				else if (token is Scalar)
				{
					if (token.Start.Line != token.End.Line)
					{
						regions.Add(new Region(token.Start, token.End, "...", ((Scalar)token).Value));
					}
				}
			}
		}
	}
}