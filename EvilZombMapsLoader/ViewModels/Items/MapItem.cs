using System;
using System.Windows.Media.Imaging;
using EvilZombMapsLoader.Helpers;
using Prism.Mvvm;

namespace EvilZombMapsLoader.ViewModels.Items
{
    public class MapItem : BindableBase
    {
        #region Fields

        private readonly string _imageUrl;
        private BitmapImage _image;

        #endregion

        #region Props

        public int Index { get; }

        public string Name { get; }

        public BitmapImage Image
        {
            get => _image;
            private set => SetProperty(ref _image, value);
        }
        
        #endregion

        public MapItem(int index, string name, string imageUrl)
        {
            Index = index;
            Name = name;
            _imageUrl = imageUrl;

            LoadImage();
        }

        public MapItem(int index, string name, BitmapImage image)
        {
            Index = index;
            Name = name;
            Image = image;
        }

        private async void LoadImage()
        {
            if (!string.IsNullOrWhiteSpace(_imageUrl))
            {
                Image = await ImageHelper.GetBitmapFromUrl(_imageUrl);
                if (Image == null)
                {
                    Image = LoadDefaultImage();
                    ImageError?.Invoke(this, EventArgs.Empty);
                    return;
                }

                ImageLoaded?.Invoke(this, Image);
            }
        }

        private BitmapImage LoadDefaultImage()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Img/nomap.png");
                var img = new BitmapImage(uri);
                img.Freeze();
                return img;
            }
            catch
            {
                return null;
            }
        }

        #region Events

        public event EventHandler<BitmapImage> ImageLoaded;
        public event EventHandler<EventArgs> ImageError;

        #endregion
    }
}
