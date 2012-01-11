using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarkupConverter.Metro.Tests
{
    [TestClass]
    public class SimpleTests
    {
        [TestMethod]        
        public void ConvertHtmlToXamlTest()
        {
            var m = new MarkupConverter();

            var html = @"<!DOCTYPE html>
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

            var xaml = m.ConvertHtmlToXaml(html);

            Debug.WriteLine(xaml);
        }
    }
}