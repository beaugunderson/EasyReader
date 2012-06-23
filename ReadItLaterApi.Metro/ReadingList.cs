using System;
using System.Collections.Generic;
using System.Diagnostics;

using Windows.Data.Json;

namespace ReadItLaterApi.Metro
{
    public class ReadingList
    {
        public int Complete { get; set; }
        public int Status { get; set; }

        public long Since { get; set; }

        public Dictionary<string, ReadingListItem> List { get; set; }

        private readonly JsonObject _json;

        public ReadingList(string json)
        {
            _json = JsonObject.Parse(json);

            Status = (int)_json.GetNamedNumber("status");
            Since = (long)_json.GetNamedNumber("since");
            Complete = (int)_json.GetNamedNumber("complete");

            List = new Dictionary<string, ReadingListItem>();

            // XXX: This throws if 'list: []' (i.e. an empty array)
            // Microsoft bug?
            try
            {
                var list = _json.GetNamedObject("list");

                foreach (var key in list.Keys)
                {
                    var item = list.GetNamedObject(key);

                    List.Add(key, new ReadingListItem(item));
                }
            }
            catch
            {
                Debug.WriteLine("Error retrieving the list property.");
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