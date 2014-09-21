using System.Threading;
using System.Windows.Threading;

using RhinoNamespace = Rhino;

using Wolfram.NETLink;


namespace Wolfram.Rhino
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class WolframScriptingPlugIn : RhinoNamespace.PlugIns.PlugIn
    {

        private volatile bool keepRunning = false;
        Dispatcher dispatcher;

        public WolframScriptingPlugIn()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the WolframScriptingPlugIn plug-in.</summary>
        public static WolframScriptingPlugIn Instance
        {
            get;
            private set;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and mantain plug-in wide options in a document.

        public override object GetPlugInObject()
        {
            return KernelLinkProvider.Link;
        }



        public void StartReaderThread()
        {
            keepRunning = true;
            Thread readerThread = new System.Threading.Thread(new System.Threading.ThreadStart(RunReader));
            dispatcher = Dispatcher.CurrentDispatcher;
            readerThread.Start();
        }

        public void StopReaderThread()
        {
            keepRunning = false;
        }


        public delegate void ReaderMethodCaller(PacketType pkt);
        
        public void RunReader()
        {

            IKernelLink readerLink = KernelLinkProvider.ReaderLink;
            while (keepRunning) {
                // we only wait if there is already something on the link.  
                if (readerLink.Ready)
                {
                    //stopWatch.Reset();
                    //stopWatch.Start();

                    //while (stopWatch.ElapsedMilliseconds <= loopMillis)
                    //{
                    //    if (readerLink.Ready)
                    //    {
                    try
                    {
                        DebugPrint("Packet arriving on Reader link");
                        PacketType pkt = readerLink.NextPacket();
                        var readerDelegate = new ReaderMethodCaller(readerLink.HandlePacket);
                        DebugPrint("calling BeginInvoke");
                        DispatcherOperation op = dispatcher.BeginInvoke(readerDelegate, new object[] { pkt });
                        DebugPrint("calling Wait()");
                        op.Wait(); // also has a millis arg
                        DebugPrint("back from Wait()");
                        readerLink.NewPacket();
                        DebugPrint("Finished handling packet on Reader link");
                        // if we got a packet, we reset the watch
                        //stopWatch.Reset();
                        //stopWatch.Start();
                    }
                    catch (MathLinkException e)
                    {
                        // 11 is "other side closed link"; not sure why this succeeds clearError, but it does.
                        if (e.ErrCode == 11 || !readerLink.ClearError())
                        {
                            DebugPrint("Exception on Wolfram Reader link: " + e);
                            KernelLinkProvider.CloseReaderLink();
                        }
                        else
                        {
                            readerLink.NewPacket();
                        }
                    }
                    //         }
                    //         else if (sleepMillis >= 0)
                    //         {
                    //             System.Threading.Thread.Sleep(sleepMillis);
                    //          }
                    //      }
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }

        }


        public static void DebugPrint(string s)
        {
            RhinoNamespace.RhinoApp.WriteLine(s);
        }

    }
}