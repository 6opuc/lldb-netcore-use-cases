using System;
using System.Threading;

namespace Runner
{
    class MemoryLeak : ITestCase
    {
        public event EventHandler AnEvent;

        public void Run(string[] args)
        {
            var memLimit = 300000000/*300MB*/;
            while (true)
            {
                if (Environment.WorkingSet >= memLimit)
                {
                    break;
                }

                var eventSource = new EventSubscriber(this);
            }
            Console.WriteLine("Ready");
            Thread.Sleep(Timeout.InfiniteTimeSpan);
        }

        class EventSubscriber
        {
            byte[] _state;

            public EventSubscriber(MemoryLeak source)
            {
                source.AnEvent += Source_AnEvent;
                _state = new byte[84000];
            }

            private void Source_AnEvent(object sender, EventArgs e)
            {
            }
        }
    }
}
