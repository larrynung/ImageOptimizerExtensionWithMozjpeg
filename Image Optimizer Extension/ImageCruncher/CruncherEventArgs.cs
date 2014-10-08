using System;

namespace ImageCruncher
{
	public class CruncherEventArgs : EventArgs
	{
		public CruncherEventArgs(CrunchResult result)
		{
			this.Result = result;
		}

		public CrunchResult Result { get; private set; }
	}
}
