using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace MarkupConverter.Metro.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]        
        public void ConvertHtmlToXamlTest()
        {
            var markupConverter = new MarkupConverter();

            const string html = @"<!DOCTYPE html>
<html>
 <head>
  <title>Just testing...</title>
 </head>

 <body>
  <b>The world is ending.</b>

  <p>Yes, <strong>it is.</strong></p>

  <ul>
   <li>I'm</li>
   <li><em>a</em></li>
   <li>list!</li>
  </ul>
 </body>
</html>";

            var xaml = markupConverter.ConvertHtmlToXaml(html);

            Debug.WriteLine(xaml);
        }
    }
}