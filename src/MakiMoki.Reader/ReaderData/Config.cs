using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarukizero.Net.MakiMoki.Data;

namespace Yarukizero.Net.MakiMoki.Reader.ReaderData {
	internal class AppConfig : Data.ConfigObject {
		public static int CurrentVersion { get; } = 2023052001;

		[JsonProperty("cookie", Required = Required.Always)]
		public Data.Cookie2[] Cookies { get; set; } = Array.Empty<Data.Cookie2>();
		[JsonProperty("ptua", Required = Required.Always)]
		public string Ptua { get; set; } = "";
		[JsonProperty("board", Required = Required.Always)]
		public string Board { get; set; } = "";
		[JsonProperty("password", Required = Required.Always)]
		public string Password { get; set; } = "";
		
	}

	internal class ReaderConfig : Data.ConfigObject {
		public static int CurrentVersion { get; } = 2023052001;

		[JsonProperty("bouyomichan-endpoint", Required = Required.Always)]
		public string BouyomiChanEndPoint { get; private set; } = "http://localhost:50080/";
		[JsonProperty("speak-id", Required = Required.Always)]
		public bool IsSpeakId { get; private set; } = false;
		[JsonProperty("speak-del", Required = Required.Always)]
		public bool IsSpeakDel { get; private set; } = false;

		[JsonProperty("save-res-image", Required = Required.Always)]
		public bool EnabledSaveResImage { get; private set; } = false;
		[JsonProperty("save-uploader", Required = Required.Always)]
		public bool EnabledSaveUploadFile { get; private set; } = false;
		[JsonProperty("save-log", Required = Required.Always)]
		public bool EnabledSaveLog { get; private set; } = false;
		[JsonProperty("enable-speak-thread-created", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadCreated { get; private set; } = SpeakMessage.Disable;
		[JsonProperty("enable-speak-start-read", Required = Required.Always)]
		public SpeakMessage EnabledSpeakStartRead { get; private set; } = SpeakMessage.Disable;
		[JsonProperty("enable-speak-image-save", Required = Required.Always)]
		public SpeakMessage EnabledSpeakImageSave { get; private set; } = SpeakMessage.Disable;
		[JsonProperty("enable-speak-thread-old", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadOld { get; private set; } = SpeakMessage.Disable;
		[JsonProperty("enable-speak-thread-die", Required = Required.Always)]
		public SpeakMessage EnabledSpeakThreadDie { get; private set; } = SpeakMessage.Disable;

		[JsonProperty("sound-speak-thread-created", Required = Required.Always)]
		public string SoundThreadCreated { get; private set; } = "";
		[JsonProperty("sound-speak-start-read", Required = Required.Always)]
		public string SoundStartRead { get; private set; } = "";
		[JsonProperty("sound-speak-image-save", Required = Required.Always)]
		public string SoundImageSave { get; private set; } = "";
		[JsonProperty("sound-speak-thread-old", Required = Required.Always)]
		public string SoundThreadOld { get; private set; } = "";
		[JsonProperty("sound-speak-thread-die", Required = Required.Always)]
		public string SoundSpeakThreadDie { get; private set; } = "";

		[JsonProperty("bouyomichan-speak-thread-created", Required = Required.Always)]
		public string MessageThreadCreated { get; private set; } = "";
		[JsonProperty("bouyomichan-speak-start-read", Required = Required.Always)]
		public string MessageStartRead { get; private set; } = "";
		[JsonProperty("bouyomichan-speak-image-save", Required = Required.Always)]
		public string MessageImageSave { get; private set; } = "";
		[JsonProperty("bouyomichan-speak-thread-old", Required = Required.Always)]
		public string MessageThreadOld { get; private set; } = "";
		[JsonProperty("bouyomichan-speak-thread-die", Required = Required.Always)]
		public string MessageSpeakThreadDie { get; private set; } = "";
		[JsonProperty("waittime-thread-old", Required = Required.Always)]
		public int WaitTimeOldThreadMiliSec { get; private set; } = 5 * 60 * 1000;
		[JsonProperty("waittime-fetch-api", Required = Required.Always)]
		public int WaitTimeFetchApiMiliSec { get; private set; } = 30 * 1000;

		public ReaderConfig() {
			this.Version = CurrentVersion;
		}

		public ReaderConfig(
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
			int waitTimeFetchApiMiliSec
			) {

			this.Version = CurrentVersion;
			this.BouyomiChanEndPoint = bouyomiChanEndPoint;
			this.IsSpeakId = speakId;
			this.IsSpeakDel = speakDel;
			this.EnabledSaveResImage = saveResImage;
			this.EnabledSaveUploadFile = saveUploader;
			this.EnabledSaveLog = saveResImage;
			this.EnabledSpeakThreadCreated = enabledSpeakThreadCreated;
			this.EnabledSpeakStartRead = enabledSpeakStartRead;
			this.EnabledSpeakImageSave = enabledSpeakImageSave;
			this.EnabledSpeakThreadOld = enabledSpeakThreadOld;
			this.EnabledSpeakThreadDie = enabledSpeakThreadDie;
			this.SoundThreadCreated = soundThreadCreated;
			this.SoundStartRead = soundStartRead;
			this.SoundImageSave = soundImageSave;
			this.SoundThreadOld = soundThreadOld;
			this.SoundSpeakThreadDie = soundSpeakThreadDie;
			this.MessageThreadCreated = messageThreadCreated;
			this.MessageStartRead = messageStartRead;
			this.MessageImageSave = messageImageSave;
			this.MessageThreadOld = messageThreadOld;
			this.MessageSpeakThreadDie = messageSpeakThreadDie;
			this.WaitTimeOldThreadMiliSec = waitTimeOldThreadMiliSec;
			this.WaitTimeFetchApiMiliSec = waitTimeFetchApiMiliSec;
		}
	}

	internal enum SpeakMessage {
		Disable,
		Sound,
		BouyomiChan,
	}

	internal class NgConfig : ConfigObject {
		public static int CurrentVersion { get; } = 2023051801;

		[JsonProperty("ng", Required = Required.Always)]
		public string[] NgWords { get; private set; }

		[JsonProperty("ng-regex", Required = Required.Always)]
		public string[] NgRegex { get; private set; }

		public NgConfig() {
			this.Version = CurrentVersion;
			this.NgWords = Array.Empty<string>();
			this.NgRegex = Array.Empty<string>();
		}

		public NgConfig(
			string[] ngWords,
			string[] ngRegex
			) {
			this.Version = CurrentVersion;
			this.NgWords = ngWords;
			this.NgRegex = ngRegex;
		}

	}
}
