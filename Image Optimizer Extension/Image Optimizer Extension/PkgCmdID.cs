// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace MadsKristensen.Image_Optimizer_Extension
{
	static class PkgCmdIDList
	{
		public const uint cmdImageOptimizer = 0x100;
		public const uint cmdImageOptimizerQueryStatus = 0x101;
		public const uint cmdidQueryStatus = 0x102;
		public const uint embedQueryStatus = 0x103;
	};
}