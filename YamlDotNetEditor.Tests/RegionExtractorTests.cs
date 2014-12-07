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
using System.Linq;
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
					&& x.End.Column.Equals(y.End.Column);
			}

			public int GetHashCode(Region obj)
			{
				return obj.GetHashCode();
			}
		}

		private Region Region(int startLine, int startColumn, int endLine, int endColumn = 1)
		{
			return new Region(new Mark(0, startLine, startColumn), new Mark(0, endLine, endColumn));
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
		public void Mapping_of_mappings_produces_a_region_for_each_inner_mapping()
		{
			var sut = new RegionExtractor();

			var regions = sut.GetRegions(Yaml.ScannerForText(@"
				x:
				  f: a
				  g: b
				y:
				  h: c
				  i: d
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
				, Region(1, 2, 4)
			);
		}
	}
}
