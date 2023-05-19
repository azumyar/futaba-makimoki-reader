using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Yarukizero.Net.MakiMoki.Reader.ReaderData;
using Yarukizero.Net.MakiMoki.Util;

namespace Yarukizero.Net.MakiMoki.Config {
	internal static class ConfigLoader {
		public static class InitializedSetting {
			public static string RestUserAgent { get; } = "FutaMaki.Reader";
		}

		public static class Board {
			public static int MaxFileSize { get; } = 1024 * 3 * 1000;
		}

		public static class MimeFutaba {
			public static Dictionary<string, string> MimeTypes { get; } = new Dictionary<string, string>{
				{ ".jpg", "image/jpeg" },
				{ ".jpeg", "image/jpeg" },
				{ ".png", "image/png" },
				{ ".gif", "image/gif" },
				{ ".webp", "image/webp" },

				{ ".mp4", "video/mp4" },
				{ ".webm", "video/webm" },
			};
		}

		// あぷは使わないので雑に
		public static class MimeUp2 {
			public static Dictionary<string, string> MimeTypes { get; } = new Dictionary<string, string>{};
		}
	}
}


namespace Yarukizero.Net.MakiMoki.Reader.ReaderConfigs {
	internal static class ConfigLoader {
		public class Setting {
			public string ReaderDirectory { get; }
			public string SaveDirectory { get; }
			public string AppDataDirectory { get; }

			public HttpClient HttpClient { get; } = new HttpClient();

			internal Setting() {
				this.ReaderDirectory = "";
				this.SaveDirectory = "";
				this.AppDataDirectory = "";
			}

			public Setting(string readerDirectory, string saveDirectory, string appDataDirectory, HttpClient? httpClient=null) {
				this.ReaderDirectory = readerDirectory;
				this.SaveDirectory = saveDirectory;
				this.AppDataDirectory = appDataDirectory;

				if(httpClient!= null) {
					this.HttpClient = httpClient;
				}
			}
		}

		public const string ConfigAppFile = "makimoki.reader.app.json";
		public const string ConfigReaderFile = "makimoki.reader.config.json";
		public const string ConfigNgFile = "makimoki.reader.ng.json";


		public static (string Regex, string Root)[] UploaderRegex { get; private set; } = new[] {
			("f\\d+\\.[a-zA-Z0-9]+", "https://dec.2chan.net/up/src/"),
			("fu\\d+\\.[a-zA-Z0-9]+", "https://dec.2chan.net/up2/src/"),
		};

#nullable disable
		public static Setting InitializeSetting { get; private set; }
		public static ReaderData.ReaderConfig Config { get; private set; }
		public static ReaderData.NgConfig NgConfig { get; private set; }
		public static ReaderData.NgConfig NgSystemConfig { get; private set; }
		public static ReaderData.NgConfig NgUserConfig { get; private set; }
		public static ReaderData.AppConfig AppConfig { get; private set; }
#nullable enable

		public static void Initialize(Setting setting) {
			InitializeSetting = setting;
			{
				var sysConfig = Path.Combine(AppContext.BaseDirectory, "Config.d", ConfigReaderFile);
				var userConfig = Path.Combine(InitializeSetting.ReaderDirectory, ConfigReaderFile);
				var json = File.Exists(userConfig) switch {
					true => File.ReadAllText(userConfig),
					false => File.ReadAllText(sysConfig),
				};
				if(JsonConvert.DeserializeObject<ReaderData.ReaderConfig>(json) is ReaderData.ReaderConfig conf) {
					Config = conf;
				} else {
					throw new InvalidOperationException("初期化失敗");
				}
			}

			{
				var sysConfig = Path.Combine(AppContext.BaseDirectory, "Config.d", ConfigNgFile);
				var userConfig = Path.Combine(InitializeSetting.ReaderDirectory, ConfigNgFile);
				if(JsonConvert.DeserializeObject<ReaderData.NgConfig>(File.ReadAllText(sysConfig)) is ReaderData.NgConfig conf) {
					NgSystemConfig = conf;
					NgConfig = conf;
				} else {
					throw new InvalidOperationException("初期化失敗");
				}
				if(File.Exists(userConfig)) {
					if(JsonConvert.DeserializeObject<ReaderData.NgConfig>(File.ReadAllText(userConfig)) is ReaderData.NgConfig conf2) {
						NgUserConfig = conf2;
						NgConfig = new ReaderData.NgConfig(
							ngWords: conf.NgWords.Concat(conf2.NgWords).ToArray(),
							ngRegex: conf.NgWords.Concat(conf2.NgWords).ToArray());
					} else {
						throw new InvalidOperationException("初期化失敗");
					}
				} else {
					NgUserConfig = new NgConfig();
				}
			}

			{
				var config = Path.Combine(InitializeSetting.AppDataDirectory, ConfigAppFile);
				if(File.Exists(config)) {
					if(JsonConvert.DeserializeObject<ReaderData.AppConfig>(File.ReadAllText(config)) is ReaderData.AppConfig conf) {
						AppConfig = conf;
					} else {
						throw new InvalidOperationException("初期化失敗");
					}
				} else {
					AppConfig = new ReaderData.AppConfig() {
						Ptua = CreatePtua(),
					};
				}
			}
		}

		public static void UpdateCookie(string url, Data.Cookie2[] cookies) {
			lock(ConfigAppFile) {
				var l = AppConfig.Cookies.ToList();
				var uri = new Uri(url);
				foreach(var it in l.Where(x => uri.Host.EndsWith(x.Domain) && uri.AbsolutePath.StartsWith(x.Path)).ToArray()) {
					l.Remove(it);
				}
				l.AddRange(cookies);
				AppConfig.Cookies = l.ToArray();

				File.WriteAllText(
					Path.Combine(InitializeSetting.AppDataDirectory, ConfigAppFile),
					AppConfig.ToString(),
					Encoding.UTF8);
			}
		}

		public static void UpdateThreadBoard(string board, string password) {
			AppConfig.Board = board;
			AppConfig.Password = password;
			File.WriteAllText(
				Path.Combine(InitializeSetting.AppDataDirectory, ConfigAppFile),
				AppConfig.ToString(),
				Encoding.UTF8);
		}

		public static void UpdateConfig(
			string bouyomiChanEndPoint,
			bool speakId,
			bool speakDel,
			bool saveResImage,
			bool saveUploader,
			bool saveLog,
			SpeakMessage enabledSpeakThreadCreated,
			SpeakMessage enabledSpeakStartRead,
			SpeakMessage enabledSpeakImageSave,
			SpeakMessage enabledSpeakThreadOld,
			SpeakMessage enabledSpeakThreadDie,
			string soundThreadCreated,
			string soundStartRead,
			string soundImageSave,
			string soundThreadOld,
			string soundSpeakThreadDie,
			string messageThreadCreated,
			string messageStartRead,
			string messageImageSave,
			string messageThreadOld,
			string messageSpeakThreadDie,
			int waitTimeOldThreadMiliSec,
			int waitTimeFetchApiMiliSec,

			string[] ngWords,
			string[] ngRegex
			) {

			Config = new ReaderConfig(
				bouyomiChanEndPoint: bouyomiChanEndPoint,
				speakId: speakId,
				speakDel: speakDel,
				saveResImage: saveResImage,
				saveUploader: saveUploader,
				saveLog: saveLog,
				enabledSpeakThreadCreated: enabledSpeakThreadCreated,
				enabledSpeakStartRead: enabledSpeakStartRead,
				enabledSpeakImageSave: enabledSpeakImageSave,
				enabledSpeakThreadOld: enabledSpeakThreadOld,
				enabledSpeakThreadDie: enabledSpeakThreadDie,
				soundThreadCreated: soundThreadCreated,
				soundStartRead: soundStartRead,
				soundImageSave: soundImageSave,
				soundThreadOld: soundThreadOld,
				soundSpeakThreadDie: soundSpeakThreadDie,
				messageThreadCreated: messageThreadCreated,
				messageStartRead: messageStartRead,
				messageImageSave: messageImageSave,
				messageThreadOld: messageThreadOld,
				messageSpeakThreadDie: messageSpeakThreadDie,
				waitTimeOldThreadMiliSec: waitTimeOldThreadMiliSec,
				waitTimeFetchApiMiliSec: waitTimeFetchApiMiliSec
				);
			NgUserConfig = new NgConfig(
				ngWords: ngWords,
				ngRegex: ngRegex
				);
			NgConfig = new ReaderData.NgConfig(
				ngWords: NgSystemConfig.NgWords.Concat(NgUserConfig.NgWords).ToArray(),
				ngRegex: NgSystemConfig.NgWords.Concat(NgUserConfig.NgWords).ToArray());


			File.WriteAllText(
				Path.Combine(InitializeSetting.ReaderDirectory, ConfigReaderFile),
				Config.ToString(),
				Encoding.UTF8);
			File.WriteAllText(
				Path.Combine(InitializeSetting.ReaderDirectory, ConfigNgFile),
				NgUserConfig.ToString(),
				Encoding.UTF8);
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
