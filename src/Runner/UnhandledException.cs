using System;

namespace Runner
{
    class UnhandledException : ITestCase
    {
        public void Run(string[] args)
        {
            var inputs = new[]
            {
                new Input{A = 1, B = 2 },
                new Input{A = 2, B = 3 },
                new Input{A = 3, B = 0 }
            };
            foreach (var input in inputs)
            {
                PrintResult(input);
            }
        }

        private void PrintResult(Input input)
        {
            var result = input.A / input.B;
            Console.WriteLine($"A/B={result}");
        }

        private class Input
        {
            public int A;
            public int B;
        }
    }
}
