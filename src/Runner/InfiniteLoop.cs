using System.Threading.Tasks;

namespace Runner
{
    class InfiniteLoop : ITestCase
    {
        public Task Run(string[] args)
        {
            while(true)
            {
            }
        }
    }
}
