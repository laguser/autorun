using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace autorun
{
    public class StartupItem : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private bool _isEnabled;
        private ImageSource _iconSource;

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }
        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged("Path"); }
        }
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; OnPropertyChanged("IsEnabled"); }
        }
        public ImageSource IconSource
        {
            get { return _iconSource; }
            set { _iconSource = value; OnPropertyChanged("IconSource"); }
        }
        public string Source { get; set; }
        public string OriginalKeyName { get; set; }
        public string TaskName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<StartupItem> RegistryItems { get; set; }
        public ObservableCollection<StartupItem> FolderItems { get; set; }
        public ObservableCollection<StartupItem> SchedulerItems { get; set; }

        private const string RegistryRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupApprovedPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string StartupApprovedFolderPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
        private string StartupFolderPath;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        public MainWindow()
        {
            InitializeComponent();
            StartupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            RegistryItems = new ObservableCollection<StartupItem>();
            FolderItems = new ObservableCollection<StartupItem>();
            SchedulerItems = new ObservableCollection<StartupItem>();

            RegistryList.ItemsSource = RegistryItems;
            StartupFolderList.ItemsSource = FolderItems;
            SchedulerList.ItemsSource = SchedulerItems;

            LoadData();
        }


        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AppIcon_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.Show(this);
            e.Handled = true;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Spin the refresh icon
            System.Windows.Media.Animation.DoubleAnimation spin = new System.Windows.Media.Animation.DoubleAnimation();
            spin.From = 0;
            spin.To = 360;
            spin.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            spin.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut };
            RotateTransform rt = RefreshIcon.RenderTransform as RotateTransform;
            if (rt != null)
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);

            LoadData();
        }




        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool addedAny = false;

                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            string name = System.IO.Path.GetFileNameWithoutExtension(file);
                            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true))
                            {
                                if (key != null)
                                {
                                    // Add quotes around the path to handle spaces
                                    key.SetValue(name, string.Format("\"{0}\"", file));
                                    addedAny = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowStyledInfo("Ошибка", "Не удалось добавить " + System.IO.Path.GetFileName(file) + ": " + ex.Message);
                        }
                    }
                }

                if (addedAny)
                {
                    LoadData();
                }
            }
        }

        private void RegistryHeader_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleAllInCategory(RegistryItems);
        }

        private void FolderHeader_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleAllInCategory(FolderItems);
        }

        private void SchedulerHeader_Click(object sender, MouseButtonEventArgs e)
        {
            ToggleAllInCategory(SchedulerItems);
        }

        private void ToggleAllInCategory(ObservableCollection<StartupItem> items)
        {
            if (items.Count == 0) return;

            // If any are enabled, disable all. Otherwise enable all.
            bool anyEnabled = false;
            foreach (StartupItem item in items)
            {
                if (item.IsEnabled) { anyEnabled = true; break; }
            }

            bool targetState = !anyEnabled; // If any enabled -> disable all, else enable all

            string action = targetState ? "включить" : "выключить";
            string msg = string.Format("{0} все элементы в этой категории ({1} шт.)?",
                targetState ? "Включить" : "Выключить", items.Count);

            if (!ConfirmDialog.Show("Подтверждение", msg, targetState ? "Включить" : "Выключить", this))
                return;

            foreach (StartupItem item in items)
            {
                if (item.IsEnabled != targetState)
                {
                    item.IsEnabled = targetState;
                    if (item.Source == "Registry")
                        ToggleRegistryItem(item);
                    else if (item.Source == "Folder")
                        ToggleFolderItem(item);
                    else if (item.Source == "Scheduler")
                        ToggleSchedulerItem(item);
                }
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "JSON files (*.json)|*.json";
            dlg.Title = "Импорт настроек автозагрузки";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    ShowStyledInfo("Импорт", "Настройки успешно импортированы из:\n" + dlg.FileName);
                    LoadData();
                }
                catch (Exception ex)
                {
                    ShowStyledInfo("Ошибка", "Ошибка импорта: " + ex.Message);
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JSON files (*.json)|*.json";
            dlg.Title = "Экспорт настроек автозагрузки";
            dlg.FileName = "autorun_backup.json";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("{");
                    sb.AppendLine("  \"registry\": [");
                    for (int i = 0; i < RegistryItems.Count; i++)
                    {
                        StartupItem item = RegistryItems[i];
                        string comma = (i < RegistryItems.Count - 1) ? "," : "";
                        sb.AppendLine(string.Format("    {{\"name\": \"{0}\", \"path\": \"{1}\", \"enabled\": {2}}}{3}",
                            EscapeJson(item.Name), EscapeJson(item.Path), item.IsEnabled ? "true" : "false", comma));
                    }
                    sb.AppendLine("  ],");
                    sb.AppendLine("  \"folder\": [");
                    for (int i = 0; i < FolderItems.Count; i++)
                    {
                        StartupItem item = FolderItems[i];
                        string comma = (i < FolderItems.Count - 1) ? "," : "";
                        sb.AppendLine(string.Format("    {{\"name\": \"{0}\", \"path\": \"{1}\", \"enabled\": {2}}}{3}",
                            EscapeJson(item.Name), EscapeJson(item.Path), item.IsEnabled ? "true" : "false", comma));
                    }
                    sb.AppendLine("  ],");
                    sb.AppendLine("  \"scheduler\": [");
                    for (int i = 0; i < SchedulerItems.Count; i++)
                    {
                        StartupItem item = SchedulerItems[i];
                        string comma = (i < SchedulerItems.Count - 1) ? "," : "";
                        sb.AppendLine(string.Format("    {{\"name\": \"{0}\", \"path\": \"{1}\", \"enabled\": {2}}}{3}",
                            EscapeJson(item.Name), EscapeJson(item.Path), item.IsEnabled ? "true" : "false", comma));
                    }
                    sb.AppendLine("  ]");
                    sb.AppendLine("}");
                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    ShowStyledInfo("Экспорт", "Настройки экспортированы в:\n" + dlg.FileName);
                }
                catch (Exception ex)
                {
                    ShowStyledInfo("Ошибка", "Ошибка экспорта: " + ex.Message);
                }
            }
        }

        private string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }


        private void ShowStyledInfo(string title, string message)
        {
            ConfirmDialog.Show(title, message, "OK", this);
        }


        private void LoadData()
        {
            RegistryItems.Clear();
            FolderItems.Clear();
            SchedulerItems.Clear();

            LoadRegistryItems();
            LoadFolderItems();
            LoadSchedulerItems();
        }


        private ImageSource ExtractIconFromPath(string commandLine)
        {
            try
            {
                string exePath = ParseExePath(commandLine);
                if (string.IsNullOrEmpty(exePath)) return null;

                exePath = Environment.ExpandEnvironmentVariables(exePath);
                if (!File.Exists(exePath)) return null;

                System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (icon == null) return null;

                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                icon.Dispose();
                return bitmapSource;
            }
            catch
            {
                return null;
            }
        }

        private string ParseExePath(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return null;
            commandLine = commandLine.Trim();

            // Handle quoted paths: "C:\path\to\app.exe" --args
            if (commandLine.StartsWith("\""))
            {
                int endQuote = commandLine.IndexOf('"', 1);
                if (endQuote > 0)
                    return commandLine.Substring(1, endQuote - 1);
            }

            // Try to find .exe in the string
            int exeIdx = commandLine.ToLower().IndexOf(".exe");
            if (exeIdx > 0)
                return commandLine.Substring(0, exeIdx + 4);

            // Try environment variable paths
            string expanded = Environment.ExpandEnvironmentVariables(commandLine);
            exeIdx = expanded.ToLower().IndexOf(".exe");
            if (exeIdx > 0)
            {
                string path = expanded.Substring(0, exeIdx + 4);
                int driveIdx = path.IndexOf(":\\");
                if (driveIdx > 0)
                    path = path.Substring(driveIdx - 1);
                return path;
            }

            return commandLine.Split(' ')[0];
        }

        private ImageSource ExtractIconFromLnk(string lnkPath)
        {
            try
            {
                if (!File.Exists(lnkPath)) return null;

                // Use WScript.Shell COM to resolve shortcut target
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                string targetPath = (string)shortcut.GetType().InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.GetProperty, null, shortcut, null);

                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);

                if (!string.IsNullOrEmpty(targetPath))
                {
                    targetPath = Environment.ExpandEnvironmentVariables(targetPath);
                    if (File.Exists(targetPath))
                    {
                        System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(targetPath);
                        if (icon != null)
                        {
                            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                                icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            bitmapSource.Freeze();
                            icon.Dispose();
                            return bitmapSource;
                        }
                    }
                }

                // Fallback: extract icon from the .lnk itself
                System.Drawing.Icon lnkIcon = System.Drawing.Icon.ExtractAssociatedIcon(lnkPath);
                if (lnkIcon != null)
                {
                    BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                        lnkIcon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    lnkIcon.Dispose();
                    return bs;
                }
            }
            catch { }
            return null;
        }


        private void LoadRegistryItems()
        {
            try
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RegistryRunPath))
                {
                    if (runKey == null) return;

                    RegistryKey approvedKey = null;
                    try
                    {
                        approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedPath);
                    }
                    catch { }

                    foreach (string valueName in runKey.GetValueNames())
                    {
                        object objVal = runKey.GetValue(valueName);
                        string val = objVal != null ? objVal.ToString() : "";

                        bool enabled = true;
                        if (approvedKey != null)
                        {
                            object approvedVal = approvedKey.GetValue(valueName);
                            if (approvedVal != null && approvedVal is byte[])
                            {
                                byte[] data = (byte[])approvedVal;
                                // byte[0]: 02/06 = enabled, 03/07 = disabled (bit 0 set = disabled)
                                if (data.Length > 0 && (data[0] & 1) != 0)
                                    enabled = false;
                            }
                        }

                        RegistryItems.Add(new StartupItem
                        {
                            Name = valueName,
                            Path = val,
                            IsEnabled = enabled,
                            Source = "Registry",
                            OriginalKeyName = valueName,
                            IconSource = ExtractIconFromPath(val)
                        });
                    }
                    if (approvedKey != null) approvedKey.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading registry: " + ex.Message);
            }
        }


        private void LoadFolderItems()
        {
            try
            {
                if (!Directory.Exists(StartupFolderPath)) return;

                // Check StartupApproved for folder items too
                RegistryKey approvedFolderKey = null;
                try
                {
                    approvedFolderKey = Registry.CurrentUser.OpenSubKey(StartupApprovedFolderPath);
                }
                catch { }

                foreach (string file in Directory.GetFiles(StartupFolderPath))
                {
                    string ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".ini") continue; // Skip desktop.ini

                    string fileName = System.IO.Path.GetFileName(file);
                    string displayName = System.IO.Path.GetFileNameWithoutExtension(file);

                    // Check if disabled via StartupApproved\StartupFolder
                    bool enabled = true;
                    if (approvedFolderKey != null)
                    {
                        object approvedVal = approvedFolderKey.GetValue(fileName);
                        if (approvedVal != null && approvedVal is byte[])
                        {
                            byte[] data = (byte[])approvedVal;
                            if (data.Length > 0 && (data[0] & 1) != 0)
                                enabled = false;
                        }
                    }

                    ImageSource icon = null;
                    if (ext == ".lnk")
                        icon = ExtractIconFromLnk(file);
                    else if (ext == ".exe")
                        icon = ExtractIconFromPath(file);

                    FolderItems.Add(new StartupItem
                    {
                        Name = displayName,
                        Path = file,
                        IsEnabled = enabled,
                        Source = "Folder",
                        OriginalKeyName = fileName,  // Store just the filename for StartupApproved lookup
                        IconSource = icon
                    });
                }
                if (approvedFolderKey != null) approvedFolderKey.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading folder: " + ex.Message);
            }
        }


        private void LoadSchedulerItems()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "schtasks.exe";
                psi.Arguments = "/query /fo csv /v";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.StandardOutputEncoding = Encoding.GetEncoding(866);

                Process proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                string[] lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                int colTaskName = -1, colTaskToRun = -1, colState = -1, colScheduleType = -1;
                bool headerFound = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("\"HostName\"") || lines[i].StartsWith("\"TaskName\""))
                    {
                        string[] headers = ParseCsvLine(lines[i]);
                        for (int h = 0; h < headers.Length; h++)
                        {
                            string hdr = headers[h].Trim('"');
                            if (hdr == "TaskName") colTaskName = h;
                            else if (hdr == "Task To Run") colTaskToRun = h;
                            else if (hdr == "Scheduled Task State") colState = h;
                            else if (hdr == "Schedule Type") colScheduleType = h;
                        }
                        headerFound = true;
                        continue;
                    }

                    if (!headerFound || colTaskName < 0) continue;

                    string[] cols = ParseCsvLine(lines[i]);
                    if (cols.Length <= colTaskName) continue;

                    string taskName = cols[colTaskName].Trim('"');
                    string taskToRun = (colTaskToRun >= 0 && colTaskToRun < cols.Length) ? cols[colTaskToRun].Trim('"') : "";
                    string state = (colState >= 0 && colState < cols.Length) ? cols[colState].Trim('"') : "";
                    string schedType = (colScheduleType >= 0 && colScheduleType < cols.Length) ? cols[colScheduleType].Trim('"') : "";

                    bool isStartup = schedType.IndexOf("logon", StringComparison.OrdinalIgnoreCase) >= 0
                        || schedType.IndexOf("startup", StringComparison.OrdinalIgnoreCase) >= 0
                        || schedType.IndexOf("boot", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!isStartup) continue;
                    if (taskName.StartsWith("\\Microsoft\\")) continue;

                    bool enabled = state.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0;

                    string displayName = taskName;
                    int lastSlash = taskName.LastIndexOf('\\');
                    if (lastSlash >= 0 && lastSlash < taskName.Length - 1)
                        displayName = taskName.Substring(lastSlash + 1);

                    SchedulerItems.Add(new StartupItem
                    {
                        Name = displayName,
                        Path = taskToRun,
                        IsEnabled = enabled,
                        Source = "Scheduler",
                        OriginalKeyName = taskName,
                        TaskName = taskName,
                        IconSource = ExtractIconFromPath(taskToRun)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading scheduler: " + ex.Message);
            }
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuote = false;
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuote = !inQuote;
                    current.Append(c);
                }
                else if (c == ',' && !inQuote)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }


        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            if (chk == null) return;

            StartupItem item = chk.Tag as StartupItem;
            if (item == null) return;

            bool newState = chk.IsChecked.HasValue ? chk.IsChecked.Value : false;
            item.IsEnabled = newState;

            if (item.Source == "Registry")
                ToggleRegistryItem(item);
            else if (item.Source == "Folder")
                ToggleFolderItem(item);
            else if (item.Source == "Scheduler")
                ToggleSchedulerItem(item);
        }

        private void ToggleRegistryItem(StartupItem item)
        {
            try
            {
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(StartupApprovedPath))
                {
                    if (approvedKey == null) return;

                    // Read existing data or create new
                    byte[] data = new byte[12];
                    object existing = approvedKey.GetValue(item.OriginalKeyName);
                    if (existing != null && existing is byte[] && ((byte[])existing).Length >= 12)
                    {
                        data = (byte[])existing;
                    }

                    if (item.IsEnabled)
                    {
                        // Enable: set byte 0 to 02 (clear bit 0)
                        data[0] = (byte)(data[0] & ~1);
                        if (data[0] == 0) data[0] = 0x02;
                    }
                    else
                    {
                        // Disable: set byte 0 to 03 (set bit 0)
                        data[0] = (byte)(data[0] | 1);
                        if (data[0] == 1) data[0] = 0x03;
                    }
                    approvedKey.SetValue(item.OriginalKeyName, data, RegistryValueKind.Binary);
                }
            }
            catch (Exception ex)
            {
                ShowStyledInfo("Ошибка", "Ошибка изменения реестра: " + ex.Message);
                item.IsEnabled = !item.IsEnabled;
            }
        }

        private void ToggleFolderItem(StartupItem item)
        {
            try
            {
                // Use StartupApproved\StartupFolder (same mechanism as Task Manager)
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(StartupApprovedFolderPath))
                {
                    if (approvedKey == null) return;

                    byte[] data = new byte[12];
                    object existing = approvedKey.GetValue(item.OriginalKeyName);
                    if (existing != null && existing is byte[] && ((byte[])existing).Length >= 12)
                    {
                        data = (byte[])existing;
                    }

                    if (item.IsEnabled)
                    {
                        data[0] = (byte)(data[0] & ~1);
                        if (data[0] == 0) data[0] = 0x02;
                    }
                    else
                    {
                        data[0] = (byte)(data[0] | 1);
                        if (data[0] == 1) data[0] = 0x03;
                    }
                    approvedKey.SetValue(item.OriginalKeyName, data, RegistryValueKind.Binary);
                }
            }
            catch (Exception ex)
            {
                ShowStyledInfo("Ошибка", "Ошибка: " + ex.Message);
                item.IsEnabled = !item.IsEnabled;
            }
        }

        private void ToggleSchedulerItem(StartupItem item)
        {
            try
            {
                string action = item.IsEnabled ? "/Enable" : "/Disable";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "schtasks.exe";
                psi.Arguments = string.Format("/Change /TN \"{0}\" {1}", item.TaskName, action);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                Process proc = Process.Start(psi);
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    ShowStyledInfo("Ошибка", "Ошибка планировщика: " + stderr + "\nВозможно, требуются права администратора.");
                    item.IsEnabled = !item.IsEnabled;
                }
            }
            catch (Exception ex)
            {
                ShowStyledInfo("Ошибка", "Ошибка планировщика: " + ex.Message);
                item.IsEnabled = !item.IsEnabled;
            }
        }


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            StartupItem item = btn.Tag as StartupItem;
            if (item == null) return;

            // Use custom styled confirm dialog
            if (!ConfirmDialog.Show("Удаление", string.Format("Удалить \"{0}\" из автозагрузки?", item.Name), "Удалить", this))
                return;

            try
            {
                if (item.Source == "Registry")
                {
                    // Delete from Run key
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true))
                    {
                        if (key != null) key.DeleteValue(item.OriginalKeyName, false);
                    }
                    // Also clean from StartupApproved
                    using (RegistryKey approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedPath, true))
                    {
                        if (approvedKey != null) approvedKey.DeleteValue(item.OriginalKeyName, false);
                    }
                    RegistryItems.Remove(item);
                }
                else if (item.Source == "Folder")
                {
                    // Delete the shortcut file
                    string fullPath = System.IO.Path.Combine(StartupFolderPath, item.OriginalKeyName);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                    // Also clean from StartupApproved
                    using (RegistryKey approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedFolderPath, true))
                    {
                        if (approvedKey != null) approvedKey.DeleteValue(item.OriginalKeyName, false);
                    }
                    FolderItems.Remove(item);
                }
                else if (item.Source == "Scheduler")
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "schtasks.exe";
                    psi.Arguments = string.Format("/Delete /TN \"{0}\" /F", item.TaskName);
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.CreateNoWindow = true;

                    Process proc = Process.Start(psi);
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (proc.ExitCode == 0)
                    {
                        SchedulerItems.Remove(item);
                    }
                    else
                    {
                        ShowStyledInfo("Ошибка", "Не удалось удалить задачу: " + stderr + "\nВозможно, требуются права администратора.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStyledInfo("Ошибка", "Ошибка: " + ex.Message);
            }
        }
    }
}
