using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Yarukizero.Net.MakiMoki.Reader.ReaderUtils;

namespace Yarukizero.Net.MakiMoki.Reader.ViewModels {
	internal class MainViewModel {
		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr ShellExecute(
			IntPtr hwnd,
			[MarshalAs(UnmanagedType.LPWStr)] string? lpVerb,
			[MarshalAs(UnmanagedType.LPWStr)] string? lpFile,
			[MarshalAs(UnmanagedType.LPWStr)] string? lpParameters,
			[MarshalAs(UnmanagedType.LPWStr)] string? lpDirectory,
			int nShowCmd);

		private enum Navigation {
			Start,
			PostThread,
			InputUrl,
			Reader
		}
		private ReactivePropertySlim<Navigation> Navigate { get; } = new ReactivePropertySlim<Navigation>(initialValue:Navigation.Start);

		public ReactivePropertySlim<string> BoardUrl { get; } = new ReactivePropertySlim<string>(initialValue:"");
		public ReactivePropertySlim<string> Subject { get; } = new ReactivePropertySlim<string>(initialValue: "");
		public ReactivePropertySlim<string> Name { get; } = new ReactivePropertySlim<string>(initialValue: "");
		public ReactivePropertySlim<string> Mail { get; } = new ReactivePropertySlim<string>(initialValue: "");
		public ReactivePropertySlim<string> Comment { get; } = new ReactivePropertySlim<string>(initialValue: "");
		public ReactivePropertySlim<string> DelKey { get; } = new ReactivePropertySlim<string>(initialValue: "");
		private ReactivePropertySlim<string> ImagePath { get; } = new ReactivePropertySlim<string>(initialValue: "");
		public IReadOnlyReactiveProperty<string> ImageButtonText { get; }
		private string threadNo = "";

		public ReactivePropertySlim<string> InputUrl {get;} = new ReactivePropertySlim<string>(initialValue: "");

		public IReadOnlyReactiveProperty<Visibility> StartViewVisibility { get; } 
		public IReadOnlyReactiveProperty<Visibility> PostThreadViewVisibility { get; }
		public IReadOnlyReactiveProperty<Visibility> PostedThreadViewVisibility { get; }
		public IReadOnlyReactiveProperty<Visibility> ReaderViewVisibility { get; }

		private ReactivePropertySlim<bool> IsReading { get; } = new ReactivePropertySlim<bool>(initialValue:true);

		public IReadOnlyReactiveProperty<string> ReadingMessage { get; }
		public IReadOnlyReactiveProperty<Visibility> CurrentReaderVisibility { get; }
		public IReadOnlyReactiveProperty<Visibility> NextReaderVisibility { get; }


		public ReactiveCommandSlim NavigateBackCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim NavigateToPostThreadCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim NavigateToPostedThreadCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim StartReaderFromInputCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim PostThreadCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim OpenImageCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim OpenBrowserCommand { get; } = new ReactiveCommandSlim();
		
		public CancellationTokenSource? cancellationTokenSource = null;
		public IDisposable? readerTask = null;

		private const string MessageThreadReading = "スレッドを読み上げ中です";
		private const string MessageThreadDie= "スレッドが落ちました";

		public MainViewModel() {
			this.StartViewVisibility = this.Navigate.Select(x => x switch {
				Navigation.Start => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
			this.PostThreadViewVisibility = this.Navigate.Select(x => x switch {
				Navigation.PostThread => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
			this.PostedThreadViewVisibility = this.Navigate.Select(x => x switch {
				Navigation.InputUrl => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
			this.ReaderViewVisibility = this.Navigate.Select(x => x switch {
				Navigation.Reader => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();

			this.ImageButtonText = this.ImagePath.Select(x => File.Exists(x) switch {
				true => "画像選択済",
				false => "画像読み込み",
			}).ToReadOnlyReactivePropertySlim<string>();

			this.ReadingMessage = this.IsReading.Select(x => x switch {
				true => MessageThreadReading,
				false => MessageThreadDie
			}).ToReadOnlyReactivePropertySlim<string>();
			this.CurrentReaderVisibility = this.IsReading.CombineLatest(this.InputUrl,
				(x, y) => { 
					if(!string.IsNullOrEmpty(y)) {
						// スレ読みは常に表示
						return Visibility.Visible;
					} else {
						return x switch {
							true => Visibility.Visible,
							false => Visibility.Collapsed,
						};
					}
				}).ToReadOnlyReactivePropertySlim();
			this.NextReaderVisibility = this.IsReading.CombineLatest(this.InputUrl,
				(x, y) => (!x && string.IsNullOrEmpty(y)) switch {
					true => Visibility.Visible,
					false => Visibility.Collapsed,
				}).ToReadOnlyReactivePropertySlim();

			this.NavigateBackCommand.Subscribe(() => {
				switch(this.Navigate.Value) {
				case Navigation.PostThread:
					this.BoardUrl.Value = "";
					this.Subject.Value = "";
					this.Name.Value = "";
					this.Mail.Value = "";
					this.Comment.Value = "";
					this.DelKey.Value = "";
					this.ImagePath.Value = "";
					this.Navigate.Value = Navigation.Start;
					break;
				case Navigation.InputUrl:
					this.InputUrl.Value = "";
					this.Navigate.Value = Navigation.Start;
					break;
				case Navigation.Reader:
					this.cancellationTokenSource?.Cancel();
					this.readerTask?.Dispose();
					this.cancellationTokenSource = null;
					this.readerTask = null;
					if(string.IsNullOrEmpty(this.InputUrl.Value)) {
						// スレ立てモード
						this.Navigate.Value = Navigation.PostThread;
					} else {
						// スレ読みモード
						this.Navigate.Value = Navigation.InputUrl;
					}
					break;
				}
			});
			this.OpenImageCommand.Subscribe(async () => {
				try {
					Application.Current.MainWindow.IsEnabled = false;
					/*
					var ext = Config.ConfigLoader.MimeFutaba.Types.Select(x => x.Ext);
					var ofd = new Microsoft.Win32.OpenFileDialog() {
						Filter = "ふたば画像ファイル|"
							+ string.Join(";", ext.Select(x => "*" + x).ToArray())
							+ "|すべてのファイル|*.*"
					};
					*/
					var ofd = new Microsoft.Win32.OpenFileDialog() {
						Filter = "すべてのファイル|*.*"
					};
					if(ofd.ShowDialog() ?? false) {
						this.ImagePath.Value = ofd.FileName;
						// ダイアログをダブルクリックで選択するとウィンドウに当たり判定がいくので
						// 一度待つ
						await Task.Delay(1);
					}
				}
				finally {
					Application.Current.MainWindow.IsEnabled = true;
				}
			});
			this.OpenBrowserCommand.Subscribe(() => {
				const int SW_SHOW = 5;
				var url =string.IsNullOrEmpty(this.InputUrl.Value) switch {
					true => $"{this.BoardUrl.Value}res/{this.threadNo}.htm",
					false => this.InputUrl.Value,
				};
				ShellExecute(IntPtr.Zero, null, url, null, null, SW_SHOW);
			});
			this.PostThreadCommand.Subscribe(() => {
				var board = Data.BoardData.From(
					name: "tmp",
					url: this.BoardUrl.Value,
					defaultComment: "きた",
					sortIndex: 100,
					extra: new Data.BoardDataExtra(),
					makimokiExtra: new Data.MakiMokiBoardDataExtra(),
					display: true);
				Util.FutabaApiReactive.PostThread(
					board: board,
					name: this.Name.Value,
					email: this.Mail.Value,
					subject: this.Subject.Value,
					comment: this.Comment.Value,
					filePath: this.ImagePath.Value,
					passwd: this.DelKey.Value,
					cookies: App.Current.Cookie,
					ptua: CreatePtua())
				.ObserveOn(UIDispatcherScheduler.Default)
				.Subscribe(x => {
					App.Current.Cookie = x.Cookies;
					if(x.Successed) {
						this.threadNo = x.NextOrMessage;
						ThreadReader.FireEvent(
							ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadCreated,
							ReaderConfigs.ConfigLoader.Config.SoundThreadCreated,
							ReaderConfigs.ConfigLoader.Config.MessageThreadCreated);
						Logger.Instance.Clear();
						Logger.Instance.Info($"スレッドが立ちました => {x.NextOrMessage}");
						if(this.StartRead()) {
							this.Navigate.Value = Navigation.Reader;
						}
					} else {
						MessageBox.Show(x.NextOrMessage);
					}
				});
			});

			this.NavigateToPostThreadCommand.Subscribe(() => {
				this.Navigate.Value = Navigation.PostThread;
			});

			this.NavigateToPostedThreadCommand.Subscribe(() => {
				this.Navigate.Value = Navigation.InputUrl;
			});

			this.StartReaderFromInputCommand.Subscribe(() => {
				Logger.Instance.Clear();
				if(this.StartRead()) {
					this.Navigate.Value = Navigation.Reader;
				}
			});
		}

		private bool StartRead() {
			this.cancellationTokenSource = new CancellationTokenSource();
			try {
				this.IsReading.Value = true;
				this.readerTask = ReaderUtils.ThreadReader.Start(string.IsNullOrEmpty(this.InputUrl.Value) switch {
					true => $"{this.BoardUrl.Value}res/{this.threadNo}.htm",
					false => this.InputUrl.Value,
				}, this.cancellationTokenSource, OnThreadDie);
				return true;
			}
			catch(InvalidOperationException e) {
				MessageBox.Show($"読み上げに失敗しました\r\n{e.Message}");
				return false;
			}
		}

		private void OnThreadDie() {
			this.IsReading.Value = false;
		}

		private static string CreatePtua() {
			var rnd = new Random();
			long t = 0;
			for(var i = 0; i < 33; i++) {
				t |= ((long)rnd.Next() % 2) << i;
			}
			return t.ToString();
		}
	}
}
