using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;


namespace Wolfram.Grasshopper {

public class ExprParam : GH_Param<ExprType> {

    // We need to supply a constructor without arguments that calls the base class constructor.
    public ExprParam() : base("E", "E", "Represents an arbitrary Wolfram Engine expression", "Params", "Wolfram", GH_ParamAccess.item) { }

    public override System.Guid ComponentGuid {
        get { return new Guid("{c88e2237-48a8-4372-8b56-d65254239171}"); }
    }

}

}
