using System;
using System.Collections.ObjectModel;
using Windows.Data.Json;

namespace ReadItLaterApi.Metro
{
    public class DiffbotArticle
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }
        public string Html { get; set; }

        public Collection<string> Tags { get; set; }
        public Collection<MediaItem> Media { get; set; }

        private readonly JsonObject _json;

        private string tryGetNamedString(string key)
        {
            try
            {
                return _json.GetNamedString(key);
            } 
            catch (Exception)
            {
                return "";
            }
        }

        public DiffbotArticle(string json)
        {
            _json = JsonObject.Parse(json);

            Title = tryGetNamedString("title");
            Author = tryGetNamedString("author");
            Date = tryGetNamedString("date");
            Url = tryGetNamedString("url");
            Html = tryGetNamedString("html");

            Tags = new Collection<string>();
            Media = new Collection<MediaItem>();

            try
            {
                var media = _json.GetNamedArray("media");

                foreach (var item in media)
                {
                    Media.Add(new MediaItem(item.GetObject()));
                }
            } 
            catch (Exception)
            {
                
            }

            try
            {
                var tags = _json.GetNamedArray("tags");

                foreach (var item in tags)
                {
                    Tags.Add(item.GetString());
                }
            }
            catch (Exception)
            {
                
            }
        }

        public string Stringify()
        {
            if (_json != null)
            {
                return _json.Stringify();
            }

            throw new NotImplementedException();
        }
    }
}