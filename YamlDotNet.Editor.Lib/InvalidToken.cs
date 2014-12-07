using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

namespace YamlDotNet.Editor.Lib
{
	public class InvalidToken : Token
	{
		public InvalidToken(Mark start, Mark end)
			: base(start, end)
		{
		}
	}
}