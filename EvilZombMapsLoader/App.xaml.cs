using System.Windows;
using EvilZombMapsLoader.ViewModels;

namespace EvilZombMapsLoader
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindow _mainWindow;
        private readonly MainViewModel _mainViewModel = new MainViewModel();

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            _mainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };

            Current.MainWindow = _mainWindow;
            Current.MainWindow.Show();
        }
    }
}
