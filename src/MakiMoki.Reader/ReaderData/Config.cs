using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarukizero.Net.MakiMoki.Data;

namespace Yarukizero.Net.MakiMoki.Reader.ReaderData {
	internal class ReaderConfig : Data.ConfigObject {
		public static int CurrentVersion { get; } = 2023051801;

		[JsonProperty("bouyomichan-endpoint", Required = Required.Always)]
		public string BouyomiChanEndPoint { get; private set; } = "http://localhost:50080/";
		[JsonProperty("speak-id", Required = Required.Always)]
		public bool SpeakId { get; private set; } = false;
		[JsonProperty("speak-del", Required = Required.Always)]
		public bool SpeakDel { get; private set; } = false;
		[JsonProperty("enable-speak-thread-created", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadCreated { get; private set; } = SpeakMessage.BouyomiChan;
		[JsonProperty("enable-speak-start-read", Required = Required.Always)]
		public SpeakMessage EnabledSpeakStartRead { get; private set; } = SpeakMessage.BouyomiChan;
		[JsonProperty("enable-speak-image-save", Required = Required.Always)]
		public SpeakMessage EnabledSpeakImageSave { get; private set; } = SpeakMessage.BouyomiChan;
		[JsonProperty("enable-speak-thread-old", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadOld { get; private set; } = SpeakMessage.BouyomiChan;
		[JsonProperty("enable-speak-thread-die", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadDie { get; private set; } = SpeakMessage.BouyomiChan;

		[JsonIgnore]
		public string SoundThreadCreated { get; private set; } = "";
		[JsonIgnore]
		public string SoundStartRead { get; private set; } = "";
		[JsonIgnore]
		public string SoundImageSave { get; private set; } = "";
		[JsonIgnore]
		public string SoundThreadOld { get; private set; } = "";
		[JsonIgnore]
		public string SoundSpeakThreadDie { get; private set; } = "";


		[JsonProperty("bouyomichan-speak-thread-created", Required = Required.Always)]
		public string MessageThreadCreated { get; private set; } = "スレッドが立ちました。";
		[JsonProperty("bouyomichan-speak-start-read", Required = Required.Always)]
		public string MessageStartRead { get; private set; } = "読み上げを開始します。";
		[JsonProperty("bouyomichan-speak-image-save", Required = Required.Always)]
		public string MessageImageSave { get; private set; } = "手書きを保存しました。";
		[JsonProperty("bouyomichan-speak-thread-old", Required = Required.Always)]
		public string MessageThreadOld { get; private set; } = "もうすぐスレッドが落ちます。";
		[JsonProperty("bouyomichan-speak-thread-die", Required = Required.Always)]
		public string MessageSpeakThreadDie { get; private set; } = "スレッドが落ちました。読み上げを終了します。";

		[JsonProperty("fetch-api-waittime", Required = Required.Always)]
		public int FetchApiWaitTimeMiliSec { get; private set; } = 30 * 1000;

		public ReaderConfig() {
			this.Version = CurrentVersion;
		}
	}

	internal enum SpeakMessage {
		Disable,
		Wave,
		BouyomiChan,
	}

	internal class NgConfig : ConfigObject {
		public static int CurrentVersion { get; } = 2023051801;

		[JsonProperty("ng", Required = Required.Always)]
		public string[] NgWords { get; private set; } = Array.Empty<string>();

		[JsonProperty("ng-regex", Required = Required.Always)]
		public string[] NgRegex { get; private set; } = Array.Empty<string>();
	}
}
