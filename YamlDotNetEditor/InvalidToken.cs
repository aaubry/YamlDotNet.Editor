using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

namespace YamlDotNetEditor
{
	internal class InvalidToken : Token
	{
		public InvalidToken(Mark start, Mark end)
			: base(start, end)
		{
		}
	}
}