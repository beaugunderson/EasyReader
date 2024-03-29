﻿using System;
using System.Diagnostics;
using Windows.Data.Json;

namespace ReadItLaterApi.Metro.Types
{
    public class ReadingListItem
    {
        public long ItemId { get; set; }

        public int State { get; set; }

        public long TimeAdded { get; set; }
        public long TimeUpdated { get; set; }

        public string Title { get; set; }
        public string Url { get; set; }

        public ReadingListItem(JsonObject json)
        {
            // Set defaults
            long itemId;
            
            long timeAdded;
            long timeUpdated;

            int state;

            // Try parsing the strings into numbers
            long.TryParse(json.GetNamedString("item_id"), out itemId);

            long.TryParse(json.GetNamedString("time_added"), out timeAdded);
            long.TryParse(json.GetNamedString("time_updated"), out timeUpdated);

            int.TryParse(json.GetNamedString("state"), out state);

            // Construct the object
            ItemId = itemId;

            State = state;

            TimeAdded = timeAdded;
            TimeUpdated = timeUpdated;

            try
            {
                Title = json.GetNamedString("title");
                Url = json.GetNamedString("url");
            }
            catch (Exception)
            {
                Debug.WriteLine("Caught exception in ReadingListItem()");
            }
        }
    }
}