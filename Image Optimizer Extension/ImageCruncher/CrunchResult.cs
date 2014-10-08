
namespace ImageCruncher
{
	public class CrunchResult
	{
		public CrunchResult(string fileName, string service)
		{
			this.FileName = fileName;
			this.Service = service;
		}

		public double SizeBefore { get; internal set; }
		public double SizeAfter { get; internal set; }
		public double PercentSaved { get; internal set; }
		public string FileName { get; internal set; }
		public string ErrorMessage { get; internal set; }
		public string Service { get; internal set; }

		public bool HasError  
		{
			get { return !string.IsNullOrEmpty(ErrorMessage); }
		}
	}
}
