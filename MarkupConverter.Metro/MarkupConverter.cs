namespace MarkupConverter.Metro
{
    public interface IMarkupConverter
    {
        string ConvertHtmlToXaml(string htmlText);
    }

    public class MarkupConverter : IMarkupConverter
    {
        public string ConvertHtmlToXaml(string htmlText)
        {
            return HtmlToXamlConverter.ConvertHtmlToXaml(htmlText, true);
        }
    }
}