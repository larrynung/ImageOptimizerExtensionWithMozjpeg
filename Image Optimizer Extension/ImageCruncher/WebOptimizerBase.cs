using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ImageCruncher
{
	public abstract class WebOptimizerBase : IOptimizer
	{
		private readonly Encoding encoding = Encoding.UTF8;
		private const string META_HEADER = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n";
		private const string FILE_HEADER = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n";
		private FileInfo file;

		/// <summary>
		/// Gets the endpoint URL of the web service.
		/// </summary>
		protected abstract Uri Endpoint { get; }

		/// <summary>
		/// Gets the service name of the cruncher.
		/// </summary>
		/// <value>The cruncher URL.</value>
		public abstract string Service { get; }

		/// <summary>
		/// Optimizes the specified file.
		/// </summary>
		/// <param name="fileName">The absolute path to the file.</param>
		public void Optimize(string fileName)
		{
			file = new FileInfo(fileName);

			FileStream fs = file.OpenRead();
			byte[] data = new byte[fs.Length];
			fs.Read(data, 0, data.Length);
			fs.Close();

			var parameter = new FileParameter() { File = data, FileName = file.Name };
			var postData = PopulatePostData(parameter);
			BeginPost(postData);
		}

		protected abstract Dictionary<string, object> PopulatePostData(FileParameter parameter);
		protected abstract void ReadResponse(string response, string fileName);

		protected void BeginPost(Dictionary<string, object> postParameters)
		{
			string boundary = "-----------------------------28947758029299";
			string contentType = "multipart/form-data; boundary=" + boundary;
			byte[] formData = GetMultipartFormData(postParameters, boundary);

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Endpoint);
			request.Method = "POST";
			request.ContentType = contentType;
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1)";
			request.ContentLength = formData.Length;

			try
			{
				using (Stream requestStream = request.GetRequestStream())
				{
					requestStream.Write(formData, 0, formData.Length);
				}

				request.BeginGetResponse(this.EndPost, new ArrayList() { request, file.FullName });
			}
			catch (WebException ex)
			{
				OnCompleted(new CrunchResult(file.FullName, this.Service) { ErrorMessage = ex.Message });
			}
		}

		protected void EndPost(IAsyncResult result)
		{
			ArrayList data = (ArrayList)result.AsyncState;
			HttpWebRequest request = (HttpWebRequest)data[0];
			HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
			string fileName = (string)data[1];

			try
			{
				string json;
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					json = reader.ReadToEnd();
				}

				this.ReadResponse(json, fileName);
			}
			catch (Exception ex)
			{
				OnCompleted(new CrunchResult(fileName, this.Service) { ErrorMessage = ex.Message });
			}
		}

		private byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
		{
			using (Stream formDataStream = new MemoryStream())
			{
				foreach (var param in postParameters)
				{
					var fileToUpload = param.Value as FileParameter;
					if (fileToUpload != null)
					{
						string header = string.Format(FILE_HEADER, boundary, param.Key, fileToUpload.FileName, fileToUpload.ContentType);
						formDataStream.Write(encoding.GetBytes(header), 0, header.Length);
						formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
					}
					else
					{
						string postData = string.Format(META_HEADER, boundary, param.Key, param.Value);
						formDataStream.Write(encoding.GetBytes(postData), 0, postData.Length);
					}
				}

				// Add the end of the request
				string footer = "\r\n--" + boundary + "--\r\n";

				formDataStream.Write(encoding.GetBytes(footer), 0, footer.Length);
				formDataStream.Position = 0;
				byte[] formData = new byte[formDataStream.Length];
				formDataStream.Read(formData, 0, formData.Length);

				return formData;
			}
		}

		/// <summary>
		/// Occurs when the image is optimized.
		/// </summary>
		public event EventHandler<CruncherEventArgs> Completed;
		protected void OnCompleted(CrunchResult result)
		{
			if (Completed != null)
			{
				Completed(this, new CruncherEventArgs(result));
			}
		}

        /// <summary>
        /// Occurs when the image is optimized.
        /// </summary>
        public event EventHandler<CruncherEventArgs> BeforeWritingFile;
        protected void OnBeforeWritingFile(CrunchResult result)
        {
            if (BeforeWritingFile != null)
            {
                BeforeWritingFile(this, new CruncherEventArgs(result));
            }
        }
	}
}
