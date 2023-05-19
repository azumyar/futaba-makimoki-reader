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
		[Obsolete]
		public Data.Cookie2[] Cookie { get; set; } = Array.Empty<Data.Cookie2>();

		public new static App? Current { get; private set; }

		protected override void OnStartup(StartupEventArgs e) {
			Current = this;
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
			ReaderConfigs.ConfigLoader.Initialize(new ReaderConfigs.ConfigLoader.Setting(
				readerDirectory: readerDirectory,
				saveDirectory: saveDirectory
				));

			base.OnStartup(e);
		}

		protected override Window CreateShell() {
			return Container.Resolve<Windows.MainWindow>();
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry) {
			base.ConfigureViewModelLocator();
			ViewModelLocationProvider.Register<Windows.MainWindow, ViewModels.MainViewModel>();
		}
	}
}