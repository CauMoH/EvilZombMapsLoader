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

        public bool DefaultImage { get; private set; }

        #endregion

        public MapItem(int index, string name, string imageUrl, bool defaultImage = false)
        {
            Index = index;
            Name = name;
            _imageUrl = imageUrl;
            DefaultImage = defaultImage;

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
                    DefaultImage = true;
                    return;
                }

                ImageLoaded?.Invoke(this, Image);
            }
        }

        #region Events

        public event EventHandler<BitmapImage> ImageLoaded;

        #endregion
    }
}
