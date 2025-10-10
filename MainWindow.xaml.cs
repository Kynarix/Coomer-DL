using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CoomerDownloader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly CoomerDownloader.Services.CoomerApiService _apiService;
    private readonly CoomerDownloader.Services.FavoritesManager _favoritesManager;
    private bool _showingFavorites = false;

    public MainWindow()
    {
        InitializeComponent();
        _apiService = new CoomerDownloader.Services.CoomerApiService();
        _favoritesManager = new CoomerDownloader.Services.FavoritesManager();
        CreatorsListView.MouseDoubleClick += CreatorsListView_MouseDoubleClick;
        CreatorsListView.ContextMenuOpening += CreatorsListView_ContextMenuOpening;
        
        // Başlangıçta bağlantı durumunu kontrol et
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckConnectionStatus();
    }

    private async Task CheckConnectionStatus()
    {
        try
        {
            ConnectionStatusIcon.Text = "🔄";
            ConnectionStatusText.Text = "Checking...";
            ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8B949E"));
            
            var status = await _apiService.CheckConnectionStatus();
            
            if (status.IsConnected)
            {
                // Bağlantı başarılı - Yeşil
                ConnectionStatusIcon.Text = "✅";
                ConnectionStatusText.Text = "Connected";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3FB950"));
                ConnectionStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3FB950"));
            }
            else if (status.HasDpiBlock)
            {
                // DPI engeli - Turuncu/Sarı
                ConnectionStatusIcon.Text = "⚠️";
                ConnectionStatusText.Text = "DPI Block Detected";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F85149"));
                ConnectionStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F85149"));
            }
            else
            {
                // Bağlantı başarısız - Kırmızı
                ConnectionStatusIcon.Text = "❌";
                ConnectionStatusText.Text = "Connection Failed";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F85149"));
                ConnectionStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F85149"));
            }
        }
        catch (System.Exception ex)
        {
            ConnectionStatusIcon.Text = "❌";
            ConnectionStatusText.Text = "Error";
            System.Windows.MessageBox.Show($"Connection check failed: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchTextBox.Text;
        
        _showingFavorites = false;
        FavoritesButton.Content = "⭐ FAVORITES";
        
        LoadingBar.Visibility = Visibility.Visible;
        StatusText.Text = "Loading creators... Please wait, motherfucker!";
        SearchButton.IsEnabled = false;
        
        try
        {
            var creators = await _apiService.SearchCreators(query);
            CreatorsListView.ItemsSource = creators;
            StatusText.Text = creators != null ? $"Found {creators.Count} creators" : "No creators found";
        }
        catch (System.Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to search creators: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
            SearchButton.IsEnabled = true;
        }
    }

    private void CreatorsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CreatorsListView.SelectedItem is CoomerDownloader.Services.Creator selectedCreator)
        {
            var postDownloaderWindow = new PostDownloaderWindow(selectedCreator);
            postDownloaderWindow.Show();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            SearchButton_Click(sender, e);
        }
    }
    
    private void FavoritesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_showingFavorites)
        {
            // Favorilerden çık, normal moda dön
            _showingFavorites = false;
            FavoritesButton.Content = "⭐ FAVORITES";
            CreatorsListView.ItemsSource = null;
            StatusText.Text = "Search for creators using the search box above";
        }
        else
        {
            // Favorileri göster
            var favorites = _favoritesManager.GetFavorites();
            CreatorsListView.ItemsSource = favorites;
            
            if (favorites.Count > 0)
            {
                StatusText.Text = $"Showing {favorites.Count} favorite creators";
                _showingFavorites = true;
                FavoritesButton.Content = "❌ CLOSE FAVORITES";
            }
            else
            {
                StatusText.Text = "No favorites yet. Right-click a creator to add to favorites!";
            }
        }
    }
    
    private void CreatorsListView_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        if (CreatorsListView.SelectedItem is CoomerDownloader.Services.Creator selectedCreator)
        {
            bool isFavorite = _favoritesManager.IsFavorite(selectedCreator);
            AddToFavoritesMenuItem.IsEnabled = !isFavorite;
            RemoveFromFavoritesMenuItem.IsEnabled = isFavorite;
        }
        else
        {
            e.Handled = true;
        }
    }
    
    private void AddToFavorites_Click(object sender, RoutedEventArgs e)
    {
        if (CreatorsListView.SelectedItem is CoomerDownloader.Services.Creator selectedCreator)
        {
            _favoritesManager.AddFavorite(selectedCreator);
            System.Windows.MessageBox.Show($"⭐ {selectedCreator.name} added to favorites!", "Success", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
    
    private void RemoveFromFavorites_Click(object sender, RoutedEventArgs e)
    {
        if (CreatorsListView.SelectedItem is CoomerDownloader.Services.Creator selectedCreator)
        {
            _favoritesManager.RemoveFavorite(selectedCreator);
            
            // Eğer favoriler görünümündeyse listeyi güncelle
            if (_showingFavorites)
            {
                FavoritesButton_Click(sender, e);
            }
            
            System.Windows.MessageBox.Show($"❌ {selectedCreator.name} removed from favorites!", "Success", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
