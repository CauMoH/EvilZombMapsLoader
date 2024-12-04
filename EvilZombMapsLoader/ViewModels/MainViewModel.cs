using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EvilZombMapsLoader.ViewModels.Items;
using HtmlAgilityPack;
using Prism.Commands;
using Prism.Mvvm;

namespace EvilZombMapsLoader.ViewModels
{
    public class MainViewModel : BindableBase
    {
        #region Fields

        private const string PhpPath = "/hlstats.php?";
        private const string BaseUrlStr = "https://evilzomb.myarena.site";
        private const string MapsPageStr = "mode=maps&game=cstrike";
        private const string NoMapImage = "nomap.png";
        private readonly List<string> _ignoreMaps = new List<string>
        {
            "(Unaccounted)"
        };
        private bool _canLoad = true;
        private int _mapsCounter;
        private int _noMapImageCounter;
        private readonly HtmlWeb _htmlWeb = new HtmlWeb();

        #endregion

        #region Props

        public string Title
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyVersion = assembly.GetName().Version;
                return string.Format(Localization.strings.MainTitle, assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
            }
        }

        public bool CanLoad
        {
            get => _canLoad;
            set => SetProperty(ref _canLoad, value);
        }

        public int NoMapImageCounter
        {
            get => _noMapImageCounter;
            private set => SetProperty(ref _noMapImageCounter, value);
        }

        public int MapsCounter
        {
            get => _mapsCounter;
            private set => SetProperty(ref _mapsCounter, value);
        }

        public ObservableCollection<MapItem> Maps { get; } = new ObservableCollection<MapItem>();

        #endregion

        public MainViewModel()
        {
            InitCommands();
        }

        #region Private Methods

        private async void Load()
        {
            Maps.Clear();
            MapsCounter = 0;
            NoMapImageCounter = 0;
            CanLoad = false;

            await Task.Run(() =>
            {
                try
                {
                    const string mapUrl = BaseUrlStr + PhpPath + MapsPageStr;
                    var mapsDoc = _htmlWeb.Load(mapUrl);

                    var rows = mapsDoc.DocumentNode.SelectNodes("//table[@class='data-table']//tr//td[@class='bg2']//a");

                    for (var index = 0; index < rows.Count; index++)
                    {
                        var row = rows[index];
                        var mapName = row.InnerText;

                        var isIgnore = _ignoreMaps.Any(ignoreMap => mapName.Contains(ignoreMap));
                        if (isIgnore)
                            continue;

                        var mapPage = BaseUrlStr +
                                      row.GetAttributeValue("href", string.Empty).Replace("amp;", string.Empty);
                        var mapDoc = _htmlWeb.Load(mapPage);
                        var imageNode = mapDoc.DocumentNode.SelectSingleNode($"//img[@alt='{mapName}']");
                        var imageUrl = string.Empty;
                        if (imageNode != null)
                        {
                            imageUrl = BaseUrlStr + imageNode.GetAttributeValue("src", string.Empty).TrimStart('.');
                        }

                        Application.Current.Dispatcher.Invoke(() => { MapsCounter++; });

                        if (imageUrl.Contains(NoMapImage))
                        {
                            Application.Current.Dispatcher.Invoke(() => { NoMapImageCounter++; });
                        }

                        var index1 = index;
                        Application.Current.Dispatcher.Invoke(() => { Maps.Add(new MapItem(index1 + 1, mapName, imageUrl)); });
                    }
                }
                catch
                {
                    //ignore   
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CanLoad = true;
                    });
                }
            });
        }

        #endregion

        #region Commands

        private void InitCommands()
        {
            LoadMapsCommand = new DelegateCommand(LoadMapsExecute);
        }

        #region Props

        public ICommand LoadMapsCommand { get; private set; }

        #endregion

        #region Executes

        private void LoadMapsExecute()
        {
            Load();
        }

        #endregion

        #endregion


    }
}
