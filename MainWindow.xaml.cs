using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

		public const string updateURL = "http://127.0.0.1:5000/get_apps";
		public AppsResponse appsData;

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



			if (appsData != null && appsData.apps != null)
			{
				foreach (var app in appsData.apps)
				{
					AddAppUI(app);
				}
			}

		}

		private void AddAppUI(AppsResponse.App app)
		{
			Button button = new Button
			{
				Background = Brushes.LightGray,
				Width = 250,
				Height = 250,
				Margin = new Thickness(16, 16, 16, 16),
			};
			button.Click += new RoutedEventHandler((s, e) => { InstallApp(app); });
			StackPanel panel = new StackPanel
			{
				Width = 250,
				Height = 250,
			};
			button.Content = panel;
			Label title = new Label
			{
				Content = app.name,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontWeight = FontWeights.Bold
			};
			panel.Children.Add(title);
			Image thumbnail = new Image
			{
				Source = new BitmapImage(new Uri(app.thumbnail)),
			};
			panel.Children.Add(thumbnail);

			appsContainer.Children.Add(button);
		}

		public void InstallApp(AppsResponse.App app)
		{
			string targetFolderName = Path.Combine(appsInstallPath, app.name);
			string downloadsFolderName = Path.Combine(appsInstallPath, "Downloads");
			string filename = Path.Combine(downloadsFolderName, Path.GetFileName(app.download));

			WebClient webClient = new WebClient();
			webClient.DownloadFileCompleted += (s, e) => { ActuallyInstallApp(filename, targetFolderName); };
			webClient.DownloadProgressChanged += ProgressChanged;
			//webClient.DownloadFileAsync(new Uri(app.download), Path.GetTempPath() + Path.GetFileName(app.download));
			if (!Directory.Exists(downloadsFolderName)) Directory.CreateDirectory(downloadsFolderName);
			webClient.DownloadFileAsync(new Uri(app.download), filename);
		}

		private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{

		}

		private void ActuallyInstallApp(string zipName, string targetDirectory)
		{
			// unzip the zip 🤐
			ZipFile.ExtractToDirectory(zipName, targetDirectory, true);
		}

		public static void RegisterUriScheme(string UriScheme, string FriendlyName)
		{
			try
			{
				using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme);
				string applicationLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IgniteBot.exe");

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
