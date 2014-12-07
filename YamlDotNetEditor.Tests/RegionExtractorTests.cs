using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Editor.Lib;

namespace YamlDotNetEditor.Tests
{
	public class RegionExtractorTests
	{
		private void ValidateRegions(IEnumerable<Region> actual, params Region[] expected)
		{
			Assert.Equal(
				expected.OrderBy(e => e.Start).ThenBy(e => e.End),
				actual.OrderBy(e => e.Start).ThenBy(e => e.End),
				new RegionComparer()
			);
		}

		private class RegionComparer : IEqualityComparer<Region>
		{
			public bool Equals(Region x, Region y)
			{
				return x.Start.Line.Equals(y.Start.Line)
					&& x.Start.Column.Equals(y.Start.Column)
					&& x.End.Line.Equals(y.End.Line)
					&& x.End.Column.Equals(y.End.Column)
					&& (x.Ellipsis == null || y.Ellipsis == null || x.Ellipsis.Equals(y.Ellipsis))
					&& (x.Tooltip == null || y.Tooltip == null || x.Tooltip.Equals(y.Tooltip));
			}

			public int GetHashCode(Region obj)
			{
				return obj.GetHashCode();
			}
		}

		private Region Region(int startLine, int startColumn, int endLine, int endColumn = 1)
		{
			return new Region(new Mark(0, startLine, startColumn), new Mark(0, endLine, endColumn), null, null);
		}

		[Fact]
		public void Scalar_produces_no_regions()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				scalar
			").AsEnumerable()).ToList();

			ValidateRegions(regions);
		}

		[Fact]
		public void Sequence_of_scalars_produces_no_regions()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				- a
				- b
				- c
			").AsEnumerable()).ToList();

			ValidateRegions(regions);
		}

		[Fact]
		public void Sequence_of_sequences_produces_a_region_for_each_inner_sequence()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				-
				  - a
				  - b
				-
				  - c
				  - d
			").AsEnumerable()).ToList();

			ValidateRegions(regions
				, Region(1, 2, 4)
				, Region(4, 2, 7)
			);
		}

		[Fact]
		public void Mapping_of_scalars_produces_no_regions()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				x: a
				y: b
				z: c
			").AsEnumerable()).ToList();

			ValidateRegions(regions);
		}

		[Fact]
		public void Mapping_of_sequences_produces_a_region_for_each_sequence()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				x:
				  - a
				  - b
				y:
				  - c
				  - d
			").AsEnumerable()).ToList();

			ValidateRegions(regions
				, Region(1, 3, 4)
				, Region(4, 3, 7)
			);
		}

		[Fact]
		public void Sequence_of_mappings_produces_a_region_for_each_mapping()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				-
				  x: a
				  y: b
				-
				  z: c
				  w: d
			").AsEnumerable()).ToList();

			ValidateRegions(regions
				, Region(1, 2, 4)
				, Region(4, 2, 7)
			);
		}

		[Fact]
		public void Block_of_text_produces_region()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				- |
				  hello
				  world
				- b
				- c
			").AsEnumerable()).ToList();

			ValidateRegions(regions
				, Region(1, 3, 4)
			);
		}
	}
}
