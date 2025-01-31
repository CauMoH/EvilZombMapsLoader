using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using EvilZombMapsLoader.Enums;
using EvilZombMapsLoader.Helpers;
using EvilZombMapsLoader.ViewModels.Items;
using EvilZombMapsLoader.Xml;
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

        private const string MapsXmlFileName = "data.xml";
        private const string MapImagesFolder = "images";
        private const string ImageExtension = ".png";

        private List<MapXmlItem> _mapsInfos = new List<MapXmlItem>();

        private readonly object _locker = new object();

        #endregion

        #region Props

        /// <summary>
        /// Название программы из Assembly
        /// </summary>
        private static string GetName => (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()).GetCustomAttribute<AssemblyTitleAttribute>().Title;

        /// <summary>
        /// Путь к рабочей папке приложения
        /// </summary>
        public static string AppDataFolderPath
        {
            get
            {
                var rootFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(rootFolder, GetName);
            }
        }

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

        private static string MapsXmlFilePath => GetFilePath(MapsXmlFileName);

        #endregion

        public MainViewModel()
        {
            InitCommands();

            LoadData();
        }

        #region Private Methods

        private void LoadData()
        {
            try
            {
                Directory.CreateDirectory(GetMapFolderPath());

                if (File.Exists(MapsXmlFilePath))
                {
                    XmlParser.Parse<MapRoot>(MapsXmlFilePath, out var config);
                    _mapsInfos = config.Maps;
                }
            }
            catch
            {
                //ignore
            }
        }

        private void SaveData()
        {
            try
            {
                lock (_locker)
                {
                    var config = new MapRoot
                    {
                        Maps = _mapsInfos
                    };

                    XmlParser.Save(MapsXmlFilePath, config);
                }
            }
            catch
            {
                //ignore
            }
        }

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

                        var index1 = index;

                        var mapInfo = _mapsInfos.FirstOrDefault(mi => mi.MapName == mapName);
                        if (mapInfo != null && mapInfo.ImageLoaded && GetMapImage(mapName, out var image))
                        {
                            UiInvoker.Invoke(() =>
                            {
                                Maps.Add(new MapItem(index1 + 1, mapName, image));
                            });
                        }
                        else
                        {
                            var mapPage = BaseUrlStr +
                                          row.GetAttributeValue("href", string.Empty).Replace("amp;", string.Empty);
                            var mapDoc = _htmlWeb.Load(mapPage);
                            var imageNode = mapDoc.DocumentNode.SelectSingleNode($"//img[@alt='{mapName}']");
                            var imageUrl = string.Empty;
                            if (imageNode != null)
                            {
                                imageUrl = BaseUrlStr + imageNode.GetAttributeValue("src", string.Empty).TrimStart('.');
                            }

                            var defaultImage = false;

                            if (imageUrl.Contains(NoMapImage))
                            {
                                defaultImage = true;

                                UiInvoker.Invoke(() =>
                                {
                                    MapsWithoutImages++;
                                });
                            }
                            
                            UiInvoker.Invoke(() =>
                            {
                                var newMap = new MapItem(index1 + 1, mapName, imageUrl, defaultImage);
                                newMap.ImageLoaded += NewMap_OnImageLoaded;
                                Maps.Add(newMap);
                            });
                        }
                        
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
        
        /// <summary>
        /// Получить полный путь к файлу
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        private static string GetFilePath(string fileName)
        {
            return Path.Combine(AppDataFolderPath, fileName);
        }

        private static string GetMpaImageFilePath(string mapName)
        {
            return GetFilePath(Path.Combine(MapImagesFolder, mapName + ImageExtension));
        }

        private static string GetMapFolderPath()
        {
            return Path.Combine(AppDataFolderPath, MapImagesFolder);
        }

        private static bool GetMapImage(string mapName, out BitmapImage image)
        {
            image = null;
            try
            {
                var mapImageFilePath = GetMpaImageFilePath(mapName);
                if (File.Exists(mapImageFilePath))
                {
                    image = new BitmapImage();
                    using (var stream = new FileStream(mapImageFilePath, FileMode.Open, FileAccess.Read))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                    }
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void SaveMapImage(MapItem mapItem)
        {
            try
            {
                if (mapItem.Image == null || mapItem.DefaultImage)
                {
                    return;
                }

                var encoder = new PngBitmapEncoder(); // Можно поменять на PngBitmapEncoder
                encoder.Frames.Add(BitmapFrame.Create(mapItem.Image));

                var mapImageFilePath = GetMpaImageFilePath(mapItem.Name);
                
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    var data = stream.ToArray();
                    File.WriteAllBytes(mapImageFilePath, data);
                }

                var existMapInfo = _mapsInfos.FirstOrDefault(mi => mi.MapName == mapItem.Name);
                if (existMapInfo != null)
                {
                    existMapInfo.ImageLoaded = true;
                }
                else
                {
                    _mapsInfos.Add(new MapXmlItem
                    {
                        MapName = mapItem.Name,
                        ImageLoaded = true
                    });
                }

                SaveData();
            }
            catch
            {
                //ignore
            }
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

        private void NewMap_OnImageLoaded(object sender, BitmapImage e)
        {
            try
            {
                if (sender is MapItem mapItem)
                {
                    mapItem.ImageLoaded -= NewMap_OnImageLoaded;
                    SaveMapImage(mapItem);
                }
            }
            catch
            {
                //ignore
            }
        }

        #endregion

        #region Commands

        private void InitCommands()
        {
            ChangeLoadProcessCommand = new DelegateCommand<LoadProcessStates?>(ChangeLoadProcessExecute);
            DeleteSavedDataCommand = new DelegateCommand(DeleteSavedDataExecute);
        }

        #region Props

        public ICommand ChangeLoadProcessCommand { get; private set; }

        public ICommand DeleteSavedDataCommand { get; private set; }

        #endregion

        #region Executes

        private void ChangeLoadProcessExecute(LoadProcessStates? newState)
        {
            if (newState == null)
                return;

            CurrentState = newState.Value;
        }

        private void DeleteSavedDataExecute()
        {
            try
            {
                _mapsInfos.Clear();
                var imageFolderPath = GetMapFolderPath();
                if (Directory.Exists(imageFolderPath))
                {
                    foreach (var file in Directory.GetFiles(imageFolderPath))
                    {
                        File.Delete(file);
                    }
                }
                SaveData();
            }
            catch
            {
                //ignore
            }
        }

        #endregion

        #endregion
    }
}
