//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Tests
{
    using System;
    using Microsoft.Azure.Documents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PartitionKeyTests
    {
        [TestMethod]
        public void NullValue()
        {
            new PartitionKey(null);
        }

        [TestMethod]
        public void ToStringGetsJsonString()
        {
            const string somePK = "somePK";
            string expected = $"[\"{somePK}\"]";
            PartitionKey pk = new PartitionKey(somePK);
            Assert.AreEqual(expected, pk.ToString());
        }

        [TestMethod]
        public void TestPartitionKeyValues()
        {
            Tuple<dynamic, string>[] testcases =
            {
                Tuple.Create<dynamic, string>(Documents.Undefined.Value, "[{}]"),
                Tuple.Create<dynamic, string>(Documents.Undefined.Value, "[{}]"),
                Tuple.Create<dynamic, string>(false, "[false]"),
                Tuple.Create<dynamic, string>(true, "[true]"),
                Tuple.Create<dynamic, string>(123.456, "[123.456]"),
                Tuple.Create<dynamic, string>("PartitionKeyValue", "[\"PartitionKeyValue\"]"),
            };

            foreach (Tuple<object, string> testcase in testcases)
            {
                Assert.AreEqual(testcase.Item2, new PartitionKey(testcase.Item1).ToString());
            }
        }

        [TestMethod]
        public void TestPartitionKeyDefinitionAreEquivalent()
        {
            //Different partition key path test
            PartitionKeyDefinition definition1 = new PartitionKeyDefinition();
            definition1.Paths.Add("/pk1");

            PartitionKeyDefinition definition2 = new PartitionKeyDefinition();
            definition2.Paths.Add("/pk2");

            Assert.IsFalse(PartitionKeyDefinition.AreEquivalent(definition1, definition2));

            //Different partition kind test
            definition1 = new PartitionKeyDefinition();
            definition1.Paths.Add("/pk1");
            definition1.Kind = PartitionKind.Hash;

            definition2 = new PartitionKeyDefinition();
            definition2.Paths.Add("/pk1");
            definition2.Kind = PartitionKind.Range;

            Assert.IsFalse(PartitionKeyDefinition.AreEquivalent(definition1, definition2));

            //Different partition version test
            definition1 = new PartitionKeyDefinition();
            definition1.Paths.Add("/pk1");
            definition1.Version = PartitionKeyDefinitionVersion.V1;

            definition2 = new PartitionKeyDefinition();
            definition2.Paths.Add("/pk1");
            definition2.Version = PartitionKeyDefinitionVersion.V2;

            Assert.IsFalse(PartitionKeyDefinition.AreEquivalent(definition1, definition2));

            //Same partition key path test
            definition1 = new PartitionKeyDefinition();
            definition1.Paths.Add("/pk1");

            definition2 = new PartitionKeyDefinition();
            definition2.Paths.Add("/pk1");

            Assert.IsTrue(PartitionKeyDefinition.AreEquivalent(definition1, definition2));
        }

        [TestMethod]
        public void RoundTripTests()
        {
            Cosmos.PartitionKey[] partitionKeys = new Cosmos.PartitionKey[]
            {
                // None partition key is not serializable.
                // Cosmos.PartitionKey.None,
                Cosmos.PartitionKey.Null,
                new Cosmos.PartitionKey(true),
                new Cosmos.PartitionKey(false),
                new Cosmos.PartitionKey(42),
                new Cosmos.PartitionKey("asdf"),
            };

            foreach (Cosmos.PartitionKey partitionKey in partitionKeys)
            {
                string serializedPartitionKey = partitionKey.ToJsonString();
                Assert.IsTrue(Cosmos.PartitionKey.TryParseJsonString(serializedPartitionKey, out Cosmos.PartitionKey parsedPartitionKey));
                Assert.AreEqual(parsedPartitionKey.ToJsonString(), serializedPartitionKey);
            }

            Assert.IsFalse(Cosmos.PartitionKey.TryParseJsonString("Ceci n'est pas une partition key.", out Cosmos.PartitionKey thisNotAPartitionKey));
        }

        [TestMethod]
        public void TestCosmosPartitionKeyComparison()
        {
            Cosmos.PartitionKey pk = new Cosmos.PartitionKey("partition_key");
            Cosmos.PartitionKey equal_to_pk = new Cosmos.PartitionKey("partition_key");
            Cosmos.PartitionKey differs_from_pk = new Cosmos.PartitionKey("different_partition_key");

            (Func<bool> testFunction, bool expectedResult)[] testcases =
            {
                (() => pk.Equals((object)equal_to_pk), true),
                (() => pk.Equals((object)differs_from_pk), false),
                (() => pk.Equals(equal_to_pk), true),
                (() => pk.Equals(differs_from_pk), false),
                (() => pk == equal_to_pk, true),
                (() => pk == differs_from_pk, false),
                (() => pk != equal_to_pk, false),
                (() => pk != differs_from_pk, true),
                (() => pk.CompareTo(equal_to_pk) == 0, true),
                (() => pk.GetHashCode() == equal_to_pk.GetHashCode(), true),
            };

            foreach ((Func<bool> testFunction, bool expectedResult) in testcases)
            {
                bool result = testFunction();
                Assert.AreEqual(result, expectedResult);
            }
        }
    }
}