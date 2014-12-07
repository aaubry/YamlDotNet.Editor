using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace YamlDotNetEditor
{
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IOutliningRegionTag))]
	[ContentType("yaml")]
	internal sealed class YamlOutliningTaggerProvider : ITaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			var parser = buffer.Properties.GetOrCreateSingletonProperty<TextBufferParser>(() => new TextBufferParser(buffer));
			return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(() => (ITagger<T>)new YamlOutliningTagger(buffer, parser));
		}
	}
}
