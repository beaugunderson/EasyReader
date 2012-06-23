using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace ReadItLaterApi.Metro.Tests
{
    [TestClass]
    public class SimpleTests
    {
        private const string USERNAME = "beau-testing";
        private const string PASSWORD = "testing";

        private ReadItLaterApi _api;

        [TestInitialize]
        public void TestSetup()
        {
            _api = new ReadItLaterApi(USERNAME, PASSWORD);
        }

        [TestMethod]        
        public async Task TestVerifyCredentials()
        {
            var result = await _api.VerifyCredentials();

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestGetReadingList()
        {
            var result = await _api.GetReadingList();

            Assert.IsNotNull(result.List);
        }

        [TestMethod]
        public async Task TestGetText()
        {
            var result = await _api.GetText("http://arstechnica.com/microsoft/news/2012/01/windows-8-storage-spaces-detailed-pooling-redundant-disk-space-for-all.ars");

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        }
    }
}