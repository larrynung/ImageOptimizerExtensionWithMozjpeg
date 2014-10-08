using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCruncher
{
	public class Cruncher
	{
		private static readonly List<string> extensions = new List<string>() { ".PNG", ".JPG", ".JPEG", ".GIF" };
		public int Count { get; set; }
		public int Optimized { get; set; }

		public static bool IsSupported(string fileName)
		{
			return extensions.Contains(Path.GetExtension(fileName.ToUpperInvariant()));
		}

		public void CrunchImages(params string[] foldersAndFiles)
		{
			List<string> images = new List<string>();
			extensions.ForEach(ext =>
			{
				Array.ForEach(foldersAndFiles, f => images.AddRange(GetImages(f, ext)));
			});

			this.Count = images.Count;
			this.Optimized = 0;

			ThreadPool.QueueUserWorkItem((state) =>
			{
				Parallel.ForEach(images, f => { this.Compress(f); });
				this.OnCompleted();
			});
		}

		private static string[] GetImages(string folderOrFile, string extension)
		{
			if (File.Exists(folderOrFile) && folderOrFile.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
			{
				return new[] { folderOrFile };
			}
			else if (Directory.Exists(folderOrFile))
			{
				var files = Directory.GetFiles(folderOrFile, "*" + extension, SearchOption.AllDirectories);
				return Array.FindAll(files, f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));
			}

			return new string[0];
		}

		private void Compress(string path)
		{
			IOptimizer optimizer;
			string extension = Path.GetExtension(path).ToUpperInvariant();
			if (extension == ".GIF")
			{
				optimizer = new PunyPng();
			}
            else if (extension == ".JPG" || extension == ".JPEG")
            {
                optimizer = new Mozjpeg();
            }
            else
            {
                optimizer = new SmushIt();
            }

		    optimizer.Completed += delegate(object s, CruncherEventArgs e) { OnProgress(e); };
            optimizer.BeforeWritingFile += delegate(object s, CruncherEventArgs e) { OnBeforeWritingFile(e.Result); };
			optimizer.Optimize(path);
		}

		public event EventHandler<CruncherEventArgs> Progress;
		protected void OnProgress(CruncherEventArgs eventArgs)
		{
			Optimized++;
			if (Progress != null)
			{
				Progress(this, eventArgs);
			}
		}

		public event EventHandler<EventArgs> Completed;
		protected void OnCompleted()
		{
			if (Completed != null)
			{
				Completed(this, EventArgs.Empty);
			}
		}

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