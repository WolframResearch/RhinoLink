using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

// This lives in the WolframGrasshopperSupport assembly instead of the WolframGrasshopperComponents assembly because I 
// don't think we want it to show up in the GHrasshopper GUI.

namespace Wolfram.Grasshopper {

public class LinkParam : GH_Param<LinkType> {

    // We need to supply a constructor without arguments that calls the base class constructor.
    public LinkParam() : base("L", "L", "The link to the Wolfram Engine", "Params", "Wolfram", GH_ParamAccess.item) { }

    public override System.Guid ComponentGuid {
        get { return new Guid("{b8c0e44f-f815-40f7-9e08-dae4da0813a2}"); }
    }

}

}
