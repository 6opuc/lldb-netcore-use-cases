using System.Threading;

namespace Runner
{
    class InfiniteLoop : ITestCase
    {
        public void Run(string[] args)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                }
            });
            thread.Start();
            thread.Join();
        }
    }
}
