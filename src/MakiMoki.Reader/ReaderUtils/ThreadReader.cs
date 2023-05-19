using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using RestSharp;
using System.Web;
using System.Net.Sockets;
using System.IO;
using Unity.Injection;
using System.ComponentModel.Design.Serialization;
using static Yarukizero.Net.MakiMoki.Config.ConfigLoader;
using Yarukizero.Net.MakiMoki.Data;
using static System.Net.Mime.MediaTypeNames;

namespace Yarukizero.Net.MakiMoki.Reader.ReaderUtils {
	internal static class ThreadReader {
		static System.Reactive.Concurrency.EventLoopScheduler FutabaScheduler { get; }
			= new System.Reactive.Concurrency.EventLoopScheduler();
		static System.Reactive.Concurrency.EventLoopScheduler BouyomiChanScheduler { get; }
			= new System.Reactive.Concurrency.EventLoopScheduler();
		static System.Reactive.Concurrency.EventLoopScheduler FileSaveScheduler { get; }
			= new System.Reactive.Concurrency.EventLoopScheduler();

		public static IDisposable Start(string url, CancellationTokenSource cancelSource, Action threadDieAction) {
			var m = Regex.Match(url, @"^(.*/)res/(\d+)\.htm$");
			if(!m.Success) {
				throw new InvalidOperationException("URLが不正");
			}
			var mm = Regex.Match(m.Groups[1].Value, @"^.+//([^\.]+)\.2chan\.net/([^/]+)/$");
			if(!mm.Success) {
				throw new InvalidOperationException("URLが不正");
			}

			var board = Data.BoardData.From(
				name: "tmp",
				url: m.Groups[1].Value,
				defaultComment: "きた",
				sortIndex: 100,
				extra: new Data.BoardDataExtra(),
				makimokiExtra: new Data.MakiMokiBoardDataExtra(),
				display: true);
			var threadNo = m.Groups[2].Value;
			//var urlCtx = new Data.UrlContext(m.Groups[1].Value, m.Groups[2].Value);
			return Observable.Create<string>(o => {
				var dir = Path.Combine(ReaderConfigs.ConfigLoader.InitializeSetting.SaveDirectory, $"{mm.Groups[1].Value}_{mm.Groups[2].Value}_{threadNo}");
				try {
					File.WriteAllText(Path.Combine(ReaderConfigs.ConfigLoader.InitializeSetting.SaveDirectory, "save.txt"), threadNo);
				}
				catch(IOException) { }
				Task.Run(async () => {
					var isOld = false;
					var futabaCtx = Data.FutabaContext.FromThreadEmpty(board, threadNo);
					Logger.Instance.Info("読み上げを開始します");
					FireEvent(
						ReaderConfigs.ConfigLoader.Config.EnabledSpeakStartRead,
						ReaderConfigs.ConfigLoader.Config.SoundStartRead,
						ReaderConfigs.ConfigLoader.Config.MessageStartRead);
					while(!cancelSource.IsCancellationRequested && !(futabaCtx.Raw?.IsDie ?? false)) {
						Logger.Instance.Debug("スレッド取得開始");
						Util.FutabaApiReactive.GetThreadRes(board, threadNo, futabaCtx, App.Current.Cookie, true)
							.ObserveOn(FutabaScheduler)
							.Subscribe(x => {
								App.Current.Cookie = x.Cookies;
								Logger.Instance.Debug("スレッド取得終了");
								if(x.Successed) {
									futabaCtx = x.Data;
									if(x.RawResponse.Res == null) {
										Logger.Instance.Info($"新着レスなし");
									} else {
										Logger.Instance.Info($"{x.RawResponse.Res.Length}件の新しいレス");
										foreach(var res in x.RawResponse.Res) {
											var id = string.IsNullOrEmpty(res.Res.Id) switch {
												true => true,
												false when ReaderConfigs.ConfigLoader.Config.SpeakId => true,
												false => false
											};
											var del = (res.Res.IsDel || res.Res.IsDel2 || res.Res.IsSelfDel) switch {
												false => true,
												true when ReaderConfigs.ConfigLoader.Config.SpeakDel => true,
												true => false
											};

											if(id && del) {
												var com = Html2Text(res.Res.Com);
												o.OnNext(com);
												DownloadResImage(board, res, dir);
												DownloadUploader(com, dir);
											} else {
												Logger.Instance.Info($"id/delで読み上げがスキップされました");
											}
										}
									}
									if(!isOld && x.RawResponse.DieDateTime.HasValue && x.RawResponse.NowDateTime.HasValue) {
										if((x.RawResponse.DieDateTime.Value - x.RawResponse.NowDateTime.Value).TotalMilliseconds < 5 * 60 * 1000) {
											Logger.Instance.Info($"スレ落ち警告");
											FireEvent(
												ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadOld,
												ReaderConfigs.ConfigLoader.Config.SoundThreadOld,
												ReaderConfigs.ConfigLoader.Config.MessageThreadOld);
											isOld = true;
										}
									}
									if(x.RawResponse.IsDie) {
										Logger.Instance.Info($"スレッドが落ちました");
										FireEvent(
											ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadDie,
											ReaderConfigs.ConfigLoader.Config.SoundSpeakThreadDie,
											ReaderConfigs.ConfigLoader.Config.MessageSpeakThreadDie);
										Observable.Return(0)
											.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
											.Subscribe(_ => {
												threadDieAction();
											});
									}
								}

							});
						try {
							await Task.Delay(Math.Max(ReaderConfigs.ConfigLoader.Config.FetchApiWaitTimeMiliSec, 30 * 1000), cancelSource.Token);
						}
						catch (Exception ex) { }
					}
				});

				return System.Reactive.Disposables.Disposable.Empty;
			}).Subscribe(m => {
				foreach(var line in m.Replace("\r\n", "\n").Split("\n")) {
					foreach(var ng in ReaderConfigs.ConfigLoader.NgConfig.NgWords) {
						if(0 <= line.IndexOf(ng)) {
							Logger.Instance.Info($"NGワード牴触で読み上げスキップ");
							goto skip;
						}
					}
					foreach(var ng in ReaderConfigs.ConfigLoader.NgConfig.NgRegex) {
						if(Regex.IsMatch(line, ng)) {
							Logger.Instance.Info($"NG正規表現牴触で読み上げスキップ");
							goto skip;
						}
					}
					Speak(line);
				skip:;
				}
			});
		}


		public static void Speak(string text) {
			Logger.Instance.Debug($"棒読みちゃんトークプッシュ({text})");
			Observable.Return(text)
				.ObserveOn(BouyomiChanScheduler)
				.Subscribe(m => {
					Logger.Instance.Debug($"棒読みちゃんトーク開始({m})");
					foreach(var line in m.Replace("\r\n", "\n")
						.Split("\n")
						.Select(x => x.Replace('%', '％').Replace('&', '＆').Replace('?', '？'))) {

						try {
							// awaitだとスレッドスタックが変わるのでちゃんとwaitする
							var r = ReaderConfigs.ConfigLoader.InitializeSetting.HttpClient.GetAsync(
								$"{ReaderConfigs.ConfigLoader.Config.BouyomiChanEndPoint}Talk?text={line}");
							r.Wait();
							if(r.Result.StatusCode != System.Net.HttpStatusCode.OK) {
								Logger.Instance.Info($"棒読みちゃんでエラー({(int)r.Result.StatusCode})");
							}
						}
						catch(AggregateException) {
							Logger.Instance.Info($"棒読みちゃんとの通信に失敗");
						}
						/*
						catch(Exception e) when(e is SocketException || e is TimeoutException) {
							Logger.Instance.Info($"棒読みちゃんとの通信に失敗");
						}
						*/
					}
				});
		}

		private static void PlaySound() {

		}

		private static string Html2Text(string html) {
			return Util.TextUtil.RowComment2Text(html);
		}

		private static void DownloadResImage(BoardData board, Data.NumberedResItem res, string dir) {
			if(res.Res.IsHavedImage) {
				Logger.Instance.Info($"レス画像を取得 => {res.Res.Src}");
				Observable.Return(res.Res)
					.Select(async y => {
						return (Name: y.Src.Replace('/', '_'), File: await Util.FutabaApi.GetThreadResImage(board.Url, y));
					})
					.ObserveOn(FileSaveScheduler)
					.Subscribe(y => {
						if(y.Result.File == null) {
							Logger.Instance.Info($"レス画像取得に失敗 => {res.Res.Src}");
						} else {
							try {
								if(!Directory.Exists(dir)) {
									Directory.CreateDirectory(dir);
								}
								var f = Path.Combine(dir, y.Result.Name);
								var save = Path.Combine(ReaderConfigs.ConfigLoader.InitializeSetting.SaveDirectory, "save.png");
								File.WriteAllBytes(f, y.Result.File);

								if(Path.GetExtension(f).ToLower() == ".png") {
									File.Copy(f, save, true);
								} else {
									using var b = System.Drawing.Bitmap.FromFile(f);
									b.Save(save);
								}
								Logger.Instance.Info($"レス画像を保存しました => {f}");
								FireEvent(
									ReaderConfigs.ConfigLoader.Config.EnabledSpeakImageSave,
									ReaderConfigs.ConfigLoader.Config.SoundImageSave,
									ReaderConfigs.ConfigLoader.Config.MessageImageSave);
							}
							catch(IOException e) {
								Logger.Instance.Info($"レス画像を保存でエラー\r\n{e.ToString()}");
							}
						}
					});
			}
		}

		private static void DownloadUploader(string com, string dir) {
			foreach(var it in ReaderConfigs.ConfigLoader.UploaderRegex) {
				foreach(Match m in Regex.Matches(com, it.Regex, RegexOptions.Multiline)) {
					Logger.Instance.Info($"アップロードファイルを取得 => {m.Value}");
					Observable.Return((Name: m.Value, Root: it.Root))
						.ObserveOn(System.Reactive.Concurrency.NewThreadScheduler.Default)
						.Select<(string Name, string Root), (string?, byte[]?)>(y => {
							try {
								var c = new RestClient($"{y.Root}{y.Name}") {
									UserAgent = Config.ConfigLoader.InitializedSetting.RestUserAgent,
									Timeout = 5000,
								};
								var ret = c.Execute(new RestRequest(Method.GET));
								if(ret.StatusCode == System.Net.HttpStatusCode.OK) {
									return (y.Name, ret.RawBytes);
								}
							}
							catch(Exception e) when(e is SocketException || e is TimeoutException) {
								// TODO: エラー処理
							}
							return (null, null);
						}).ObserveOn(FileSaveScheduler)
					.Subscribe<(string? Name, byte[]? File)>(y => {
						if((y.Name != null) && (y.File != null)) {
							try {
								if(!Directory.Exists(dir)) {
									Directory.CreateDirectory(dir);
								}
								var f = Path.Combine(dir, y.Name);
								File.WriteAllBytes(f, y.File);
								Logger.Instance.Info($"アップロードファイルを保存しました => {f}");
								FireEvent(
									ReaderConfigs.ConfigLoader.Config.EnabledSpeakImageSave,
									ReaderConfigs.ConfigLoader.Config.SoundImageSave,
									ReaderConfigs.ConfigLoader.Config.MessageImageSave);
							}
							catch(IOException e) {
								Logger.Instance.Info($"アップロードファイルを保存でエラー\r\n{e.ToString()}");
							}
						}
					});
				}
			}
		}

		public static void FireEvent(ReaderData.SpeakMessage msg, string sound, string message) {
			switch(msg) {
			case ReaderData.SpeakMessage.Wave:
				break;
			case ReaderData.SpeakMessage.BouyomiChan:
				Speak(message);
				break;
			}
		}
	}
}
