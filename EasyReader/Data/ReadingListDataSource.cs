using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

using EasyReader.Hacks;

using ReadItLaterApi.Metro.Types;

namespace EasyReader.Data
{
    // TODO: Add INotifyPropertyChanged
    public class ReadingListDataSource
    {
        public ObservableVector<object> Items { get; private set; }
        
        public ReadItLaterApi.Metro.ReadItLaterApi ReadItLaterApi { get; set; }

        private const bool DEMO = true;

        // TODO: Convert to bindings (OnPropertyChanged)
        private bool _isUpdating;
        public bool IsUpdating
        { 
            get
            {
                return _isUpdating;
            }
            
            set
            {
                _isUpdating = value; 
            
                if (UpdatingStatusChanged != null)
                {
                    UpdatingStatusChanged(this, new EventArgs());
                }
            }
        }

        private readonly ApplicationData _applicationData = ApplicationData.Current;

        public EventHandler UpdatingStatusChanged; 

        public bool CheckCredentials()
        {
            var roamingSettings = _applicationData.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("username") &&
                roamingSettings.Values.ContainsKey("password"))
            {
                var username = roamingSettings.Values["username"] as string;
                var password = roamingSettings.Values["password"] as string;

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    Debug.WriteLine("No credentials.");

                    return false;
                }
                
                ReadItLaterApi = new ReadItLaterApi.Metro.ReadItLaterApi(username, password);

                return true;
            }

            Debug.WriteLine("No credentials.");

            return false;
        }

        public async Task UpdateReadingList()
        {
            Debug.WriteLine("UpdateReadingList()");

            if (IsUpdating)
            {
                Debug.WriteLine("Cancelling UpdateReadingList(), already running.");

                return;
            }

            if (!CheckCredentials())
            {
                Debug.WriteLine("Cancelling UpdateReadingList(), no credentials found.");

                return;
            }

            IsUpdating = true;

            // XXX: Probably too broad
            try
            {
                await ReadItemsFromDiskWithRemoteFallback();
            } 
            catch(Exception)
            {
                Debug.WriteLine("Caught exception from ReadItemsFromDiskWithRemoteFallback");    
            }

            IsUpdating = false;
        }

        #region Data retrieval methods
        public async Task WriteItemToFile(StorageFolder folder, string filename, string data)
        {
            var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, data);
        }

        public async Task<string> ReadItemFromFile(StorageFolder folder, string filename)
        {
            Debug.WriteLine("Reading from '{0}'", filename);

            try
            {
                var file = await folder.GetFileAsync(filename);

                var contents = await FileIO.ReadTextAsync(file);

                Debug.WriteLine("'{0}' characters: {1}", filename, contents.Length);

                return contents;
            } 
            catch (FileNotFoundException)
            {
                Debug.WriteLine("File '{0}' was null!", filename);

                return null;
            }
            catch (Exception)
            {
                Debug.WriteLine("Other exception from ReadItemFromFile");

                return null;
            }
        }

        private async Task<DiffbotArticle> GetRemoteTextFromListItem(KeyValuePair<string, ReadingListItem> item)
        {
            Debug.WriteLine(string.Format("Getting remote text for '{0}'", item.Key));

            var diffbotArticle = await ReadItLaterApi.GetText(item.Value);

            if (String.IsNullOrEmpty(diffbotArticle.Title))
            {
                return null;
            }

            await WriteItemToFile(_applicationData.LocalFolder, string.Format("{0}.json", item.Key), diffbotArticle.Stringify());

            return diffbotArticle;
        }

        private async Task<DiffbotArticle> ReadItemFromDiskWithRemoteFallback(StorageFolder folder, KeyValuePair<string, ReadingListItem> item)
        {
            Debug.WriteLine(folder.Path);

            var json = await ReadItemFromFile(folder, string.Format("{0}.json", item.Key));

            if (String.IsNullOrEmpty(json))
            {
                Debug.WriteLine("'{0}' wasn't cached, retrieving it from the Internet", item.Key);

                if (App.HasConnectivity && !DEMO)
                {
                    return await GetRemoteTextFromListItem(item);
                }

                return null;
            }

            return new DiffbotArticle(json);
        }

        private async Task ReadItemsFromDiskWithRemoteFallback()
        {
            var readingListJson = await ReadItemFromFile(_applicationData.LocalFolder, "reading-list.json");

            ReadingList list;

            if (readingListJson == null) {
                if (App.HasConnectivity)
                {
                    // Get the reading list from Pocket
                    list = await ReadItLaterApi.GetReadingList();

                    // Save the reading list for future use
                    await WriteItemToFile(_applicationData.LocalFolder, "reading-list", list.Stringify());
                }
                else
                {
                    Debug.WriteLine("There was no cached list and we don't currently have Internet connectivity, aborting");

                    return;
                }
            }
            else
            {
                list = new ReadingList(readingListJson);
            }

            foreach (var item in list.List)
            {
                if (DEMO && Items.Count > 75)
                {
                    break;
                }

                Debug.WriteLine("Trying to read '{0}' from disk with remote fallback", item.Key);

                var diffbotArticle = await ReadItemFromDiskWithRemoteFallback(_applicationData.LocalFolder, item);

                if (diffbotArticle == null)
                {
                    continue;
                }

                AddItem(item.Value, diffbotArticle);
            }
        }
        #endregion

        public void AddItem(ReadingListItem item, DiffbotArticle article)
        {
            string url = "";

            try
            {
                foreach (var image in article.Media.Where(image => image.Primary && image.Type == "image"))
                {
                    url = image.Url;
                }

                if (String.IsNullOrEmpty(url))
                {
                    foreach (var image in article.Media.Where(image => image.Type == "image"))
                    {
                        url = image.Url;
                    }
                }

                if (String.IsNullOrEmpty(url))
                {
                    return;
                }
            } 
            catch (Exception)
            {
                Debug.WriteLine("Caught exception in AddItem()");   
            }

            AddItem(article.Title,
                "",
                "",
                url,
                article.Url,
                "",
                "",
                article.Html);
        }

        public void AddItem(ReadingListItem item, string text)
        {
            AddItem(item.Title,
                "",
                "SampleData/Images/LightGray.png",
                "",
                item.Url,
                "",
                "",
                text);
        }

        public void AddItem(String title, String subtitle, String baseUri, String imagePath, String link, String category, String description, String content)
        {
            var matches = Items.Any(x => ((ReadingListDataItem)x).Link == link);

            if (matches)
            {
                return;
            }

            var item = new ReadingListDataItem
            {
                Title = title,
                Subtitle = subtitle,
                Link = link,
                Category = category,
                Description = description,
                Content = content
            };

            if (!string.IsNullOrWhiteSpace(baseUri) && 
                !string.IsNullOrWhiteSpace(imagePath))
            {
                item.SetImage(new Uri(baseUri), imagePath);
            }
            else if (!string.IsNullOrWhiteSpace(imagePath))
            {
                item.SetImage(imagePath);
            }
            
            Items.Add(item);
        }

        public ReadingListDataSource()
        {
            var observableCollection = new ObservableCollection<object>();

            Items = observableCollection.ToObservableVector<object>();
        }
    }
}