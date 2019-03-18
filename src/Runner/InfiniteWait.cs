using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    class InfiniteWait : ITestCase
    {
        public Task Run(string[] args)
        {
            return Task.Delay(Timeout.InfiniteTimeSpan);
        }
    }
}
