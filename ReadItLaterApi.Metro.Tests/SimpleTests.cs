using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReadItLaterApi.Metro.Tests
{
    [TestClass]
    public class SimpleTests
    {
        public static string USERNAME = "beau-testing";
        public static string PASSWORD = "testing";

        public ReadItLaterApi _api;

        [TestInitialize]
        public void TestSetup()
        {
            _api = new ReadItLaterApi(USERNAME, PASSWORD);
        }

        [TestMethod]        
        public void TestVerifyCredentials()
        {
            Assert.IsTrue(_api.VerifyCredentials());
        }

        [TestMethod]
        public void TestGetReadingList()
        {
            var result = _api.GetReadingList();

            Assert.IsNotNull(result.List);
        }

        [TestMethod]
        public void TestGetText()
        {
            var result = _api.GetText("http://arstechnica.com/microsoft/news/2012/01/windows-8-storage-spaces-detailed-pooling-redundant-disk-space-for-all.ars");

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        }
    }
}