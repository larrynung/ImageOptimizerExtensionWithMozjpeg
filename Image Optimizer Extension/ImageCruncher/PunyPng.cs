using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace ImageCruncher
{
	public class PunyPng : WebOptimizerBase
	{
		protected override Uri Endpoint
		{
			get { return new Uri("http://www.punypng.com/api/optimize", UriKind.Absolute); }
		}

		public override string Service
		{
			get { return "http://punypng.com"; }
		}

		protected override Dictionary<string, object> PopulatePostData(FileParameter parameter)
		{
			return new Dictionary<string, object>(){
				{"filename", parameter.FileName},
				{"key", "f07f683326c2e2fc12e53c01afa75e55f944ec1c"},
				{"img", parameter}
			};
		}

		protected override void ReadResponse(string response, string fileName)
		{
			if (response.Contains("\"savings_percent\":0}"))
			{
				OnCompleted(new CrunchResult(fileName, this.Service));
				return;
			}

			JavaScriptSerializer jSerialize = new JavaScriptSerializer();
			PunyPngResponse sir = jSerialize.Deserialize<PunyPngResponse>(response);

			Uri url;
			if (!Uri.TryCreate(sir.optimized_url, UriKind.Absolute, out url))
			{
				OnCompleted(new CrunchResult(fileName, this.Service));
				return;
			}

			var result = new CrunchResult(fileName, this.Service) {
				SizeBefore = sir.original_size,
				SizeAfter = sir.optimized_size,
				PercentSaved = sir.savings_percent
			};

            if (result.SizeAfter != result.SizeBefore)
            {
                OnBeforeWritingFile(result);
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, fileName);
                }
            }

            OnCompleted(result);
		}
	}
}