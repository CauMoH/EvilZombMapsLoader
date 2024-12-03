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

        public string Name { get; }

        public BitmapImage Image
        {
            get => _image;
            private set => SetProperty(ref _image, value);
        }

        #endregion

        public MapItem(string name, string imageUrl)
        {
            Name = name;
            _imageUrl = imageUrl;

            LoadImage();
        }

        private async void LoadImage()
        {
            if (!string.IsNullOrWhiteSpace(_imageUrl))
            {
                Image = await ImageHelper.GetBitmapFromUrl(_imageUrl);
            }
        }
    }
}
