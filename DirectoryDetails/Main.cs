using MediaInfoLib;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryDetails
{
    public partial class Main : Form
    {
        //private string videoDirectory = @"C:\Hassan\Courses\Python\Complete Python Bootcamp HD";
        private MediaInfo mediaInfo;
        private Dictionary<string, long> directorySizesCache = new Dictionary<string, long>();
        private Dictionary<string, double> directoryDurationCache = new Dictionary<string, double>();

        public Main()
        {
            InitializeComponent();
        }
        private async Task LoadVideoFilesAsync(string directoryPath)
        {
            tvControl.Nodes.Clear();
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

            await TraverseDirectoriesAsync(directoryInfo, tvControl.Nodes);
        }
        private async Task TraverseDirectoriesAsync(DirectoryInfo directory, TreeNodeCollection parentNodes)
        {
            double fileSize = 0;
            double duration = 0;

            var directories = directory.GetDirectories();

            foreach (var subDirectory in directories)
            {
                //fileSize = await Task.Run(() => subDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length) / (1024 * 1024));
                fileSize = await Task.Run(() => CalculateDirectorySize(subDirectory.FullName));
                fileSize = fileSize / (1024 * 1024);

                //duration = await Task.Run(() => subDirectory.GetFiles("*.*").Where(file => file.Extension.ToLower() == ".mp4" || file.Extension.ToLower() == ".avi").Sum(f => GetVideoLengthAsync(f.FullName).Result.Seconds));
                duration = await Task.Run(() => CalculateDirectoryDuration(subDirectory.FullName));

                TreeNode directoryNode = new TreeNode(subDirectory.Name + " | " + TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss") + " | " + (fileSize > 1024 ? Math.Round((fileSize / 1024), 2) + " GB" : Math.Round(fileSize,2) + " MB"));

                directoryNode.Tag = subDirectory.FullName;
                parentNodes.Add(directoryNode);
                await TraverseDirectoriesAsync(subDirectory, directoryNode.Nodes);
            }

            var files = directory.GetFiles("*.*").Where(file => file.Extension.ToLower() == ".mp4" || file.Extension.ToLower() == ".avi" || file.Extension.ToLower() == ".m4v" || file.Extension.ToLower() == ".mkv");

            foreach (var fileInfo in files)
            {
                TimeSpan videoLength = await GetVideoLengthAsync(fileInfo.FullName);
                double size = fileInfo.FullName.Length / (1024 * 1024);

                TreeNode fileNode = new TreeNode(fileInfo.Name + " | " + videoLength.ToString(@"hh\:mm\:ss") + " | " + (size > 1024 ? Math.Round((size / 1024), 2) + " GB" : Math.Round(size,2) + " MB"));

                //fileSize += fileInfo.FullName.Length;
                //duration += videoLength.TotalSeconds;

                fileNode.Tag = fileInfo.FullName;
                parentNodes.Add(fileNode);
            }
        }
        public double CalculateDirectoryDuration(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException("Directory not found.");
            }

            if (directoryDurationCache.ContainsKey(directoryPath))
            {
                return directoryDurationCache[directoryPath];
            }

            double totalDuration = 0;

            // Calculate size of files in the current directory
            string[] files = Directory.GetFiles(directoryPath);

            //Parallel.ForEach(files, file =>
            //{
            //    FileInfo fileInfo = new FileInfo(file);
            //    totalDuration += GetVideoLengthAsync(fileInfo.FullName).Result.TotalSeconds;
            //});

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                totalDuration += GetVideoLengthAsync(fileInfo.FullName).Result.TotalSeconds;
            }

            // Calculate size of subdirectories
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            foreach (string subDirectory in subDirectories)
            {
                totalDuration += CalculateDirectoryDuration(subDirectory); // Recursively calculate nested subdirectory sizes
            }

            directoryDurationCache[directoryPath] = totalDuration; // Store calculated duration in cache
            return totalDuration;
        }
        public long CalculateDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException("Directory not found.");
            }

            if (directorySizesCache.ContainsKey(directoryPath))
            {
                return directorySizesCache[directoryPath];
            }

            long totalSize = 0;

            // Calculate size of files in the current directory
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }

            // Calculate size of subdirectories
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            foreach (string subDirectory in subDirectories)
            {
                totalSize += CalculateDirectorySize(subDirectory); // Recursively calculate nested subdirectory sizes
            }

            directorySizesCache[directoryPath] = totalSize; // Store calculated size in cache
            return totalSize;
        }
        private void tvControl_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                string filePath = e.Node.Tag as string;
                if (!string.IsNullOrEmpty(filePath))
                {
                    //TimeSpan videoLength = await GetVideoLengthAsync(filePath);

                    //fileSizeLabel.Text = $"File Size: {new FileInfo(filePath).Length / (1024 * 1024)} MB";
                    //videoLengthLabel.Text = $"Video Length: {videoLength.TotalHours:F2} hours";
                }
                else
                {
                    //fileSizeLabel.Text = "File Size: N/A";
                    //videoLengthLabel.Text = "Video Length: N/A";
                }
            }
        }
        private async Task<TimeSpan> GetVideoLengthAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                mediaInfo = new MediaInfo();
                mediaInfo.Open(filePath);
                string duration = mediaInfo.Get(StreamKind.General, 0, "Duration");
                mediaInfo.Close();

                if (double.TryParse(duration, out double durationMs))
                {
                    TimeSpan videoLength = TimeSpan.FromMilliseconds(durationMs);
                    return videoLength;
                }
                return TimeSpan.Zero;
            });
        }
        private async void btnSelect_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fbd.SelectedPath;
                await LoadVideoFilesAsync(fbd.SelectedPath);

                MessageBox.Show("Completed", "", MessageBoxButtons.OK);
            }
        }
        private void tvControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string filePath = tvControl.SelectedNode.Tag as string;
            Process.Start(filePath);
        }
    }
}
