using System.Collections.Generic;

using Windows.Data.Json;

namespace ReadItLaterApi.Metro
{
    public class ReadingList
    {
        public int Complete { get; set; }
        public int Status { get; set; }

        public long Since { get; set; }

        public Dictionary<string, ReadingListItem> List { get; set; }

        public ReadingList(JsonObject json)
        {
            Status = (int)json.GetNamedNumber("status");
            Since = (long)json.GetNamedNumber("since");
            Complete = (int)json.GetNamedNumber("complete");

            List = new Dictionary<string, ReadingListItem>();

            var list = json.GetNamedObject("list");

            foreach (var key in list.Keys)
            {
                var item = list.GetNamedObject(key);

                List.Add(key, new ReadingListItem(item));
            }
        }
    }
}