using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;

namespace Wolfram.Grasshopper {
    
public class KernelLinkProvider {

    private static IKernelLink kl = null;
    //private static volatile string[] linkArgs = null;
    private static volatile string[] linkArgs = new string[] { "-linkmode", "launch", "-linkname", "java -classpath \"c:/users/tgayley/documents/mathjava/jlink/src/java\" -Dcom.wolfram.jlink.libdir=\"c:/program files/wolfram research/mathematica/10.0/systemfiles/links/jlink\" com.wolfram.jlink.util.LinkSnooper -kernelmode launch -kernelname \"c:/program files/wolfram research/mathematica/10.0/mathkernel.exe\"" };
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
