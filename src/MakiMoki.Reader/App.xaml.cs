using AngleSharp.Dom;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Yarukizero.Net.MakiMoki.Reader {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : PrismApplication {
		protected override void OnStartup(StartupEventArgs e) {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Reactive.Bindings.UIDispatcherScheduler.Initialize();

			var userRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FutaMaki");
			if(!Directory.Exists(userRoot)) {
				Directory.CreateDirectory(userRoot);
			}
			var readerDirectory = Path.Combine(userRoot, "Reader");
			if(!Directory.Exists(readerDirectory)) {
				Directory.CreateDirectory(readerDirectory);
			}
			var saveDirectory = Path.Combine(readerDirectory, "Save");
			if(!Directory.Exists(saveDirectory)) {
				Directory.CreateDirectory(saveDirectory);
			}
			var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FutaMaki");
			if(!Directory.Exists(appData)) {
				Directory.CreateDirectory(appData);
			}
			appData = Path.Combine(appData, "FutaMaki.Reader");
			if(!Directory.Exists(appData)) {
				Directory.CreateDirectory(appData);
			}
			ReaderConfigs.ConfigLoader.Initialize(new ReaderConfigs.ConfigLoader.Setting(
				readerDirectory: readerDirectory,
				saveDirectory: saveDirectory,
				appDataDirectory: appData
				));

			base.OnStartup(e);
		}

		protected override Window CreateShell() {
			return Container.Resolve<Windows.MainWindow>();
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry) {
			base.ConfigureViewModelLocator();
			ViewModelLocationProvider.Register<Windows.MainWindow, ViewModels.MainViewModel>();
			ViewModelLocationProvider.Register<Windows.ConfigDialog, ViewModels.ConfigDialogViewModel>();
			containerRegistry.RegisterDialog<Windows.ConfigDialog, ViewModels.ConfigDialogViewModel>();
		}
	}
}