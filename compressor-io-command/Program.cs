using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace compressoriocommand
{
	class MainClass
	{
		public static string cookies = string.Empty;

		public static string PostMultipleFiles(string url, string[] files)
		{
			string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
			httpWebRequest.Method = "POST";
			httpWebRequest.KeepAlive = true;
			httpWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
			Stream memStream = new System.IO.MemoryStream();
			byte[] boundarybytes =System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary     +"\r\n");
			string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition:  form-data; name=\"{0}\";\r\n\r\n{1}";
			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";
			memStream.Write(boundarybytes, 0, boundarybytes.Length);
			for (int i = 0; i < files.Length; i++)
			{
				string header = string.Format(headerTemplate, "files[]", files[i]);
				//string header = string.Format(headerTemplate, "uplTheFile", files[i]);
				byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
				memStream.Write(headerbytes, 0, headerbytes.Length);
				FileStream fileStream = new FileStream(files[i], FileMode.Open,
					FileAccess.Read);
				byte[] buffer = new byte[1024];
				int bytesRead = 0;
				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					memStream.Write(buffer, 0, bytesRead);
				}
				memStream.Write(boundarybytes, 0, boundarybytes.Length);
				fileStream.Close();
			}
			httpWebRequest.ContentLength = memStream.Length;
			Stream requestStream = httpWebRequest.GetRequestStream();
			memStream.Position = 0;
			byte[] tempBuffer = new byte[memStream.Length];
			memStream.Read(tempBuffer, 0, tempBuffer.Length);
			memStream.Close();
			requestStream.Write(tempBuffer, 0, tempBuffer.Length);
			requestStream.Close();
			try
			{
				WebResponse webResponse = httpWebRequest.GetResponse();
				cookies = ((HttpWebResponse)webResponse).Headers.Get("Set-Cookie");
				Stream stream = webResponse.GetResponseStream();
				StreamReader reader = new StreamReader(stream);
				string var = reader.ReadToEnd();
				return var;
			}
			catch (Exception ex)
			{
				Console.WriteLine (ex.Message + " - " + ex.StackTrace);
			}
			httpWebRequest = null;
			return "";
		}
		public static bool Validator (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		public static void Compress (FileInfo file) 
		{
			try
			{
				ServicePointManager.ServerCertificateValidationCallback = Validator;

				var files = new String[]{ file.FullName };

				var response = PostMultipleFiles("https://compressor.io/server/Lossy.php", files);

				JObject o = JsonConvert.DeserializeObject<JObject>(response);
				JToken rFiles = o.GetValue("files").ToObject<JToken>();

				foreach (var item in rFiles) {
					var url = item.Value<string>("url");

					HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
					httpWebRequest.CookieContainer = new CookieContainer();
					httpWebRequest.Headers.Add("Set-Cookie",cookies);
					var partes = cookies.Split(';');
					foreach (var parte in partes) {
						var pedazos = parte.Split('=');
						httpWebRequest.CookieContainer.Add(new Cookie(pedazos[0], pedazos[1]) { Domain = "compressor.io"});
						break;
					}
					HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
					Stream stream = httpWebReponse.GetResponseStream();
					var img = Image.FromStream(stream);
					img.Save(file.FullName);

				}
			} catch (Exception ex) {
				Console.WriteLine (ex.Message + " - " + ex.StackTrace);
			}
		}

		public static void Main (string[] args)
		{
			try
			{
				var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
				DirectoryInfo dir = new DirectoryInfo(path);
				Console.WriteLine("File Name                       Size        Creation Date and Time");
				Console.WriteLine("=================================================================");
				foreach (FileInfo flInfo in dir.GetFiles())
				{
					String name = flInfo.Name;
					long size = flInfo.Length;
					string ext = flInfo.Extension;
					DateTime creationTime = flInfo.CreationTime;
					Console.WriteLine("{0, -30:g} {1,-12:N0} {2} ", name, size, creationTime);
					var mimesTypes = new List<String>(){ ".jpg", ".jpeg", ".gif", ".svg", ".png"};
					foreach (var mimes in mimesTypes) {
						if(flInfo.Extension.ToLower() == mimes) {
							//Este deberia mandar
							Compress(flInfo);
						}
					}

				}
				/*
				ServicePointManager.ServerCertificateValidationCallback = Validator;

				var files = new String[]{ "Develop/compressor-io-command/compressor-io-command/bin/Debug/a.jpeg" };

				var response = PostMultipleFiles("https://compressor.io/server/Lossy.php", files);

				JObject o = JsonConvert.DeserializeObject<JObject>(response);
				JToken rFiles = o.GetValue("files").ToObject<JToken>();

				foreach (var item in rFiles) {
					var url = item.Value<string>("url");

					HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
					httpWebRequest.CookieContainer = new CookieContainer();
					foreach (var galleta in galletitas) {

						httpWebRequest.CookieContainer.Add((Cookie)galleta);
					}
					httpWebRequest.Headers.Add("Set-Cookie",cookies);
					var partes = cookies.Split(';');
					foreach (var parte in partes) {
						var pedazos = parte.Split('=');
						httpWebRequest.CookieContainer.Add(new Cookie(pedazos[0], pedazos[1]) { Domain = "compressor.io"});
						break;
					}
					HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
					Stream stream = httpWebReponse.GetResponseStream();
					var img = Image.FromStream(stream);
					img.Save("Develop/compressor-io-command/compressor-io-command/bin/Debug/a-compress.jpeg");*/


			} catch (Exception ex) {
				Console.WriteLine (ex.Message + " - " + ex.StackTrace);
			}

		}
	}
}
