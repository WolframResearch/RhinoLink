using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Wolfram.Grasshopper
{
    public class `Name`Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "`Name`";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return `Bitmap`;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "`Description`";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("`GUID`");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "`AuthorName`";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "`AuthorContact`";
            }
        }
    }
}
