using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Drawing;

using RhinoNamespace = Rhino;

using Wolfram.NETLink;
using Rhino.Geometry;

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
    
        private const bool DEBUG = false;

        private volatile bool isReaderThreadRunning = false;
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
            if (!isReaderThreadRunning) {
                keepRunning = true;
                Thread readerThread = new System.Threading.Thread(new System.Threading.ThreadStart(RunReader));
                dispatcher = Dispatcher.CurrentDispatcher;
                readerThread.Start();
                isReaderThreadRunning = true;
            }
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
                            KernelLinkProvider.CloseLinks();
                            keepRunning = false;
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
                    System.Threading.Thread.Sleep(1);
                }                    
            }
            isReaderThreadRunning = false;
        }


        public static void DebugPrint(string s)
        {
            if (DEBUG)
            {
                RhinoNamespace.RhinoApp.WriteLine(s);

                // poor man's Pause
//              for (double x = 0; x < 100000000.0; x++) { x -= .1; }
            }
        }

//
// Rhino Utilities
//

       public static RhinoNamespace.Geometry.Mesh ToRhinoMesh(double[,] vertices, int[,] faces)
       {
            Mesh mesh = new RhinoNamespace.Geometry.Mesh();

            for (int i = 0; i < vertices.GetLength(0); i++) {
                 mesh.Vertices.Add(vertices[i,0], vertices[i,1], vertices[i,2]);
            }

            if (faces.GetLength(1) == 3)
            {
                for (int i = 0; i < faces.GetLength(0); i++)
                {
                    mesh.Faces.AddFace(faces[i, 0], faces[i, 1], faces[i, 2]);
                }
            }
            else // (faces.GetLength(1) == 4)
            {
                for (int i = 0; i < faces.GetLength(0); i++)
                {
                    mesh.Faces.AddFace(faces[i, 0], faces[i, 1], faces[i, 2], faces[i, 3]);
                }
            }
            
           mesh.Normals.ComputeNormals();
           mesh.Compact();

           return mesh;
        }

       public static double[,] RhinoMeshVertices(RhinoNamespace.Geometry.Mesh mesh)
       {
           int n = mesh.Vertices.Count;
           double[,] vertexCoordinates = new double[n, 3];
           var e = mesh.Vertices.GetEnumerator();

           for (int i = 0; i < n; i++)
           {
               e.MoveNext();
               vertexCoordinates[i, 0] = e.Current.X;
               vertexCoordinates[i, 1] = e.Current.Y;
               vertexCoordinates[i, 2] = e.Current.Z;
           }

           return vertexCoordinates;
       }

      public static int[][] RhinoMeshFaces(RhinoNamespace.Geometry.Mesh mesh)
       {
           int n = mesh.Faces.Count;
           int[][] vertexIndices = new int[n][];
           var e = mesh.Faces.GetEnumerator();

           for (int i=0; i < n; i++) {
                e.MoveNext();
                if (e.Current.IsTriangle)
                {
                    vertexIndices[i] = new int[3];
                    vertexIndices[i][0] = e.Current.A + 1;
                    vertexIndices[i][1] = e.Current.B + 1;
                    vertexIndices[i][2] = e.Current.C + 1;
                }
                else
                {
                    vertexIndices[i] = new int[4];
                    vertexIndices[i][0] = e.Current.A + 1;
                    vertexIndices[i][1] = e.Current.B + 1;
                    vertexIndices[i][2] = e.Current.C + 1;
                    vertexIndices[i][3] = e.Current.D + 1;
                }
            }

           return vertexIndices;
       }

      public static RhinoNamespace.Geometry.Point3d[] ToRhinoPoint3dArray(double[,] data)
      {
          RhinoNamespace.Geometry.Point3d[] result = new RhinoNamespace.Geometry.Point3d[data.GetLength(0)];

          for (int i = 0; i < data.GetLength(0); i++)
          {
              result[i] = new RhinoNamespace.Geometry.Point3d(data[i, 0], data[i, 1], data[i, 2]);
          }

          return result;
      }

      public static System.Drawing.Color[] ToRhinoColorList(double[,] data)
      {
          System.Drawing.Color[] result = new System.Drawing.Color[data.GetLength(0)];

          for (int i = 0; i < data.GetLength(0); i++)
          {
              result[i] = System.Drawing.Color.FromArgb((int)(255 * data[i, 0]), (int)(255 * data[i, 1]), (int)(255 * data[i, 2]));
          }

          return result;
      }

    }
}