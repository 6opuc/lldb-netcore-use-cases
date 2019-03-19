using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Runner
{
    class Program
    {
        public static void Main(string[] args)
        {
            var availableTestCases = GetAvailableTestCases();

            var testCaseName = args.FirstOrDefault();
            if (testCaseName == null)
            {
                Console.Error.WriteLine($"Test case name was expected in first argument. Available test case names: {string.Join(",", availableTestCases.Keys)}.");
                Environment.Exit(1);
            }

            if (!availableTestCases.TryGetValue(testCaseName, out ITestCase testCase))
            {
                Console.Error.WriteLine($"Unknown test case name. Available test case names: {string.Join(",", availableTestCases.Keys)}.");
                Environment.Exit(1);
            }

            testCase.Run(args);
        }

        private static Dictionary<string, ITestCase> GetAvailableTestCases()
        {
            var testCases = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && typeof(ITestCase).IsAssignableFrom(type))
                .ToDictionary(type => type.Name, type => (ITestCase)Activator.CreateInstance(type), StringComparer.InvariantCultureIgnoreCase);
            return testCases;
        }
    }
}
