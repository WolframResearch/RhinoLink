using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;

namespace Wolfram.Grasshopper {
    
public class KernelLinkProvider {

    private static IKernelLink kl = null;
    private static volatile string[] linkArgs = null;
    private static object linkLock = new object();

    public static IKernelLink Link {
        get {
            //Console.WriteLine("entering Link getter");
            lock (linkLock) {
                //Console.WriteLine("acquired lock");
                if (kl == null) {
                    if (LinkArguments == null)
                        kl = MathLinkFactory.CreateKernelLink();
                    else
                        kl = MathLinkFactory.CreateKernelLink(LinkArguments);
                    kl.WaitAndDiscardAnswer();
                    kl.EnableObjectReferences();
                }
                //Console.WriteLine("returning");
                return kl;
            }
        }
    }

    // REturns null if default.
    public static string[] LinkArguments {
        get { return linkArgs; }
        set { linkArgs = value; }
    }

}

}
