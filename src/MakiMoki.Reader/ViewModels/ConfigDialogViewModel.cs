using Prism.Services.Dialogs;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Yarukizero.Net.MakiMoki.Reader.ReaderData;
using static Yarukizero.Net.MakiMoki.Config.ConfigLoader;

namespace Yarukizero.Net.MakiMoki.Reader.ViewModels {
	internal class ConfigDialogViewModel : IDialogAware {
		public string Title { get; } = "設定";

#nullable disable
		public event Action<IDialogResult> RequestClose;
#nullable enable

		private IDialogService DialogService { get; }

		public ReactivePropertySlim<string> BouyomiChanUrl { get; }
		public ReactivePropertySlim<bool> EnableSpeakId { get; }
		public ReactivePropertySlim<bool> EnableSpeakDel { get; }
		public ReactivePropertySlim<bool> EnableSaveResImage { get; }
		public ReactivePropertySlim<bool> EnableSaveUploadFile { get; }
		public ReactivePropertySlim<bool> EnableSaveLog { get; }
		public ReactivePropertySlim<int> EnabledSpeakThreadCreated { get; }
		public ReactivePropertySlim<string> SoundSpeakThreadCreated { get; }
		public ReactivePropertySlim<string> MessageSpeakThreadCreated { get; }
		public ReactivePropertySlim<int> EnabledSpeakStartRead { get; }
		public ReactivePropertySlim<string> SoundSpeakStartRead { get; }
		public ReactivePropertySlim<string> MessageSpeakStartRead { get; }
		public ReactivePropertySlim<int> EnabledSpeakImageSave { get; }
		public ReactivePropertySlim<string> SoundSpeakImageSave { get; }
		public ReactivePropertySlim<string> MessageSpeakImageSave { get; }
		public ReactivePropertySlim<int> EnabledSpeakThreadOld { get; }
		public ReactivePropertySlim<string> SoundSpeakThreadOld { get; }
		public ReactivePropertySlim<string> MessageSpeakThreadOld { get; }
		public ReactivePropertySlim<int> EnabledSpeakThreadDie { get; }
		public ReactivePropertySlim<string> SoundSpeakThreadDie { get; }
		public ReactivePropertySlim<string> MessageSpeakThreadDie { get; }
		public ReactivePropertySlim<string> WaitTimeOldThread { get; }
		public ReactivePropertySlim<string> WaitTimeFetchApi { get; }
		public ReactivePropertySlim<string> NgWord { get; }
		public ReactivePropertySlim<string> NgRegex { get; }

		public ReactiveCommandSlim FileSpeakThreadCreatedClickCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim FileSpeakStartReadClickCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim FileSpeakImageSaveClickCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim FileSpeakThreadOldClickCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim FileSpeakThreadDieClickCommand { get; } = new ReactiveCommandSlim();
		public ReactiveCommandSlim SaveClickCommand { get; } = new ReactiveCommandSlim();


		public ConfigDialogViewModel(IDialogService dialogService) {
			this.DialogService = dialogService;

			this.BouyomiChanUrl = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.BouyomiChanEndPoint);
			this.EnableSpeakId = new ReactivePropertySlim<bool>(initialValue: ReaderConfigs.ConfigLoader.Config.IsSpeakId);
			this.EnableSpeakDel = new ReactivePropertySlim<bool>(initialValue: ReaderConfigs.ConfigLoader.Config.IsSpeakDel);
			this.EnableSaveResImage = new ReactivePropertySlim<bool>(initialValue: ReaderConfigs.ConfigLoader.Config.EnabledSaveResImage);
			this.EnableSaveUploadFile = new ReactivePropertySlim<bool>(initialValue: ReaderConfigs.ConfigLoader.Config.EnabledSaveUploadFile);
			this.EnableSaveLog = new ReactivePropertySlim<bool>(initialValue: ReaderConfigs.ConfigLoader.Config.EnabledSaveLog);
			this.EnabledSpeakThreadCreated = new ReactivePropertySlim<int>(initialValue: (int)ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadCreated);
			this.EnabledSpeakStartRead = new ReactivePropertySlim<int>(initialValue: (int)ReaderConfigs.ConfigLoader.Config.EnabledSpeakStartRead);
			this.EnabledSpeakImageSave = new ReactivePropertySlim<int>(initialValue: (int)ReaderConfigs.ConfigLoader.Config.EnabledSpeakImageSave);
			this.EnabledSpeakThreadOld = new ReactivePropertySlim<int>(initialValue: (int)ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadOld);
			this.EnabledSpeakThreadDie = new ReactivePropertySlim<int>(initialValue: (int)ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadDie);
			this.SoundSpeakThreadCreated = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.SoundThreadCreated);
			this.SoundSpeakStartRead = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.SoundStartRead);
			this.SoundSpeakImageSave = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.SoundImageSave);
			this.SoundSpeakThreadOld = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.SoundThreadOld);
			this.SoundSpeakThreadDie = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.SoundSpeakThreadDie);
			this.MessageSpeakThreadCreated = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.MessageThreadCreated);
			this.MessageSpeakStartRead = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.MessageStartRead);
			this.MessageSpeakImageSave = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.MessageImageSave);
			this.MessageSpeakThreadOld = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.MessageThreadOld);
			this.MessageSpeakThreadDie = new ReactivePropertySlim<string>(initialValue: ReaderConfigs.ConfigLoader.Config.MessageSpeakThreadDie);
			this.WaitTimeOldThread = new ReactivePropertySlim<string>(initialValue: (ReaderConfigs.ConfigLoader.Config.WaitTimeOldThreadMiliSec / 1000).ToString());
			this.WaitTimeFetchApi = new ReactivePropertySlim<string>(initialValue: (ReaderConfigs.ConfigLoader.Config.WaitTimeFetchApiMiliSec / 1000).ToString());
	
			this.NgWord = new ReactivePropertySlim<string>(initialValue: string.Join(Environment.NewLine, ReaderConfigs.ConfigLoader.NgUserConfig.NgWords));
			this.NgRegex = new ReactivePropertySlim<string>(initialValue: string.Join(Environment.NewLine, ReaderConfigs.ConfigLoader.NgUserConfig.NgRegex));

			this.FileSpeakThreadCreatedClickCommand.Subscribe(async () => {
				if(await this.OpenFileDialog() is string file) {
					this.SoundSpeakThreadCreated.Value = file;
				}
			});
			this.FileSpeakStartReadClickCommand.Subscribe(async () => {
				if(await this.OpenFileDialog() is string file) {
					this.SoundSpeakStartRead.Value = file;
				}
			});
			this.FileSpeakImageSaveClickCommand.Subscribe(async () => {
				if(await this.OpenFileDialog() is string file) {
					this.SoundSpeakImageSave.Value = file;
				}
			});
			this.FileSpeakThreadOldClickCommand.Subscribe(async () => {
				if(await this.OpenFileDialog() is string file) {
					this.SoundSpeakThreadOld.Value = file;
				}
			});
			this.FileSpeakThreadDieClickCommand.Subscribe(async () => {
				if(await this.OpenFileDialog() is string file) {
					this.SoundSpeakThreadDie.Value = file;
				}
			});

			this.SaveClickCommand.Subscribe(() => this.OnSave());
		}

		private void OnSave() {
			if(!int.TryParse(this.WaitTimeOldThread.Value, out var waitTimeOldThread) || (waitTimeOldThread <= 0)) {
				MessageBox.Show("スレ落ち警告は1以上の整数を入力してください");
				return;
			}
			if(!int.TryParse(this.WaitTimeFetchApi.Value, out var waitTimeFetchApi) || (waitTimeFetchApi <= 0)) {
				MessageBox.Show("スレ取得間隔は1以上の整数を入力してください");
				return;
			}
			waitTimeOldThread *= 1000;
			waitTimeFetchApi *= 1000;

			ReaderConfigs.ConfigLoader.UpdateConfig(
				bouyomiChanEndPoint: this.BouyomiChanUrl.Value,
				speakId: this.EnableSpeakId.Value,
				speakDel: this.EnableSpeakDel.Value,
				saveResImage: this.EnableSaveResImage.Value,
				saveUploader: this.EnableSaveUploadFile.Value,
				saveLog: this.EnableSaveLog.Value,
				enabledSpeakThreadCreated: (SpeakMessage)this.EnabledSpeakThreadCreated.Value,
				enabledSpeakStartRead: (SpeakMessage)this.EnabledSpeakStartRead.Value,
				enabledSpeakImageSave: (SpeakMessage)this.EnabledSpeakImageSave.Value,
				enabledSpeakThreadOld: (SpeakMessage)this.EnabledSpeakThreadOld.Value,
				enabledSpeakThreadDie: (SpeakMessage)this.EnabledSpeakThreadDie.Value,
				soundThreadCreated: this.SoundSpeakThreadCreated.Value,
				soundStartRead: this.SoundSpeakStartRead.Value,
				soundImageSave: this.SoundSpeakImageSave.Value,
				soundThreadOld: this.SoundSpeakThreadOld.Value,
				soundSpeakThreadDie: this.SoundSpeakThreadDie.Value,
				messageThreadCreated: this.MessageSpeakThreadCreated.Value,
				messageStartRead: this.MessageSpeakStartRead.Value,
				messageImageSave: this.MessageSpeakImageSave.Value,
				messageThreadOld: this.MessageSpeakThreadOld.Value,
				messageSpeakThreadDie: this.MessageSpeakThreadDie.Value,
				waitTimeOldThreadMiliSec: waitTimeOldThread,
				waitTimeFetchApiMiliSec: waitTimeFetchApi,

				ngWords: string.IsNullOrEmpty(this.NgWord.Value) switch {
					true => Array.Empty<string>(),
					false => this.NgWord.Value.Replace("\r\n", "\n").Split("\n"),
				},
				ngRegex: string.IsNullOrEmpty(this.NgRegex.Value) switch {
					true => Array.Empty<string>(),
					false => this.NgRegex.Value.Replace("\r\n", "\n").Split("\n"),
				});
			RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
		}

		public bool CanCloseDialog() {
			return true;
		}

		public void OnDialogClosed() {}

		public void OnDialogOpened(IDialogParameters parameters) {}

		private async Task<string?> OpenFileDialog() {
			var ofd = new Microsoft.Win32.OpenFileDialog() {
				Filter = "サウンドファイル|*.wav;*.mp3"
			};
			if(ofd.ShowDialog() ?? false) {
				await Task.Delay(1);
				return ofd.FileName;
			} else {
				return null;
			}
		}
	}
}
