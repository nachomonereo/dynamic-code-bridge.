using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace DynamicCodeBridge
{
    public class iaacInfo : GH_AssemblyInfo
    {
        public override string Name => "Dynamic Code Bridge";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "A universal live-coding bridge between VS Code and Grasshopper with auto-debugging and dynamic parameters.";

        public override Guid Id => new Guid("4673A06B-0635-4309-9D6C-B7C41829B639");

        //Return a string identifying you or your company.
        public override string AuthorName => "Antigravity & User";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}