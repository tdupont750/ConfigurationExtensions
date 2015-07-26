using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using Xunit;

namespace ConfigurationExtensions
{
    public class NameValueCollectionExtensionsTests
    {
        internal enum TestEnum
        {
            Goodnight,
            Moon
        }

        [Fact]
        public void Combine()
        {
            var one = new NameValueCollection
            {
                {"A", "1"}
            };

            var two = new NameValueCollection
            {
                {"A", "2"},
                {"B", "2"}
            };

            var three = new NameValueCollection
            {
                {"A", "3"},
                {"B", "3"},
                {"C", "3"}
            };

            var result = one.Combine(two, three);

            Assert.Equal(3, result.AllKeys.Length);
            Assert.Equal("1", result["A"]);
            Assert.Equal("2", result["B"]);
            Assert.Equal("3", result["C"]);
        }

        [Fact]
        public void CombineAndCreateObject()
        {
            var primaryValues = (NameValueCollection) ConfigurationManager
                .GetSection("primaryValues");

            var primaryObject = primaryValues.CreateObject<TestConfig>();
            Assert.Equal("Moon", primaryObject.String);
            Assert.Equal(0, primaryObject.Int);

            var defaultValues = (NameValueCollection)ConfigurationManager
                .GetSection("defaultValues");

            var defaultObject = defaultValues.CreateObject<TestConfig>();
            Assert.Equal("Goodnight", defaultObject.String);
            Assert.Equal(42, defaultObject.Int);

            var combineValues = primaryValues.Combine(defaultValues);

            var combineObject = combineValues.CreateObject<TestConfig>();
            Assert.Equal("Moon", combineObject.String);
            Assert.Equal(42, combineObject.Int);
        }

        [Fact]
        public void CreateSimpleObject()
        {
            var result = ConfigurationManager.AppSettings.CreateObject<TestConfig>();

            Assert.NotNull(result);
            Assert.Equal(1, result.Int);
            Assert.Equal(42, result.IntWithDefault);
            Assert.Equal("Hello", result.String);
            Assert.Equal(new DateTime(2015, 1, 2), result.DateTime);
            Assert.Equal(TimeSpan.FromMinutes(1), result.TimeSpan);
            Assert.Equal(TestEnum.Moon, result.Enum);
            Assert.Equal(2, result.NullableIntA);
            Assert.Null(result.NullableIntB);
        }

        [Fact]
        public void CreateComplexObject()
        {
            var collection = new NameValueCollection
            {
                {"TestConfig.Test.Int", "3"},
                {"TestConfig.ListOfInt[0]", "4"},
                {"TestConfig.ListOfInt[1]", "5"},
                {"TestConfig.ListOfTestConfig[2].Int", "6"},
                {"TestConfig.ListOfTestConfig[2].String", "Arg"},
                {"TestConfig.ListOfTestConfig[1].Int", "7"},
                {"TestConfig.MapOfInt[Hello]", "8"},
                {"TestConfig.MapOfInt[World]", "9"},
            };

            var result = collection.CreateObject<TestConfig>();

            Assert.NotNull(result);

            Assert.NotNull(result.Test);
            Assert.Equal(3, result.Test.Int);

            Assert.NotNull(result.ListOfInt);
            Assert.Equal(2, result.ListOfInt.Count);
            Assert.Equal(4, result.ListOfInt[0]);
            Assert.Equal(5, result.ListOfInt[1]);

            Assert.NotNull(result.ListOfTestConfig);
            Assert.Equal(2, result.ListOfTestConfig.Count);
            Assert.NotNull(result.ListOfTestConfig[0]);
            Assert.Equal(7, result.ListOfTestConfig[0].Int);
            Assert.NotNull(result.ListOfTestConfig[1]);
            Assert.Equal(6, result.ListOfTestConfig[1].Int);
            Assert.Equal("Arg", result.ListOfTestConfig[1].String);

            Assert.NotNull(result.MapOfInt);
            Assert.Equal(2, result.MapOfInt.Count);
            Assert.Equal(8, result.MapOfInt["Hello"]);
            Assert.Equal(9, result.MapOfInt["World"]);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("10")]
        public void CreateValidObject(string value)
        {
            var collection = new NameValueCollection
            {
                {"TestConfigWithAnnotations.Int", value},
            };

            collection.CreateObject<TestConfigWithAnnotations>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("11")]
        public void CreateInvalidObject(string value)
        {
            var collection = new NameValueCollection
            {
                {"TestConfigWithAnnotations.Int", value},
            };

            Assert.Throws<ValidationException>(
                () =>
                {
                    collection.CreateObject<TestConfigWithAnnotations>();
                });
        }

        [Fact]
        public void CreateInt()
        {
            var collection = new NameValueCollection
            {
                {"A", "1"},
            };

            var result = collection.CreateObject<int>("A");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CreateIntList()
        {
            var collection = new NameValueCollection
            {
                {"A[1]", "1"},
                {"A[2]", "2"},
                {"Something", "Else"},
                {"A[4]", "4"},
            };

            var result = collection.CreateObject<List<int>>("A");

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(4, result[2]);
        }

        [Fact]
        public void CreateObjectList()
        {
            var collection = new NameValueCollection
            {
                {"Something", "Else"},
                {"TestConfig.ListOfTestConfig[2].Int", "6"},
                {"TestConfig.ListOfTestConfig[2].String", "Arg"},
                {"TestConfig.ListOfTestConfig[1].Int", "7"},
            };

            var result = collection.CreateObject<TestConfig>();

            Assert.NotNull(result);
            Assert.NotNull(result.ListOfTestConfig);
            Assert.Equal(2, result.ListOfTestConfig.Count);
            Assert.NotNull(result.ListOfTestConfig[0]);
            Assert.Equal(7, result.ListOfTestConfig[0].Int);
            Assert.NotNull(result.ListOfTestConfig[1]);
            Assert.Equal(6, result.ListOfTestConfig[1].Int);
            Assert.Equal("Arg", result.ListOfTestConfig[1].String);
        }

        [Fact]
        public void CreateIntDictionary()
        {
            var collection = new NameValueCollection
            {
                {"A[A]", "1"},
                {"Something", "Else"},
                {"A[BB]", "22"},
                {"A[CCC]", "333"},
            };

            var result = collection.CreateObject<Dictionary<string, int>>("A");

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result["A"]);
            Assert.Equal(22, result["BB"]);
            Assert.Equal(333, result["CCC"]);
        }

        [Fact]
        public void CreateObjectDictionary()
        {
            var collection = new NameValueCollection
            {
                {"A[A].Int", "1"},
                {"Something", "Else"},
                {"A[BB].String", "Hello"},
                {"A[CCC].Enum", "Goodnight"},
            };

            var result = collection.CreateObject<Dictionary<string, TestConfig>>("A");

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result["A"].Int);
            Assert.Equal("Hello", result["BB"].String);
            Assert.Equal(TestEnum.Goodnight, result["CCC"].Enum);
        }

        internal class TestConfig
        {
            public TestConfig()
            {
                IntWithDefault = 42;
            }

            public int Int { get; set; }
            public int IntWithDefault { get; set; }
            public string String { get; set; }
            public DateTime DateTime { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public TestEnum Enum { get; set; }
            public int? NullableIntA { get; set; }
            public int? NullableIntB { get; set; }
            public TestConfig Test { get; set; }
            public List<int> ListOfInt { get; set; }
            public List<TestConfig> ListOfTestConfig { get; set; }
            public Dictionary<string, int> MapOfInt { get; set; }
        }

        internal class TestConfigWithAnnotations
        {
            [Range(1, 10)]
            public int Int { get; set; }
        }
    }
}
