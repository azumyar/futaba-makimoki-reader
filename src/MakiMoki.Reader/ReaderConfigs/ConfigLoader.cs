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

			public HttpClient HttpClient { get; } = new HttpClient();

			internal Setting() {
				this.ReaderDirectory = "";
				this.SaveDirectory = "";
			}

			public Setting(string readerDirectory, string saveDirectory, HttpClient? httpClient=null) {
				this.ReaderDirectory = readerDirectory;
				this.SaveDirectory = saveDirectory;

				if(httpClient!= null) {
					this.HttpClient = httpClient;
				}
			}
		}



		public static ReaderData.ReaderConfig Config { get; private set; } = new ReaderData.ReaderConfig();
		public static ReaderData.NgConfig NgConfig { get; private set; } = new ReaderData.NgConfig();

		public static (string Regex, string Root)[] UploaderRegex { get; private set; } = new[] {
			("f\\d+\\.[a-zA-Z0-9]+", "https://dec.2chan.net/up/src/"),
			("fu\\d+\\.[a-zA-Z0-9]+", "https://dec.2chan.net/up2/src/"),
		};

#nullable disable
		public static Setting InitializeSetting { get; private set; }
#nullable enable

		public static void Initialize(Setting setting) {
			InitializeSetting = setting;
			{
				var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.d", "makimoki.reader.config.json"));
				if(JsonConvert.DeserializeObject<ReaderData.ReaderConfig>(json) is ReaderData.ReaderConfig conf) {
					Config = conf;
				} else {
					throw new InvalidOperationException("初期化失敗");
				}
			}

			{
				var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.d", "makimoki.reader.ng.json"));
				if(JsonConvert.DeserializeObject<ReaderData.NgConfig>(json) is ReaderData.NgConfig conf) {
					NgConfig = conf;
				} else {
					throw new InvalidOperationException("初期化失敗");
				}
			}
		}
	}
}
