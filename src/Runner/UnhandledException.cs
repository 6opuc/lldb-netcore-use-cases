using System;

namespace Runner
{
    class UnhandledException : ITestCase
    {
        public void Run(string[] args)
        {
            throw new NotImplementedException();
        }
    }
}
