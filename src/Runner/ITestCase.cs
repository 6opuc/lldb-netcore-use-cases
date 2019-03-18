using System.Threading.Tasks;

namespace Runner
{
    public interface ITestCase
    {
        Task Run(string[] args);
    }
}
