using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;

namespace Wolfram.Rhino {
    
public class KernelLinkProvider {

    private static IKernelLink kl = null;
    private static IKernelLink readerLink = null;
    private static volatile string[] linkArgs = null;
    //private static volatile string[] linkArgs = new string[] { "-linkmode", "launch", "-linkname", "java -classpath \"c:/users/tgayley/documents/mathjava/jlink/src/java\" -Dcom.wolfram.jlink.libdir=\"c:/program files/wolfram research/mathematica/10.0/systemfiles/links/jlink\" com.wolfram.jlink.util.LinkSnooper -kernelmode launch -kernelname \"c:/program files/wolfram research/mathematica/10.0/mathkernel.exe\"" };
    private static object linkLock = new object();

    public static IKernelLink Link
    {
        get
        {
            WolframScriptingPlugIn.DebugPrint("entering Link getter");
            lock (linkLock)
            {
                if (kl == null)
                {
                    if (LinkArguments == null)
                        kl = MathLinkFactory.CreateKernelLink();
                    else
                        kl = MathLinkFactory.CreateKernelLink(LinkArguments);
                    kl.WaitAndDiscardAnswer();
                    kl.EnableObjectReferences();
                    WolframScriptingPlugIn.DebugPrint("back from M launch");
                    kl.Evaluate("PacletDirectoryAdd[\"c:/users/tgayley/documents/workspace/grasshopperlink\"]");
                    kl.WaitAndDiscardAnswer();
                    kl.Evaluate("Needs[\"GrasshopperLink`\"]");
                    kl.WaitAndDiscardAnswer();
                    WolframScriptingPlugIn.DebugPrint("about to call setupLinksFromRhino");
                    kl.Evaluate("GrasshopperLink`Private`setupLinksFromRhino[]");
                    kl.WaitAndDiscardAnswer();
                    WolframScriptingPlugIn.DebugPrint("back from setupLinksFromRhino");
                    // Start the Reader link, so named because it mimics the Reader thread in standard .NET/Link. This is the link
                    // that handles calls like NETNew from Wolfram, enabling "scripting" of Unity from M.
                    kl.Evaluate("GrasshopperLink`Private`startReader[]");
                    // Not the "result"; sent manually so we can connect the link from both sides before the startReader[] function returns.
                    string readerLinkName = kl.GetString();
                    readerLink = MathLinkFactory.CreateKernelLink("-linkmode connect -linkname " + readerLinkName);
                    WolframScriptingPlugIn.DebugPrint("about to connect readerLink");
                    readerLink.Connect();
                    WolframScriptingPlugIn.DebugPrint("back from connecting readerLink");
                    kl.WaitAndDiscardAnswer();

                    //(* This causes the reader and main links to share the same CallPacketHandler/ObjectHandler. *)
                    ((KernelLinkImpl)readerLink).copyStateFrom((KernelLinkImpl)kl);

                    WolframScriptingPlugIn.DebugPrint("back from startReader[]");
                }
                return kl;
            }
        }
    }

    public static IKernelLink ReaderLink
    {
        get
        {
            IKernelLink mainLink = Link;
            return readerLink;
        }
    }

    // Returns null if default.
    public static string[] LinkArguments {
        get { return linkArgs; }
        set { linkArgs = value; }
    }

}

}
