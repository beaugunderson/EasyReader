using Windows.Data.Json;

namespace ReadItLaterApi.Metro
{
    public class MediaItem
    {
        public string Type { get; set; }
        public string Url { get; set; }

        public bool Primary { get; set; }

        public MediaItem(JsonObject item)
        {
            Type = item.GetNamedString("type");
            Url = item.GetNamedString("link");

            Primary = !string.IsNullOrEmpty(item.GetNamedString("primary"));
        }
    }
}