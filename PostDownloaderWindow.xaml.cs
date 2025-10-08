using CoomerDownloader.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace CoomerDownloader
{
    public partial class PostDownloaderWindow : Window
    {
        private readonly CoomerApiService _apiService;
        private readonly Creator _creator;
        private string _downloadPath;

        public PostDownloaderWindow(Creator creator)
        {
            InitializeComponent();
            _apiService = new CoomerApiService();
            _creator = creator;
            CreatorNameText.Text = $"• {creator.name} ({creator.service})";
            
            _downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", creator.name ?? "downloads");
            DownloadPathText.Text = _downloadPath;
            
            LoadPosts();
        }

        private async void LoadPosts()
        {
            if (!string.IsNullOrEmpty(_creator.id) && !string.IsNullOrEmpty(_creator.service))
            {
                DownloadStatusText.Text = "Loading posts... This may take a while for creators with many posts.";
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressBar.IsIndeterminate = true;
                DownloadButton.IsEnabled = false;
                SelectAllButton.IsEnabled = false;
                
                var posts = await _apiService.GetCreatorPosts(_creator.service, _creator.id);
                
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                DownloadStatusText.Text = "";
                DownloadButton.IsEnabled = true;
                SelectAllButton.IsEnabled = true;
                
                if (posts != null)
                {
                    PostsListView.ItemsSource = posts;
                    DownloadStatusText.Text = $"Loaded {posts.Count} posts";
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to load posts!");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Creator information is incomplete, motherfucker!");
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            PostsListView.SelectAll();
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select Download Folder",
                FileName = "Select Folder",
                Filter = "Folder|*.folder",
                CheckFileExists = false,
                CheckPathExists = true
            };

            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Select download folder";
                folderDialog.SelectedPath = _downloadPath;
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _downloadPath = folderDialog.SelectedPath;
                    DownloadPathText.Text = _downloadPath;
                }
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

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPosts = PostsListView.SelectedItems;
            if (selectedPosts.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one post to download!", "No Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var downloadFolder = _downloadPath;
            System.IO.Directory.CreateDirectory(downloadFolder);

            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadButton.IsEnabled = false;
            SelectAllButton.IsEnabled = false;

            int totalFiles = 0;
            int downloadedFiles = 0;

            foreach (Post post in selectedPosts)
            {
                totalFiles += post.AllFiles.Count;
                if (post.attachments != null) totalFiles += post.attachments.Count;
            }

            DownloadProgressBar.Maximum = totalFiles;
            DownloadProgressBar.IsIndeterminate = false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");

                    foreach (Post post in selectedPosts)
                    {

                        foreach (var file in post.AllFiles)
                        {
                            if (file != null && !string.IsNullOrEmpty(file.name) && !string.IsNullOrEmpty(file.path))
                            {
                                try
                                {
                                    DownloadStatusText.Text = $"Downloading: {file.name}... ({downloadedFiles + 1}/{totalFiles})";
                                    var filePath = System.IO.Path.Combine(downloadFolder, file.name);
                                    var fileUrl = $"https://coomer.st{file.path}";
                                    var fileBytes = await client.GetByteArrayAsync(fileUrl);
                                    await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
                                    downloadedFiles++;
                                    DownloadProgressBar.Value = downloadedFiles;
                                }
                                catch (System.Exception ex)
                                {
                                    DownloadStatusText.Text = $"Failed to download {file.name}: {ex.Message}";
                                    await System.Threading.Tasks.Task.Delay(1000);
                                }
                            }
                        }

                        if (post.attachments != null)
                        {
                            foreach (var attachment in post.attachments)
                            {
                                if (attachment != null && !string.IsNullOrEmpty(attachment.name) && !string.IsNullOrEmpty(attachment.path))
                                {
                                    try
                                    {
                                        DownloadStatusText.Text = $"Downloading: {attachment.name}... ({downloadedFiles + 1}/{totalFiles})";
                                        var attachmentPath = System.IO.Path.Combine(downloadFolder, attachment.name);
                                        var attachmentUrl = $"https://coomer.st{attachment.path}";
                                        var attachmentBytes = await client.GetByteArrayAsync(attachmentUrl);
                                        await System.IO.File.WriteAllBytesAsync(attachmentPath, attachmentBytes);
                                        downloadedFiles++;
                                        DownloadProgressBar.Value = downloadedFiles;
                                    }
                                    catch (System.Exception ex)
                                    {
                                        DownloadStatusText.Text = $"Failed to download {attachment.name}: {ex.Message}";
                                        await System.Threading.Tasks.Task.Delay(1000);
                                    }
                                }
                            }
                        }
                    }
                }

                DownloadStatusText.Text = $"Download complete! {downloadedFiles} files saved to {downloadFolder}";
                System.Windows.MessageBox.Show($"Download complete!\n\n{downloadedFiles} files downloaded to:\n{downloadFolder}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                DownloadStatusText.Text = $"Error: {ex.Message}";
                System.Windows.MessageBox.Show($"Download failed: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                DownloadButton.IsEnabled = true;
                SelectAllButton.IsEnabled = true;
            }
        }
    }
}
