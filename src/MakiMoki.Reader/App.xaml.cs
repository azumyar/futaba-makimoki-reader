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
		public string? UserRootDirectory { get; private set; }
		public string? RederDirectory { get; private set; }

		public Data.Cookie2[] Cookie { get; set; } = Array.Empty<Data.Cookie2>();

		public static App? Current { get; private set; }

		protected override void OnStartup(StartupEventArgs e) {
			Current = this;
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Reactive.Bindings.UIDispatcherScheduler.Initialize();

			this.UserRootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FutaMaki");
			if(!Directory.Exists(this.UserRootDirectory)) {
				Directory.CreateDirectory(this.UserRootDirectory);
			}
			this.RederDirectory = Path.Combine(this.UserRootDirectory, "Reader");
			if(!Directory.Exists(this.RederDirectory)) {
				Directory.CreateDirectory(this.RederDirectory);
			}
			ReaderConfigs.ConfigLoader.Initialize();

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