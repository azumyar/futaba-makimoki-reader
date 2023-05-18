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
				var dir = Path.Combine(App.Current.RederDirectory, threadNo);
				try {
					File.WriteAllText(Path.Combine(App.Current.RederDirectory, "save.txt"), threadNo);
				}
				catch(IOException) { }
				Task.Run(async () => {
					var isOld = false;
					var futabaCtx = Data.FutabaContext.FromThreadEmpty(board, threadNo);
					while(!cancelSource.IsCancellationRequested && !(futabaCtx.Raw?.IsDie ?? false)) {
						Util.FutabaApiReactive.GetThreadRes(board, threadNo, futabaCtx, App.Current.Cookie, true)
							.ObserveOn(FutabaScheduler)
							.Subscribe(async x => {
								App.Current.Cookie = x.Cookies;
								if(x.Successed) {
									futabaCtx = x.Data;
									if(x.RawResponse.Res != null) {
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
												o.OnNext(Html2Text(res.Res.Com));
												if(res.Res.IsHavedImage) {
													Observable.Return(res.Res)
														.Select(async y => {
															return (Name: y.Src.Replace('/', '_'), File: await Util.FutabaApi.GetThreadResImage(board.Url, y));
														})
														.ObserveOn(FileSaveScheduler)
														.Subscribe(y => {
															if(y.Result.File != null) {
																try {
																	if(!Directory.Exists(dir)) {
																		Directory.CreateDirectory(dir);
																	}
																	var f = Path.Combine(dir, y.Result.Name);
																	File.WriteAllBytes(f, y.Result.File);

																	using var b = System.Drawing.Bitmap.FromFile(f);
																	b.Save(Path.Combine(App.Current.RederDirectory, "save.png"));
																}
																catch(IOException) { }

																switch(ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadDie) {
																case ReaderData.SpeakMessage.Wave:
																	break;
																case ReaderData.SpeakMessage.BpuyomiChan:
																	o.OnNext(ReaderConfigs.ConfigLoader.Config.MessageImageSave);
																	break;
																}
															}
														});
												}
											}
										}
									}
									if(!isOld && x.RawResponse.DieDateTime.HasValue && x.RawResponse.NowDateTime.HasValue) {
										if((x.RawResponse.DieDateTime.Value - x.RawResponse.NowDateTime.Value).TotalMilliseconds < 5 * 60 * 1000) {
											switch(ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadOld) {
											case ReaderData.SpeakMessage.Wave:
												break;
											case ReaderData.SpeakMessage.BpuyomiChan:
												o.OnNext(ReaderConfigs.ConfigLoader.Config.MessageThreadOld);
												break;
											}
											isOld = true;
										}
									}
									if(x.RawResponse.IsDie) {
										switch(ReaderConfigs.ConfigLoader.Config.EnabledSpeakThreadDie) {
										case ReaderData.SpeakMessage.Wave:
											break;
										case ReaderData.SpeakMessage.BpuyomiChan:
											o.OnNext(ReaderConfigs.ConfigLoader.Config.MessageSpeakThreadDie);
											break;
										}
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
			}).ObserveOn(BouyomiChanScheduler)
			.Subscribe(m => {
				var c = new RestClient(ReaderConfigs.ConfigLoader.Config.BouyomiChanEndPoint) {
					UserAgent = Config.ConfigLoader.InitializedSetting.RestUserAgent,
					Timeout = 5000,
				};
				foreach(var line in m.Replace("\r\n", "\n").Split("\n")) {
					foreach(var ng in ReaderConfigs.ConfigLoader.NgConfig.NgWords) {
						if(0 <= line.IndexOf(ng)) {
							goto skip;
						}
					}
					foreach(var ng in ReaderConfigs.ConfigLoader.NgConfig.NgRegex) {
						if(Regex.IsMatch(line, ng)) {
							goto skip;
						}
					}
					SpeakCore(c, line);

				skip:;
				}
			});
		}


		public static void Speak(string text) {
			Observable.Return(text)
				.ObserveOn(BouyomiChanScheduler)
				.Subscribe(m => {
					var c = new RestClient(ReaderConfigs.ConfigLoader.Config.BouyomiChanEndPoint) {
						UserAgent = Config.ConfigLoader.InitializedSetting.RestUserAgent,
						Timeout = 5000,
					};
					foreach(var line in m.Replace("\r\n", "\n").Split("\n")) {
						SpeakCore(c, line);
					}
				});
		}

		private static void SpeakCore(IRestClient client, string line) {
			try {
				var r = new RestRequest("Talk", Method.GET)
					.AddParameter("text", line.Replace('%', '％')
						.Replace('&', '＆')
						.Replace('?', '？'));
				var ret = client.Execute(r);
				if(ret.StatusCode != System.Net.HttpStatusCode.OK) {
					// TODO: エラー処理
				}
			}
			catch(Exception e) when(e is SocketException || e is TimeoutException) {
				// TODO: エラー処理
			}
		}

		private static void PlaySound() {

		}

		private static string Html2Text(string html) {
			return Util.TextUtil.RowComment2Text(html);
		}
	}
}
