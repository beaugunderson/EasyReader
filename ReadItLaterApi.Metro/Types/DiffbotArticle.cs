using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Data.Json;

namespace ReadItLaterApi.Metro.Types
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

        public DiffbotArticle(string jsonString)
        {
            JsonObject.TryParse(jsonString, out _json);

            if (_json == null)
            {
                return;
            }

            Title = tryGetNamedString("title");

            // Prevent needless exceptions because we don't care about entries without a title
            if (String.IsNullOrWhiteSpace(Title))
            {
                return;
            }

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
                Debug.WriteLine("Caught exception in media JSON");
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
                Debug.WriteLine("Caught exception in tags JSON");
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