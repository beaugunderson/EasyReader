using System;
using System.Collections.ObjectModel;
using System.Linq;

using EasyReader.Hacks;

using ReadItLaterApi.Metro;

namespace EasyReader.Data
{
    class ReadingListDataSource
    {
        public ObservableVector<object> Items { get; private set; }

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

            String LONG_LOREM_IPSUM = String.Format("{0}\n\n{0}\n\n{0}\n\n{0}",
                "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");

            //AddItem("Aliquam integer",
            //        "Maecenas class nam praesent cras aenean mauris aliquam nullam aptent accumsan duis nunc curae donec integer auctor sed congue amet",
            //        "", "SampleData/Images/LightGray.png",
            //        "http://www.adatum.com/",
            //        "Pellentesque nam",
            //        "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat",
            //        LONG_LOREM_IPSUM);

            //AddItem("Aenean mauris nullam cras",
            //        "Senectus sem lacus erat sociosqu eros suscipit primis nibh nisi nisl gravida torquent",
            //        "", "SampleData/Images/MediumGray.png",
            //        "http://www.baldwinmuseumofscience.com/",
            //        "Phasellus duis",
            //        "Est auctor inceptos congue interdum egestas scelerisque pellentesque fermentum ullamcorper cursus dictum lectus suspendisse condimentum libero vitae vestibulum lobortis ligula fringilla euismod class scelerisque feugiat habitasse diam litora adipiscing sollicitudin parturient hendrerit curae himenaeos imperdiet ullamcorper suspendisse nascetur hac gravida pharetra eget donec leo mus nec non malesuada vestibulum pellentesque elit penatibus vestibulum per condimentum porttitor sed adipiscing scelerisque ullamcorper etiam iaculis enim tincidunt erat parturient sem vestibulum eros",
            //        LONG_LOREM_IPSUM);

            //AddItem("Maecenas aliquam class",
            //        "Class aliquam curae donec etiam integer quisque nam maecenas cras vivamus duis sed dis aliquam aliquet praesent fusce",
            //        "", "SampleData/Images/DarkGray.png",
            //        "http://www.consolidatedmessenger.com/",
            //        "Condimentum convallis",
            //        "Imperdiet litora erat netus pellentesque egestas vestibulum euismod eros nibh nulla nisi porta feugiat scelerisque purus est sollicitudin luctus hac nisl ullamcorper vestibulum adipiscing magnis malesuada mattis mauris suspendisse suscipit leo parturient torquent vestibulum gravida pellentesque mollis consectetuer condimentum iaculis risus penatibus pellentesque lacinia ultrices vestibulum vehicula mus laoreet nunc velit porttitor tincidunt volutpat nec adipiscing tristique natoque scelerisque montes odio nostra maecenas ultricies non praesent venenatis accumsan per ullamcorper sollicitudin orci pede suspendisse condimentum posuere scelerisque ornare quam vulputate sed platea pellentesque parturient primis rutrum quis sem vestibulum curabitur sapien",
            //        LONG_LOREM_IPSUM);
        }
    }
}