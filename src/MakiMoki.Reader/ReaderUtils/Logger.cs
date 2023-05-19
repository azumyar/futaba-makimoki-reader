using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarukizero.Net.MakiMoki.Reader.ReaderUtils {
	internal class Logger {
		public static Logger Instance { get; } = new Logger();

		public ReadOnlyReactivePropertySlim<string> Log { get; }
		private ReactivePropertySlim<string> LogInput { get; } = new ReactivePropertySlim<string>(initialValue: "");
		private readonly StringBuilder log = new StringBuilder();

		private Logger() {
			this.Log = LogInput.Select(x => x).ToReadOnlyReactivePropertySlim<string>();;
		}

		public void Clear() {
			Observable.Return("")
				.ObserveOn(UIDispatcherScheduler.Default)
				.Subscribe(x => {
					log.Clear();
					this.LogInput.Value = log.ToString();
				});
		}

		public void Info(string message) {
			Observable.Return(message)
				.ObserveOn(UIDispatcherScheduler.Default)
				.Subscribe(x => {
					Format(this.log, message);
					this.LogInput.Value = log.ToString();
				});
		}

		public void Debug(string message) {
#if DEBUG
			Observable.Return(message)
				.ObserveOn(UIDispatcherScheduler.Default)
				.Subscribe(x => {
					Format(this.log, message);
					this.LogInput.Value = log.ToString();
				});
#endif
		}

		public void Format(StringBuilder dst, string message) {
			var date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
			foreach(var line in message.Replace("\r\n", "\n").Split('\n')) {
				dst.AppendLine($"{date} {line}");
#if DEBUG
				System.Diagnostics.Debug.WriteLine(line);
#endif
			}
		}
	}
}
