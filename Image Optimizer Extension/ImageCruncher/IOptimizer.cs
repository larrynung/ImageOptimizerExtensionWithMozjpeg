using System;
namespace ImageCruncher
{
	/// <summary>
	/// An interface for describing an image optimizer.
	/// </summary>
	public interface IOptimizer
	{
		/// <summary>
		/// Gets the name of the service used to optimize the image.
		/// </summary>
		/// <value>The service.</value>
		string Service { get; }

		/// <summary>
		/// Optimizes the specified file.
		/// </summary>
		/// <param name="fileName">The absolute path to the file.</param>
		void Optimize(string fileName);

		/// <summary>
		/// Occurs when the image is optimized.
		/// </summary>
		event EventHandler<CruncherEventArgs> Completed;

        event EventHandler<CruncherEventArgs> BeforeWritingFile;
	}
}
