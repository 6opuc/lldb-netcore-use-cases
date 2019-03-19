using System.Threading;

namespace Runner
{
    class InfiniteWait : ITestCase
    {
        public void Run(string[] args)
        {
            var locker = new object();
            lock (locker)
            {
                var thread = new Thread(() =>
                {
                    lock(locker)
                    {
                    }
                });
                thread.Start();
                thread.Join();
            }
        }
    }
}
