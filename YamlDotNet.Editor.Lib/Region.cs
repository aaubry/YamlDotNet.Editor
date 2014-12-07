using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace YamlDotNet.Editor.Lib
{
	public class Region
	{
		public Mark Start { get; private set; }
		public Mark End { get; private set; }
		public string Ellipsis { get; private set; }
		public string Tooltip { get; private set; }

		public Region(Mark start, Mark end, string ellipsis, string tooltip)
		{
			Start = start;
			End = end;
			Ellipsis = ellipsis;
			Tooltip = tooltip;
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}) => ({2}, {3})", Start.Line, Start.Column, End.Line, End.Column);
		}
	}
}
