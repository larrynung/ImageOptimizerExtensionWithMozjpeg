// Guids.cs
// MUST match guids.h
using System;

namespace MadsKristensen.Image_Optimizer_Extension
{
    static class GuidList
    {
        public const string guidImage_Optimizer_ExtensionPkgString = "bf95754f-93d3-42ff-bfe3-e05d23188b08";
        public const string guidImage_Optimizer_ExtensionCmdSetString = "bb2f3f4a-e8c9-41bb-94df-d9eaa52356ea";
				public const string guidDynamicMenuDevelopmentCmdSetPart2String = "9d9046da-94f8-4fd0-8a00-92bf4f6defa8";
				public const string guidDynamicMenuDevelopmentCmdSetPart3String = "9d9046da-94f8-4fd0-8a00-92bf4f6defa9";
				public const string guidDynamicMenuDevelopmentCmdSetPart5String = "9d9046da-94f8-4fd0-8a00-92bf4f6defa0";

        public static readonly Guid guidImage_Optimizer_ExtensionCmdSet = new Guid(guidImage_Optimizer_ExtensionCmdSetString);
				public static readonly Guid guidDynamicMenuDevelopmentCmdSetPart2 = new Guid(guidDynamicMenuDevelopmentCmdSetPart2String);
				public static readonly Guid guidDynamicMenuDevelopmentCmdSetPart3 = new Guid(guidDynamicMenuDevelopmentCmdSetPart3String);
				public static readonly Guid guidDynamicMenuDevelopmentCmdSetPart5 = new Guid(guidDynamicMenuDevelopmentCmdSetPart5String);
    };
}