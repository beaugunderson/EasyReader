﻿using System;
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

        private JsonObject _json;

        public ReadingList(JsonObject json)
        {
            _json = json;

            Status = (int)json.GetNamedNumber("status");
            Since = (long)json.GetNamedNumber("since");
            Complete = (int)json.GetNamedNumber("complete");

            List = new Dictionary<string, ReadingListItem>();

            // XXX: This throws if 'list: []' (i.e. an empty array) // Microsoft bug?
            try
            {
                var list = json.GetNamedObject("list");

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