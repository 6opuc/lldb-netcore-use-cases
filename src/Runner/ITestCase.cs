using System.Threading.Tasks;

namespace Runner
{
    public interface ITestCase
    {
        void Run(string[] args);
    }
}
