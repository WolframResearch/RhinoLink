using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Wolfram.Grasshopper {


public class LinkType : GH_Goo<IKernelLink> {

    public LinkType() {
        Value = null;
    }

    public LinkType(IKernelLink link) {
        Value = link;
    }

    // Copy Constructor
    public LinkType(LinkType source) {
        Value = source.Value;
    }

    public override IGH_Goo Duplicate() {
        return new LinkType(this);
    }

    // I think this determines whether an input will fire. I don't want unassigned links to fire, right?
    public override bool IsValid {
        get { return Value != null; }
    }

    public override string TypeName {
        get { return "Wolfram Link"; }
    }

    public override string TypeDescription {
        get { return "The link to the Wolfram Engine"; }
    }

    public override string ToString() {
        return Value.ToString();
    }

}


}
