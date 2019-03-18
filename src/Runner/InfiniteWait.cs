using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    class InfiniteWait : ITestCase
    {
        public async Task Run(string[] args)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }
    }
}
