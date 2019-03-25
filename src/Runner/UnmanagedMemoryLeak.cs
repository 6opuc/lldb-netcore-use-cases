using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Runner
{
    class UnmanagedMemoryLeak : ITestCase
    {
        public void Run(string[] args)
        {
            var random = new Random();
            var memLimit = 300000000/*300MB*/;
            while (true)
            {
                //using (new UnmanagedResource(random.Next(1000, 10000000)/*1KB - 10MB*/))
                new UnmanagedResource(random.Next(1000, 10000000)/*1KB - 10MB*/);
                {
                    if (Environment.WorkingSet >= memLimit)
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("Ready");
            Thread.Sleep(Timeout.InfiniteTimeSpan);
        }

        class UnmanagedResource : IDisposable
        {
            IntPtr _pointer;

            public UnmanagedResource(int size)
            {
                _pointer = Marshal.AllocHGlobal(size);
                for (var i=0; i<size; i++)
                {
                    Marshal.WriteByte(_pointer, i, 0);
                }
            }

            public void Dispose()
            {
                Free();
                GC.SuppressFinalize(this);
            }

            private void Free()
            {
                Marshal.FreeHGlobal(_pointer);
            }

            ~UnmanagedResource()
            {
                Free();
            }
        }
    }
}
