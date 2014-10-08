using System;
using System.Diagnostics;
using System.IO;
using ImageCruncher.Properties;

namespace ImageCruncher
{
    /// <summary>
    /// 
    /// </summary>
    class Mozjpeg : IOptimizer
    {
		#region Fields 
        private String _jpegtranFilePath;
        private String _mozjpegFolderPath;
		#endregion Fields 

		#region Event 
        public event EventHandler<CruncherEventArgs> BeforeWritingFile;

        public event EventHandler<CruncherEventArgs> Completed;
		#endregion Event 

		#region Properties 
        /// <summary>
        /// Gets the m_ jpegtran file path.
        /// </summary>
        /// <value>
        /// The m_ jpegtran file path.
        /// </value>
        protected String m_JpegtranFilePath
        {
            get
            {
                return _jpegtranFilePath ?? (_jpegtranFilePath = Path.Combine(m_MozjpegFolderPath, "jpegtran.exe"));
            }
        }

        /// <summary>
        /// Gets the m_ mozjpeg folder path.
        /// </summary>
        /// <value>
        /// The m_ mozjpeg folder path.
        /// </value>
        protected String m_MozjpegFolderPath
        {
            get
            {
                return _mozjpegFolderPath ?? (_mozjpegFolderPath = Path.Combine(Path.GetTempPath(), "Mozjpeg"));
            }
        }
        /// <summary>
        /// Gets the name of the service used to optimize the image.
        /// </summary>
        /// <value>
        /// The service.
        /// </value>
        public String Service 
        {
            get
            {
                return "Mozjpeg";
            }
        }
		#endregion Properties 

		#region Methods 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        private static void ShellCopyTo(String from, String to)
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            var shellObject = System.Activator.CreateInstance(shellType);
            var objSrcFile = shellType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shellObject, new Object[] { from });
            var objDestFolder = shellType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, shellObject, new Object[] { to });
            var FolderType = Type.GetTypeFromCLSID(new Guid("BBCBDE60-C3FF-11CE-8350-444553540000"));
            var items = FolderType.InvokeMember("Items", System.Reflection.BindingFlags.InvokeMethod, null, objSrcFile, null);
            FolderType.InvokeMember("CopyHere", System.Reflection.BindingFlags.InvokeMethod, null, objDestFolder, new Object[] { items, 20 });
        }

        /// <summary>
        /// Tries to initialize mozjpeg.
        /// </summary>
        private void TryToInitMozjpeg()
        {
            var mozjpegFolder = m_MozjpegFolderPath;

            if (!Directory.Exists(mozjpegFolder))
                Directory.CreateDirectory(mozjpegFolder);

            if (File.Exists(m_JpegtranFilePath))
                return;

            var tempFile = Path.Combine(Path.GetTempPath(), String.Format("{0}.zip", Guid.NewGuid().ToString()));
            File.WriteAllBytes(tempFile, Resources.libmozjpeg_2_1_x86);

            DeCompress(tempFile, mozjpegFolder);

            File.Delete(tempFile);
        }

        /// <summary>
        /// Called when [before writing file].
        /// </summary>
        /// <param name="result">The result.</param>
        protected void OnBeforeWritingFile(CrunchResult result)
        {
            if (BeforeWritingFile == null)
                return;
            BeforeWritingFile(this, new CruncherEventArgs(result));
        }

        /// <summary>
        /// Called when [completed].
        /// </summary>
        /// <param name="result">The result.</param>
        protected void OnCompleted(CrunchResult result)
        {
            if (Completed == null)
                return;
            Completed(this, new CruncherEventArgs(result));
        }

        /// <summary>
        /// Decompress.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <param name="destinationFolderPath">The destination folder path.</param>
        public static void DeCompress(String zipFile, String destinationFolderPath)
        {
            if (!File.Exists(zipFile))
                throw new FileNotFoundException();

            if (!Directory.Exists(destinationFolderPath))
                Directory.CreateDirectory(destinationFolderPath);

            ShellCopyTo(zipFile, destinationFolderPath);
        }

        /// <summary>
        /// Optimizes the specified file.
        /// </summary>
        /// <param name="fileName">The absolute path to the file.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Optimize(String fileName)
        {
            TryToInitMozjpeg();

            var originalFile = fileName;
            var tempFile = Path.Combine(Path.GetTempPath(), String.Format("{0}.jpg", Guid.NewGuid().ToString()));

            CompressJpeg(originalFile, tempFile);

            var originalFileSize = (new FileInfo(fileName)).Length;
            var tempFileSize = (new FileInfo(tempFile)).Length;
            var result = new CrunchResult(fileName, this.Service)
            {
                SizeBefore = originalFileSize,
                SizeAfter = tempFileSize,
                PercentSaved = Math.Round(100 - ((Double)tempFileSize / originalFileSize) * 100, 2)
            };

            File.Delete(tempFile);

            OnBeforeWritingFile(result);

            CompressJpeg(originalFile, originalFile);

            OnCompleted(result);
        }

        private void CompressJpeg(string originalFile, string newFile)
        {
            var psi = new ProcessStartInfo(m_JpegtranFilePath,
                String.Format(@"-copy none -outfile ""{0}"" ""{1}""", newFile, originalFile))
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var p = Process.Start(psi);

            if (p == null)
                return;

            p.WaitForExit();
        }

        #endregion Methods 
    }
}
