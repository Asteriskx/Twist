﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Twist.API;
using Newtonsoft.Json.Linq;

namespace Twist
{
	/// <summary>
	/// Twitter へアクセスするためのラッパークラス
	/// </summary>
	public class Twitter
	{

		#region Field

		/// <summary>
		/// Access Token を取得する際に必要なエンドポイントURL
		/// </summary>
		public static readonly string AccessTokenUrl = "https://api.twitter.com/oauth/access_token";

		/// <summary>
		/// 認証を実施する際に必要なエンドポイントURL
		/// </summary>
		public static readonly string AuthorizeUrl = "https://api.twitter.com/oauth/authorize";

		/// <summary>
		/// 画像アップロードを実施する際に必要なエンドポイントURL
		/// </summary>
		public static readonly string ChunkUpload = "https://upload.twitter.com/1.1/media/upload.json";

		/// <summary>
		/// 認証画面を表示する際に必要なエンドポイントURL
		/// </summary>
		public static readonly string RequestTokenUrl = "https://api.twitter.com/oauth/request_token";

		/// <summary>
		/// 投稿を実施する際に必要なエンドポイントURL
		/// </summary>
		public static readonly string Update = "https://api.twitter.com/1.1/statuses/update.json";

		#endregion field

		#region Properties

		/// <summary>
		/// Twitter アクセスラッパーAPI の管理を行います。
		/// </summary>
		private Core _Core { get; set; }

		#endregion

		#region Constractor 

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="ck"> Consumer Key </param>
		/// <param name="cs"> Consumer Secret </param>
		/// <param name="client"> HttpClient </param>
		public Twitter(string ck, string cs, HttpClient client) => this._Core = new Core(ck, cs, client);

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="ck"> Consumer Key </param>
		/// <param name="cs"> Consumer Secret </param>
		/// <param name="at"> Access Token </param>
		/// <param name="ats"> Access Token Secret </param>
		/// <param name="id"> User ID </param>
		/// <param name="name"> Screen Name </param>
		/// <param name="client"> HttpClient </param>
		public Twitter(string ck, string cs, string at, string ats, string id, string name, HttpClient client)
			=> this._Core = new Core(ck, cs, at, ats, id, name, client);

		#endregion Constractor

		#region Inner class

		private class MediaData
		{
			[JsonProperty("media_id_string")]
			public string MediaIdString { get; set; }
		}

		#endregion Inner class

		#region TwitterAPI Access Wrapper Methods

		/// <summary>
		/// 認証用のURLを返却します。
		/// </summary>
		/// <returns> 認証用のURL </returns>
		public async Task<string> GenerateAuthorizeAsync()
		{
			Debug.WriteLine("------------ 認証シーケンス開始 -----------------");

			await this._Core.GetRequestTokenAsync(RequestTokenUrl);
			Uri url = this._Core.GetAuthorizeUrl(AuthorizeUrl);

			Debug.WriteLine("------------ 認証シーケンス完了 ----------------- >> " + url.ToString());
			return url.ToString();
		}

		/// <summary>
		/// Twitter へ リクエストを投げるための薄いラッパーメソッド
		/// </summary>
		/// <param name="url"> リクエストURL(エンドポイント) </param>
		/// <param name="type"> リクエストタイプ(GET/POST) </param>
		/// <param name="query"> テキストデータ </param>
		/// <param name="stream"> 画像データ(画像がない場合は、null扱い) </param>
		/// <returns></returns>
		private Task<string> _Request(string url, HttpMethod type, IDictionary<string, string> query, Stream stream = null)
		{
			if (stream == null)
				return this._Core.RequestAsync(this._Core.ConsumerKey, this._Core.ConsumerSecret, this._Core.AccessToken, this._Core.AccessTokenSecret, url, type, query);
			else
				return this._Core.RequestAsync(this._Core.ConsumerKey, this._Core.ConsumerSecret, this._Core.AccessToken, this._Core.AccessTokenSecret, url, type, query, stream);
		}

		/// <summary>
		/// Twitter へツイートを非同期にて行います。
		/// </summary>
		/// <param name="text">ツイート内容</param>
		/// <returns></returns>
		public async Task UpdateWithTextAsync(string text)
		{
			var query = new Dictionary<string, string> { { "status", text } };
			await this._Request(Update, HttpMethod.Post, query);
		}


		/// <summary>
		/// Twitter へ画像付きツイートを非同期にて行います。
		/// </summary>
		/// <param name="text">ツイート内容</param>
		/// <param name="path">画像ファイルパス</param>
		/// <returns></returns>
		public async Task UpdateWithMediaAsync(string text, string path)
		{
			using (var image = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				var id = await this._Request(ChunkUpload, HttpMethod.Post, new Dictionary<string, string>() { }, image);
				var deserialize = JsonConvert.DeserializeObject<MediaData>(id).MediaIdString;

				var query = new Dictionary<string, string> { { "status", text }, { "media_ids", deserialize } };
				await this._Request(Update, HttpMethod.Post, query);
			}
		}

		/// <summary>
		/// Twitter へ画像付きツイートを非同期にて行います。
		/// </summary>
		/// <param name="text">ツイート内容</param>
		/// <param name="stream">画像データ：Stream 形式</param>
		/// <returns></returns>
		public async Task UpdateWithMediaAsync(string text, Stream stream)
		{
			if (stream != null)
			{
				var id = await this._Request(ChunkUpload, HttpMethod.Post, new Dictionary<string, string>() { }, stream);
				var deserialize = JsonConvert.DeserializeObject<MediaData>(id).MediaIdString;

				var query = new Dictionary<string, string> { { "status", text }, { "media_ids", deserialize } };
				await this._Request(Update, HttpMethod.Post, query);
			}
			else
			{
				throw new Exception("stream is Empty........");
			}
		}

		/// <summary>
		/// Access Token の取得を行うためのラッパーメソッド
		/// </summary>
		/// <param name="pin"> 認証時に表示された PIN コード </param>
		/// <returns> 各種認証キー：ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret </returns>
		public async Task GetAccessTokenAsync(string pin) =>
			(this._Core.AccessToken, this._Core.AccessTokenSecret, this._Core.UserId, this._Core.ScreenName) = await this._Core.GetAccessTokenAsync(AccessTokenUrl, pin);

		#endregion TwitterAPI Access Wrapper Methods

	}
}
