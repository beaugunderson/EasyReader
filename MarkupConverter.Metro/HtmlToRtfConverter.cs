using System;
using System.IO;
using System.Text;

using Windows.UI.Xaml.Controls;

namespace MarkupConverter
{
    public static class HtmlToRtfConverter
    {
        public static string ConvertHtmlToRtf(string htmlText)
        {
            var xamlText = HtmlToXamlConverter.ConvertHtmlToXaml(htmlText, false);

            return ConvertXamlToRtf(xamlText);
        }

        private static string ConvertXamlToRtf(string xamlText)
        {
            var richTextBlock = new RichTextBlock();

            if (string.IsNullOrEmpty(xamlText))
            {
                return "";
            }

            var textRange = new TextRange(richTextBlock.Document.ContentStart, richTextBlock.Document.ContentEnd);

            //Create a MemoryStream of the xaml content

            using (var xamlMemoryStream = new MemoryStream())
            {
                using (var xamlStreamWriter = new StreamWriter(xamlMemoryStream))
                {
                    xamlStreamWriter.Write(xamlText);
                    xamlStreamWriter.Flush();
                    xamlMemoryStream.Seek(0, SeekOrigin.Begin);

                    //Load the MemoryStream into TextRange ranging from start to end of RichTextBlock.
                    textRange.Load(xamlMemoryStream, DataFormats.Xaml);
                }
            }

            using (var rtfMemoryStream = new MemoryStream())
            {
                textRange = new TextRange(richTextBlock.Document.ContentStart, richTextBlock.Document.ContentEnd);
                
                textRange.Save(rtfMemoryStream, DataFormats.Rtf);

                rtfMemoryStream.Seek(0, SeekOrigin.Begin);
                
                using (var rtfStreamReader = new StreamReader(rtfMemoryStream))
                {
                    return rtfStreamReader.ReadToEnd();
                }
            }
        }
    }
}