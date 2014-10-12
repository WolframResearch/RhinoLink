using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wolfram.NETLink;

using RhinoNamespace = Rhino;

namespace Wolfram.Rhino {
    
public class KernelLinkProvider {

    private static IKernelLink mainLink = null;
    private static IKernelLink readerLink = null;
    private static volatile string[] linkArgs = null;
    //private static volatile string[] linkArgs = new string[] { "-linkmode", "launch", "-linkname", "java -classpath \"c:/users/tgayley/documents/mathjava/jlink/src/java\" -Dcom.wolfram.jlink.libdir=\"c:/program files/wolfram research/mathematica/10.0/systemfiles/links/jlink\" com.wolfram.jlink.util.LinkSnooper -kernelmode launch -kernelname \"c:/program files/wolfram research/mathematica/10.0/mathkernel.exe\"" };
    private static object linkLock = new object();
    private static bool appLoadSucceeded = true;
    

    public static IKernelLink Link
    {
        get
        {
            WolframScriptingPlugIn.DebugPrint("entering Link getter");
            lock (linkLock)
            {
                if (mainLink == null)
                {
                    WolframScriptingPlugIn.DebugPrint("creating link");
                    if (LinkArguments == null)
                        mainLink = MathLinkFactory.CreateKernelLink();
                    else
                        mainLink = MathLinkFactory.CreateKernelLink(LinkArguments);
                    mainLink.WaitAndDiscardAnswer();
                    mainLink.EnableObjectReferences();
                    StdLink.Link = mainLink;
                    WolframScriptingPlugIn.DebugPrint("back from M launch");
                    mainLink.Evaluate("PacletDirectoryAdd[\"c:/users/tgayley/documents/workspace/grasshopperlink\"]");
                    mainLink.WaitAndDiscardAnswer();
                    mainLink.Evaluate("Needs[\"GrasshopperLink`\"]");
                    mainLink.WaitForAnswer();
                    Expr res = mainLink.GetExpr();
                    if (res.ToString() != "Null") {
                        appLoadSucceeded = false;
                        RhinoNamespace.RhinoApp.WriteLine(
                            "Error: Failed to load the GrasshopperLink application into the Wolfram Engine. Scripting from the Wolfram Engine will not function.");
                    }
                    
                    mainLink.Evaluate("2+2");
                    mainLink.WaitForAnswer();
                    int i = mainLink.GetInteger();
                    WolframScriptingPlugIn.DebugPrint("2+2=" + i);
                }
                return mainLink;
            }
        }
    }

    public static IKernelLink ReaderLink
    {
        get
        {
            lock (linkLock)
            {
                if (readerLink == null && appLoadSucceeded)
                {
                    // Call the link getter to ensure the kernel has launched.
                    IKernelLink link = Link;

                    WolframScriptingPlugIn.DebugPrint("about to call setupLinksFromRhino");
                    link.Evaluate("GrasshopperLink`Private`setupRhinoAttachLink[]");
                    link.WaitAndDiscardAnswer();
                    WolframScriptingPlugIn.DebugPrint("back from setupLinksFromRhino");
                    // Start the Reader link, so named because it mimics the Reader thread in standard .NET/Link. This is the link
                    // that handles calls like NETNew from Wolfram, enabling "scripting" of Unity from M.
                    link.Evaluate("GrasshopperLink`Private`setupReaderLink[]");
                    // Not the "result"; sent manually so we can connect the link from both sides before the startReader[] function returns.
                    string readerLinkName = link.GetString();
                    readerLink = MathLinkFactory.CreateKernelLink("-linkmode connect -linkname " + readerLinkName);
                    WolframScriptingPlugIn.DebugPrint("about to connect readerLink");
                    readerLink.Connect();
                    WolframScriptingPlugIn.DebugPrint("back from connecting readerLink");
                    link.WaitAndDiscardAnswer();

                    // This causes the reader and main links to share the same CallPacketHandler/ObjectHandler. 
                    ((KernelLinkImpl)readerLink).copyStateFrom((KernelLinkImpl)link);

                    WolframScriptingPlugIn.DebugPrint("back from startReader[]");
                }
                return readerLink;
            }
        }
    }

    // Returns null if default.
    public static string[] LinkArguments {
        get { return linkArgs; }
        set { linkArgs = value; }
    }


    public static void CloseLinks()
    {
        CloseReaderLink();
        if (mainLink != null)
        {
            mainLink.Close();
            mainLink = null;
        }
    }

    public static void CloseReaderLink()
    {
        if (readerLink != null)
        {
            // TODO: What needs to be done here? Send something to kernel to have it close the RhinoAttach link?
            if (mainLink != null)
            {
            }
            readerLink.Close();
            readerLink = null;
        }
    }
}

}
