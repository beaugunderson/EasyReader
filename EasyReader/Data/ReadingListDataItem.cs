using System;
using System.ComponentModel;

using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace EasyReader.Data
{
    public class ReadingListDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _title = string.Empty;

        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                if (_title != value)
                {
                    _title = value;

                    OnPropertyChanged("Title");
                }
            }
        }

        private string _subtitle = string.Empty;

        public string Subtitle
        {
            get
            {
                return _subtitle;
            }

            set
            {
                if (_subtitle != value)
                {
                    _subtitle = value;

                    OnPropertyChanged("Subtitle");
                }
            }
        }

        private ImageSource _image;
        private Uri _imageBaseUri;
        private String _imagePath;

        public ImageSource Image
        {
            get
            {
                if (_image == null && _imageBaseUri != null && _imagePath != null)
                {
                    _image = new BitmapImage(new Uri(_imageBaseUri, _imagePath));
                }
                else if (_image == null && _imagePath != null)
                {
                    _image = new BitmapImage(new Uri(_imagePath));
                }

                return _image;
            }

            set
            {
                if (_image != value)
                {
                    _image = value;
                    _imageBaseUri = null;
                    _imagePath = null;
                
                    OnPropertyChanged("Image");
                }
            }
        }

        public string ImageUri
        {
            get 
            { 
                return _imagePath;
            }
        }

        public void SetImage(String path)
        {
            _image = null;

            _imagePath = path;
            
            OnPropertyChanged("Image");
        }

        public void SetImage(Uri baseUri, String path)
        {
            _image = null;

            _imageBaseUri = baseUri;
            _imagePath = path;
            
            OnPropertyChanged("Image");
        }

        private string _link = string.Empty;

        public string Link
        {
            get
            {
                return _link;
            }

            set
            {
                if (_link != value)
                {
                    _link = value;

                    OnPropertyChanged("Link");
                }
            }
        }

        private string _category = string.Empty;

        public string Category
        {
            get
            {
                return _category;
            }

            set
            {
                if (_category != value)
                {
                    _category = value;

                    OnPropertyChanged("Category");
                }
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (_description != value)
                {
                    _description = value;

                    OnPropertyChanged("Description");
                }
            }
        }

        private string _content = string.Empty;

        public string Content
        {
            get
            {
                return _content;
            }

            set
            {
                if (_content != value)
                {
                    _content = value;

                    OnPropertyChanged("Content");
                }
            }
        }
    }
}