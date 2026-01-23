using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpBlock
{
    class PipeServer
    {

        private string PipeName = "";

        private long upSize = 0;

        public static String endResult = "";

        public static String startResult = "";

        public static String secondReulst = "";

        private static volatile int RuningThreadID = 0;
        private int MaxThread;
        public static volatile int SectionCount = 0;

        private static NamedPipeServerStream[] ServerGroup;

        private StringBuilder PipeContentStream = new StringBuilder();

        public static readonly object _locker = new object();

        public PipeServer(int MaxThread, String PipeName)
        {
            this.MaxThread = MaxThread;
            ServerGroup = new NamedPipeServerStream[MaxThread];
            this.PipeName = PipeName;
            RuningThreadID = 0;
            SectionCount = 0;
        }

        public void DisposePipe()
        {
            if (ServerGroup != null)
            {
                foreach (NamedPipeServerStream item in ServerGroup)
                {
                    if (item != null)
                    {
                        try
                        {
                            if (item.IsConnected) item.Disconnect();
                            item.Close();
                            item.Dispose();
                        }
                        catch { }
                    }
                }
            }
        }


        public void StartServers()
        {
            ArrayList ThreadArr = new ArrayList();
            for (int i = 0; i < this.MaxThread; i++)
            {
                Thread thread = new Thread(new ThreadStart(StartNewPipeServer));
                thread.Start();
                ThreadArr.Add(thread);
            }
            foreach (Thread t in ThreadArr)
            {
                t.Join();
            }
        }

        public StringBuilder GetContent()
        {
            return this.PipeContentStream;
        }

        private void StartNewPipeServer()
        {
            NamedPipeServerStream stream = null;
            stream = ServerGroup[RuningThreadID] = new NamedPipeServerStream(
                 this.PipeName,
                 PipeDirection.In,
                 this.MaxThread
                 );

            RuningThreadID += 1;
            StreamReader sr = new StreamReader(stream);

            while (true)
            {
                stream.WaitForConnection();
                String sResult = sr.ReadToEnd();

                if (sResult == "ok")
                {
                    break;
                }
                else
                {
                    lock (_locker)
                    {
                        SharpBlock.Program.WriteLine($"[+] PipeStream {SectionCount} {sResult.Length} {sResult[0]}");

                        if (sResult.Length < upSize && SectionCount > 1)
                        {
                            upSize = sResult.Length;
                            endResult = sResult;
                            SectionCount++;
                        } else
                        {
                            if (SectionCount == 0)
                            {
                                startResult = sResult;
                            } else if (SectionCount == 1)
                            {
                                secondReulst = sResult;
                            } else
                            {
                                upSize = sResult.Length;
                                this.PipeContentStream.Append(sResult);
                            }
                            SectionCount++;

                        }

                    }

                }
                stream.Disconnect();

            }
        }

    }
}
