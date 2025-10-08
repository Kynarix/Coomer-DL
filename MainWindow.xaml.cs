using System.Text;
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

    public MainWindow()
    {
        InitializeComponent();
        _apiService = new CoomerDownloader.Services.CoomerApiService();
        CreatorsListView.MouseDoubleClick += CreatorsListView_MouseDoubleClick;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchTextBox.Text;
        
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
}
