using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VELAppsHub
{
	public partial class MainWindow : Window
	{

		public const string updateURL = "https://vel.engr.uga.edu/apps/VELAppsHub/get_apps.php";
		private const string versionFileName = "velappshub_appversion.txt";
		public AppsResponse appsData;
		private List<UIElement> appsUI = new List<UIElement>();
		private Dictionary<AppsResponse.App, ProgressBar> progressBars = new Dictionary<AppsResponse.App, ProgressBar>();

		public string appsInstallPath;

		public class AppsResponse
		{
			public App[] apps { get; set; }
			public class App
			{
				public string name { get; set; }
				public string description { get; set; }
				public string download { get; set; }
				public string thumbnail { get; set; }
				public string version { get; set; }
				public string[] accessible_by { get; set; }
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			RegisterUriScheme("velappshub", "VEL Apps Hub Protocol");
			appsInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "VELApps");

			GetApps();
		}

		public void GetApps()
		{
			WebResponse response;
			StreamReader sReader;
			string rawJSON = string.Empty;

			// Do we get a response?
			try
			{
				// Create Session.
				WebRequest request = WebRequest.Create(updateURL);
				response = request.GetResponse();

				Stream dataStream = response.GetResponseStream();
				sReader = new StreamReader(dataStream);

				// Session Contents
				rawJSON = sReader.ReadToEnd();

				// pls close (;-;)
				if (sReader != null)
					sReader.Close();
				if (response != null)
					response.Close();
			}
			catch (Exception)
			{
				Console.WriteLine("Can't get apps/updates");
			}


			try
			{
				appsData = JsonSerializer.Deserialize<AppsResponse>(rawJSON);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Can't parse apps/updates response\n{e}");
			}



			RefreshUI();

		}

		private void RefreshUI()
		{
			if (appsData != null && appsData.apps != null)
			{
				foreach (var app in appsUI)
				{
					appsContainer.Children.Remove(app);
				}
				appsUI.Clear();

				foreach (var app in appsData.apps)
				{
					AddAppUI(app);
				}
			}
		}

		private void AddAppUI(AppsResponse.App app)
		{
			GroupBox groupBox = new GroupBox
			{
				Margin = new Thickness(12),
			};
			appsContainer.Children.Add(groupBox);
			appsUI.Add(groupBox);
			groupBox.Header = app.name;
			StackPanel panel = new StackPanel
			{
				Width = 250,
				Height = 250,
			};
			groupBox.Content = panel;
			Label title = new Label
			{
				Content = app.name,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontWeight = FontWeights.Bold
			};
			//panel.Children.Add(title);
			Image thumbnail = new Image
			{
				Source = new BitmapImage(new Uri(app.thumbnail)),
				MaxHeight = 150,
				Margin = new Thickness(0, 0, 0, 4),
			};
			panel.Children.Add(thumbnail);

			var buttonPadding = new Thickness(6, 2, 6, 2);
			if (IsAppInstalled(app))
			{
				if (GetInstalledAppVersion(app) != app.version)
				{
					Button updateButton = new Button
					{
						Content = "Update",
						Margin = buttonPadding,
					};
					updateButton.Click += new RoutedEventHandler((s, e) => { InstallApp(app); });
					panel.Children.Add(updateButton);
				}

				Button openButton = new Button
				{
					Content = "Open",
					Margin = buttonPadding,
				};
				openButton.Click += new RoutedEventHandler((s, e) => { OpenApp(app); });
				panel.Children.Add(openButton);

				Button uninstallButton = new Button
				{
					Content = "Uninstall",
					Margin = buttonPadding,
				};
				uninstallButton.Click += new RoutedEventHandler((s, e) => { UninstallApp(app); });
				panel.Children.Add(uninstallButton);
			}
			else
			{
				Button installButton = new Button
				{
					Content = "Install",
					Margin = buttonPadding,
				};
				installButton.Click += new RoutedEventHandler((s, e) => { InstallApp(app); });
				panel.Children.Add(installButton);
			}

			ProgressBar progressBar = new ProgressBar
			{
				Visibility = Visibility.Collapsed,
				Height = 14,
				Margin = buttonPadding,
			};
			panel.Children.Add(progressBar);
			progressBars[app] = progressBar;

		}

		private string GetInstalledAppVersion(AppsResponse.App app)
		{
			string targetFolderName = Path.Combine(appsInstallPath, app.name);
			if (!Directory.Exists(targetFolderName)) return null;

			string versionFile = Path.Combine(targetFolderName, versionFileName);
			if (!File.Exists(versionFile)) return null;
			return File.ReadAllText(versionFile);
		}

		public bool IsAppInstalled(AppsResponse.App app)
		{
			string targetFolderName = Path.Combine(appsInstallPath, app.name);
			return Directory.Exists(targetFolderName);
		}

		public void InstallApp(AppsResponse.App app)
		{
			string downloadsFolderName = Path.Combine(appsInstallPath, "Downloads");
			string filename = Path.Combine(downloadsFolderName, Path.GetFileName(app.download));

			WebClient webClient = new WebClient();
			webClient.DownloadProgressChanged += (s, e) => { ProgressChanged(app, e.ProgressPercentage); };
			webClient.DownloadFileCompleted += (s, e) => { ActuallyInstallApp(filename, app); };
			if (!Directory.Exists(downloadsFolderName)) Directory.CreateDirectory(downloadsFolderName);
			webClient.DownloadFileAsync(new Uri(app.download), filename);
		}

		public void UninstallApp(AppsResponse.App app)
		{
			string targetFolderName = Path.Combine(appsInstallPath, app.name);
			Directory.Delete(targetFolderName, true);

			RefreshUI();
		}

		/// <summary>
		/// Finds the relevant exe and runs it
		/// </summary>
		/// <param name="app"></param>
		public void OpenApp(AppsResponse.App app)
		{
			string targetFolderName = Path.Combine(appsInstallPath, app.name);
			//// go up to 3 levels deep
			//string folder = null;
			//for (int i = 0; i < 3; i++)
			//{
			//	if (Directory.EnumerateFiles(targetFolderName).Contains("UnityCrashHandler64.exe"))
			//	{
			//		folder = true;
			//		break;
			//	}
			//}
			//if (folder == null) return;

			List<string> filteredFiles = Directory.EnumerateFiles(targetFolderName).Where(f => f.EndsWith(".exe") && !f.EndsWith("UnityCrashHandler64.exe")).ToList();
			if (filteredFiles.Count != 1) return;
			Process.Start(new ProcessStartInfo
			{
				FileName = filteredFiles[0],
				UseShellExecute = true
			});
		}

		/// <summary>
		/// Progress bar is hidden when progress is at 0 or 1
		/// </summary>
		/// <param name="app"></param>
		/// <param name="progressPercentage">int 0-100</param>
		private void ProgressChanged(AppsResponse.App app, int progressPercentage)
		{
			progressBars[app].Visibility = progressPercentage == 0 || progressPercentage == 100 ? Visibility.Collapsed : Visibility.Visible;
			progressBars[app].Value = progressPercentage;
		}

		private void ActuallyInstallApp(string zipName, AppsResponse.App app)
		{
			try
			{
				string targetFolderName = Path.Combine(appsInstallPath, app.name);
				// unzip the zip 🤐
				ZipFile.ExtractToDirectory(zipName, targetFolderName, true);

				// add the version file
				string versionFile = Path.Combine(targetFolderName, versionFileName);
				File.WriteAllText(versionFile, app.version);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Can't read downloaded zip.\n{e}");
			}

			RefreshUI();
		}

		public static void RegisterUriScheme(string UriScheme, string FriendlyName)
		{
			try
			{
				using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme);
				string applicationLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VELAppsHub.exe");

				key.SetValue("", "URL:" + FriendlyName);
				key.SetValue("URL Protocol", "");

				using RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon");
				defaultIcon.SetValue("", applicationLocation + ",1");

				using RegistryKey commandKey = key.CreateSubKey(@"shell\open\command");
				commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to set URI scheme\n{e}");
			}
		}
	}
}
