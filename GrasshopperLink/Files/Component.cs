using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Wolfram.NETLink;

namespace Wolfram.Grasshopper
{
    public class `Name` : GH_Component
    {
        public `Name`()
            : base("`Name`", "`Nickname`",
                "`Description`",
                "`Category`", "`Subcategory`")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            `RegisterInputParams`
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            `RegisterOutputParams`
            pManager.AddParameter(new LinkParam(), "link", "WL", "The link to the Wolfram Engine", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            `SolveInstance`
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager("gh", typeof(`Name`).Assembly);
                byte[] array = (byte[]) rm.GetObject("Icon");
                return new System.Drawing.Bitmap(new System.IO.MemoryStream(array));
                //return (System.Drawing.Bitmap) obj;
                //return obj ==null ? null : new System.Drawing.Bitmap(@"C:\Users\tgayley\Documents\workspace\GrasshopperLink\Projects\WolframGrasshopperComponents\Resources\spikeyIcon.png");
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{`GUID`}"); }
        }
    }
}
