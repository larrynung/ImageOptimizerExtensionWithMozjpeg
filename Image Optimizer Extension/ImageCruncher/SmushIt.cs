using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace ImageCruncher
{
    public class SmushIt : WebOptimizerBase
    {
        protected override Uri Endpoint
        {
            get { return new Uri("http://www.smushit.com/ysmush.it/ws.php", UriKind.Absolute); }
        }

        public override string Service
        {
            get { return "http://smushit.com"; }
        }

        protected override Dictionary<string, object> PopulatePostData(FileParameter parameter)
        {
            return new Dictionary<string, object>(){
				{"filename", parameter.FileName},
				{"files", parameter}
			};
        }

        protected override void ReadResponse(string response, string fileName)
        {
            if (response.Contains("No saving"))
            {
                OnCompleted(new CrunchResult(fileName, this.Service));
                return;
            }

            JavaScriptSerializer jSerialize = new JavaScriptSerializer();
            SmushItResponse sir = jSerialize.Deserialize<SmushItResponse>(response);

            Uri url;
            if (!Uri.TryCreate(sir.dest, UriKind.Absolute, out url))
            {
                OnCompleted(new CrunchResult(fileName, this.Service));
                return;
            }

            var result = new CrunchResult(fileName, this.Service)
            {
                SizeBefore = sir.src_size,
                SizeAfter = sir.dest_size,
                PercentSaved = sir.percent
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