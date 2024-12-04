using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using EvilZombMapsLoader.Enums;
using EvilZombMapsLoader.Helpers;
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
        private int _mapsWithoutImages;
        private readonly HtmlWeb _htmlWeb = new HtmlWeb();

        private LoadProcessStates _currentState = LoadProcessStates.Unknown;

        private int _numberMapsToDownload = 1;

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

        public int NumberMapsToDownload
        {
            get => _numberMapsToDownload;
            set => SetProperty(ref _numberMapsToDownload, value);
        }
        
        public int MapsWithoutImages
        {
            get => _mapsWithoutImages;
            private set => SetProperty(ref _mapsWithoutImages, value);
        }
        
        public ObservableCollection<MapItem> Maps { get; } = new ObservableCollection<MapItem>();

        public LoadProcessStates CurrentState
        {
            get => _currentState;
            private set => SetProperty(ref _currentState, value, State_OnChanged);
        }
        
        #endregion

        public MainViewModel()
        {
            InitCommands();
        }

        #region Private Methods

        private async void Load()
        {
            Maps.Clear();
            MapsWithoutImages = 0;
            NumberMapsToDownload = 1;
            CurrentState = LoadProcessStates.Loading;

            await Task.Run(() =>
            {
                try
                {
                    const string mapUrl = BaseUrlStr + PhpPath + MapsPageStr;
                    var mapsDoc = _htmlWeb.Load(mapUrl);

                    var rows = mapsDoc.DocumentNode.SelectNodes("//table[@class='data-table']//tr//td[@class='bg2']//a");

                    UiInvoker.Invoke(() =>
                    {
                        NumberMapsToDownload = rows.Count;
                    });

                    for (var index = 0; index < rows.Count; index++)
                    {
                        if (CurrentState == LoadProcessStates.Cancel)
                        {
                            UiInvoker.Invoke(() =>
                            {
                                CurrentState = LoadProcessStates.Canceled;
                            });

                            return;
                        }

                        var row = rows[index];
                        var mapName = row.InnerText;

                        var isIgnore = _ignoreMaps.Any(ignoreMap => mapName.Contains(ignoreMap));
                        if (isIgnore)
                        {
                            UiInvoker.Invoke(() =>
                            {
                                NumberMapsToDownload--;
                            });

                            continue;
                        }

                        var mapPage = BaseUrlStr +
                                      row.GetAttributeValue("href", string.Empty).Replace("amp;", string.Empty);
                        var mapDoc = _htmlWeb.Load(mapPage);
                        var imageNode = mapDoc.DocumentNode.SelectSingleNode($"//img[@alt='{mapName}']");
                        var imageUrl = string.Empty;
                        if (imageNode != null)
                        {
                            imageUrl = BaseUrlStr + imageNode.GetAttributeValue("src", string.Empty).TrimStart('.');
                        }
                        
                        if (imageUrl.Contains(NoMapImage))
                        {
                            UiInvoker.Invoke(() =>
                            {
                                MapsWithoutImages++;
                            });
                        }

                        var index1 = index;
                        UiInvoker.Invoke(() =>
                        {
                            Maps.Add(new MapItem(index1 + 1, mapName, imageUrl));
                        });
                    }

                    UiInvoker.Invoke(() =>
                    {
                        CurrentState = LoadProcessStates.Loaded;
                    });
                }
                catch
                {
                    UiInvoker.Invoke(() =>
                    {
                        CurrentState = LoadProcessStates.LoadedWithError;
                    });
                }
            });
        }

        #endregion

        #region Event Handlers

        private void State_OnChanged()
        {
            switch (CurrentState)
            {
                case LoadProcessStates.ReadyToLoading:
                    Load();
                    break;
            }
        }

        #endregion

        #region Commands

        private void InitCommands()
        {
            ChangeLoadProcessCommand = new DelegateCommand<LoadProcessStates?>(ChangeLoadProcessExecute);
        }

        #region Props

        public ICommand ChangeLoadProcessCommand { get; private set; }

        #endregion

        #region Executes

        private void ChangeLoadProcessExecute(LoadProcessStates? newState)
        {
            if (newState == null)
                return;

            CurrentState = newState.Value;
        }

        #endregion

        #endregion
    }
}
