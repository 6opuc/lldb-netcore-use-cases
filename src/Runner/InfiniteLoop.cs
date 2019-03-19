using System.Threading.Tasks;

namespace Runner
{
    class InfiniteLoop : ITestCase
    {
        public void Run(string[] args)
        {
            while(true)
            {
            }
        }
    }
}
