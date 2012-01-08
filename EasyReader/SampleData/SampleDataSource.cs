using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Expression.Blend.SampleData.SampleDataSource
{
    using System;

    class SampleDataCollection : SampleDataItem, IGroupInfo
    {
        public Object Key
        {
            get { return this; }
        }

        private List<SampleDataItem> _itemCollection = new List<SampleDataItem>();

        public void Add(SampleDataItem item)
        {
            _itemCollection.Add(item);
        }

        public IEnumerator<Object> GetEnumerator()
        {
            return _itemCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class SampleDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private SampleDataCollection _collection;
        public SampleDataCollection Collection
        {
            get
            {
                return this._collection;
            }

            set
            {
                if (this._collection != value)
                {
                    this._collection = value;
                    this.OnPropertyChanged("Collection");
                }
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get
            {
                return this._title;
            }

            set
            {
                if (this._title != value)
                {
                    this._title = value;
                    this.OnPropertyChanged("Title");
                }
            }
        }

        private string _subtitle = string.Empty;
        public string Subtitle
        {
            get
            {
                return this._subtitle;
            }

            set
            {
                if (this._subtitle != value)
                {
                    this._subtitle = value;
                    this.OnPropertyChanged("Subtitle");
                }
            }
        }

        private ImageSource _image = null;
        private Uri _imageBaseUri = null;
        private String _imagePath = null;
        public ImageSource Image
        {
            get
            {
                if (_image == null && _imageBaseUri != null && _imagePath != null)
                {
                    _image = new BitmapImage(new Uri(_imageBaseUri, _imagePath));
                }
                return this._image;
            }

            set
            {
                if (this._image != value)
                {
                    this._image = value;
                    this._imageBaseUri = null;
                    this._imagePath = null;
                    this.OnPropertyChanged("Image");
                }
            }
        }

        public void SetImage(Uri baseUri, String path)
        {
            _image = null;
            _imageBaseUri = baseUri;
            _imagePath = path;
            this.OnPropertyChanged("Image");
        }

        private string _link = string.Empty;
        public string Link
        {
            get
            {
                return this._link;
            }

            set
            {
                if (this._link != value)
                {
                    this._link = value;
                    this.OnPropertyChanged("Link");
                }
            }
        }

        private string _category = string.Empty;
        public string Category
        {
            get
            {
                return this._category;
            }

            set
            {
                if (this._category != value)
                {
                    this._category = value;
                    this.OnPropertyChanged("Category");
                }
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get
            {
                return this._description;
            }

            set
            {
                if (this._description != value)
                {
                    this._description = value;
                    this.OnPropertyChanged("Description");
                }
            }
        }

        private string _content = string.Empty;
        public string Content
        {
            get
            {
                return this._content;
            }

            set
            {
                if (this._content != value)
                {
                    this._content = value;
                    this.OnPropertyChanged("Content");
                }
            }
        }
    }

    class SampleDataSource
    {
        public List<SampleDataCollection> GroupedCollections { get; private set; }

        private void AddCollection(String title, String subtitle, Uri baseUri, String imagePath, String link, String category, String description, String content)
        {
            var collection = new SampleDataCollection();

            collection.Title = title;
            collection.Subtitle = subtitle;
            collection.SetImage(baseUri, imagePath);
            collection.Link = link;
            collection.Category = category;
            collection.Description = description;
            collection.Content = content;
            
            GroupedCollections.Add(collection);
        }

        private void AddItem(String title, String subtitle, Uri baseUri, String imagePath, String link, String category, String description, String content)
        {
            SampleDataCollection lastCollection = GroupedCollections.LastOrDefault() as SampleDataCollection;

            var item = new SampleDataItem();
            
            item.Title = title;
            item.Subtitle = subtitle;
            item.SetImage(baseUri, imagePath);
            item.Link = link;
            item.Category = category;
            item.Description = description;
            item.Content = content;
            item.Collection = lastCollection;

            if (lastCollection != null)
            {
                lastCollection.Add(item);
            }
        }

        public SampleDataSource(Uri baseUri)
        {
            String LONG_LOREM_IPSUM = String.Format("{0}\n\n{0}\n\n{0}\n\n{0}",
                        "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");

            GroupedCollections = new List<SampleDataCollection>();

            AddCollection("Collection 1",
                    "Maecenas class nam praesent cras aenean mauris aliquam nullam aptent accumsan duis nunc curae donec integer auctor sed congue amet",
                    baseUri, "SampleData/Images/LightGray.png",
                    "http://www.adatum.com/",
                    "Pellentesque nam",
                    "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat",
                    LONG_LOREM_IPSUM);

            AddItem("Aliquam integer",
                    "Maecenas class nam praesent cras aenean mauris aliquam nullam aptent accumsan duis nunc curae donec integer auctor sed congue amet",
                    baseUri, "SampleData/Images/LightGray.png",
                    "http://www.adatum.com/",
                    "Pellentesque nam",
                    "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat",
                    LONG_LOREM_IPSUM);


            AddCollection("Collection 2",
                    "Senectus sem lacus erat sociosqu eros suscipit primis nibh nisi nisl gravida torquent",
                    baseUri, "SampleData/Images/MediumGray.png",
                    "http://www.baldwinmuseumofscience.com/",
                    "Phasellus duis",
                    "Est auctor inceptos congue interdum egestas scelerisque pellentesque fermentum ullamcorper cursus dictum lectus suspendisse condimentum libero vitae vestibulum lobortis ligula fringilla euismod class scelerisque feugiat habitasse diam litora adipiscing sollicitudin parturient hendrerit curae himenaeos imperdiet ullamcorper suspendisse nascetur hac gravida pharetra eget donec leo mus nec non malesuada vestibulum pellentesque elit penatibus vestibulum per condimentum porttitor sed adipiscing scelerisque ullamcorper etiam iaculis enim tincidunt erat parturient sem vestibulum eros",
                    LONG_LOREM_IPSUM);

            AddItem("Aenean mauris nullam cras",
                    "Senectus sem lacus erat sociosqu eros suscipit primis nibh nisi nisl gravida torquent",
                    baseUri, "SampleData/Images/MediumGray.png",
                    "http://www.baldwinmuseumofscience.com/",
                    "Phasellus duis",
                    "Est auctor inceptos congue interdum egestas scelerisque pellentesque fermentum ullamcorper cursus dictum lectus suspendisse condimentum libero vitae vestibulum lobortis ligula fringilla euismod class scelerisque feugiat habitasse diam litora adipiscing sollicitudin parturient hendrerit curae himenaeos imperdiet ullamcorper suspendisse nascetur hac gravida pharetra eget donec leo mus nec non malesuada vestibulum pellentesque elit penatibus vestibulum per condimentum porttitor sed adipiscing scelerisque ullamcorper etiam iaculis enim tincidunt erat parturient sem vestibulum eros",
                    LONG_LOREM_IPSUM);


            AddCollection("Collection 3",
                    "Class aliquam curae donec etiam integer quisque nam maecenas cras vivamus duis sed dis aliquam aliquet praesent fusce",
                    baseUri, "SampleData/Images/DarkGray.png",
                    "http://www.consolidatedmessenger.com/",
                    "Condimentum convallis",
                    "Imperdiet litora erat netus pellentesque egestas vestibulum euismod eros nibh nulla nisi porta feugiat scelerisque purus est sollicitudin luctus hac nisl ullamcorper vestibulum adipiscing magnis malesuada mattis mauris suspendisse suscipit leo parturient torquent vestibulum gravida pellentesque mollis consectetuer condimentum iaculis risus penatibus pellentesque lacinia ultrices vestibulum vehicula mus laoreet nunc velit porttitor tincidunt volutpat nec adipiscing tristique natoque scelerisque montes odio nostra maecenas ultricies non praesent venenatis accumsan per ullamcorper sollicitudin orci pede suspendisse condimentum posuere scelerisque ornare quam vulputate sed platea pellentesque parturient primis rutrum quis sem vestibulum curabitur sapien",
                    LONG_LOREM_IPSUM);

            AddItem("Maecenas aliquam class",
                    "Class aliquam curae donec etiam integer quisque nam maecenas cras vivamus duis sed dis aliquam aliquet praesent fusce",
                    baseUri, "SampleData/Images/DarkGray.png",
                    "http://www.consolidatedmessenger.com/",
                    "Condimentum convallis",
                    "Imperdiet litora erat netus pellentesque egestas vestibulum euismod eros nibh nulla nisi porta feugiat scelerisque purus est sollicitudin luctus hac nisl ullamcorper vestibulum adipiscing magnis malesuada mattis mauris suspendisse suscipit leo parturient torquent vestibulum gravida pellentesque mollis consectetuer condimentum iaculis risus penatibus pellentesque lacinia ultrices vestibulum vehicula mus laoreet nunc velit porttitor tincidunt volutpat nec adipiscing tristique natoque scelerisque montes odio nostra maecenas ultricies non praesent venenatis accumsan per ullamcorper sollicitudin orci pede suspendisse condimentum posuere scelerisque ornare quam vulputate sed platea pellentesque parturient primis rutrum quis sem vestibulum curabitur sapien",
                    LONG_LOREM_IPSUM);
        }
    }
}
