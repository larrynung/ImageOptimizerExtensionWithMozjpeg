
namespace ImageCruncher
{
	internal class PunyPngResponse
	{
		public string original_url { get; set; }
		public double original_size { get; set; }
		public string optimized_url { get; set; }
		public double optimized_size { get; set; }
		public double savings_percent { get; set; }
	}
}