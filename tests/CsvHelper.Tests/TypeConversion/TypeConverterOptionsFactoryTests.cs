﻿// Copyright 2009-2022 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Xunit;

namespace CsvHelper.Tests.TypeConversion
{
	
	public class TypeConverterOptionsFactoryTests
	{
		[Fact]
		public void AddGetRemoveTest()
		{
			var customOptions = new TypeConverterOptions
			{
				Formats = new string[] { "custom" },
			};
			var typeConverterOptionsFactory = new TypeConverterOptionsCache();

			typeConverterOptionsFactory.AddOptions<string>(customOptions);
			var options = typeConverterOptionsFactory.GetOptions<string>();

			Assert.Equal(customOptions.Formats, options.Formats);

			typeConverterOptionsFactory.RemoveOptions<string>();

			options = typeConverterOptionsFactory.GetOptions<string>();

			Assert.NotEqual(customOptions.Formats, options.Formats);
		}

		[Fact]
		public void GetFieldTest()
		{
			var options = new TypeConverterOptions { NumberStyles = NumberStyles.AllowThousands };

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvReader = new CsvReader(reader, config))
			{
				writer.WriteLine("\"1,234\",\"5,678\"");
				writer.Flush();
				stream.Position = 0;

				csvReader.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvReader.Read();
				Assert.Equal(1234, csvReader.GetField<int>(0));
				Assert.Equal(5678, csvReader.GetField(typeof(int), 1));
			}
		}

		[Fact]
		public void GetRecordsTest()
		{
			var options = new TypeConverterOptions { NumberStyles = NumberStyles.AllowThousands };

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvReader = new CsvReader(reader, config))
			{
				writer.WriteLine("\"1,234\",\"5,678\"");
				writer.Flush();
				stream.Position = 0;

				csvReader.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvReader.GetRecords<Test>().ToList();
			}
		}

		[Fact]
		public void GetRecordsAppliedWhenMappedTest()
		{
			var options = new TypeConverterOptions { NumberStyles = NumberStyles.AllowThousands };

			var config = new CsvConfiguration(new CultureInfo("en-US"))
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvReader = new CsvReader(reader, config))
			{
				writer.WriteLine("\"1,234\",\"$5,678\"");
				writer.Flush();
				stream.Position = 0;

				csvReader.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvReader.Context.RegisterClassMap<TestMap>();
				csvReader.GetRecords<Test>().ToList();
			}
		}

		[Fact]
		public void WriteFieldTest()
		{
			var options = new TypeConverterOptions { Formats = new string[] { "c" } };

			var config = new CsvConfiguration(new CultureInfo("en-US"))
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvWriter = new CsvWriter(writer, config))
			{
				csvWriter.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvWriter.WriteField(1234);
				csvWriter.NextRecord();
				writer.Flush();
				stream.Position = 0;
				var record = reader.ReadToEnd();

				Assert.Equal("\"$1,234.00\"\r\n", record);
			}
		}

		[Fact]
		public void WriteRecordsTest()
		{
			var options = new TypeConverterOptions { Formats = new string[] { "c" } };

			var config = new CsvConfiguration(new CultureInfo("en-US"))
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvWriter = new CsvWriter(writer, config))
			{
				var list = new List<Test>
				{
					new Test { Number = 1234, NumberOverridenInMap = 5678 },
				};
				csvWriter.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvWriter.WriteRecords(list);
				writer.Flush();
				stream.Position = 0;
				var record = reader.ReadToEnd();

				Assert.Equal("\"$1,234.00\",\"$5,678.00\"\r\n", record);
			}
		}

		[Fact]
		public void WriteRecordsAppliedWhenMappedTest()
		{
			var options = new TypeConverterOptions { Formats = new string[] { "c" } };

			var config = new CsvConfiguration(new CultureInfo("en-US"))
			{
				HasHeaderRecord = false,
			};
			using (var stream = new MemoryStream())
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			using (var csvWriter = new CsvWriter(writer, config))
			{
				var list = new List<Test>
				{
					new Test { Number = 1234, NumberOverridenInMap = 5678 },
				};
				csvWriter.Context.TypeConverterOptionsCache.AddOptions<int>(options);
				csvWriter.Context.RegisterClassMap<TestMap>();
				csvWriter.WriteRecords(list);
				writer.Flush();
				stream.Position = 0;
				var record = reader.ReadToEnd();

				Assert.Equal("\"$1,234.00\",\"5,678.00\"\r\n", record);
			}
		}

		private class Test
		{
			public int Number { get; set; }

			public int NumberOverridenInMap { get; set; }
		}

		private sealed class TestMap : ClassMap<Test>
		{
			public TestMap()
			{
				Map(m => m.Number);
				Map(m => m.NumberOverridenInMap)
					.TypeConverterOption.NumberStyles(NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol)
					.TypeConverterOption.Format("N2");
			}
		}
	}
}
