# Excessive memory usage
Our dockerized dotnet application uses too much memory.

## Steps to reproduce
1. Run test app and wait for 'Ready' message:
```bash
docker run -it 6opuc/lldb-netcore-use-cases MemoryLeak
```

2. Check, that our app uses too much memory(REZ=~300000 KB):
```bash
top -c -p $(pgrep -d',' -f dotnet)
```

## Steps to analyze
1. Get id of container with our application(dotnet Runner.dll ...):
```bash
docker ps
```

2. Run container with createdump utility:
```bash
docker run --rm -it \
	--cap-add sys_admin \
	--cap-add sys_ptrace \
	--net=container:7cc287a840cf \
	--pid=container:7cc287a840cf \
	-v /tmp:/tmp \
	6opuc/lldb-netcore \
	/bin/bash
```
where 7cc287a840cf is id of container with our application.

3. Find PID of dotnet process we need to analyze(dotnet Runner.dll ...):
```bash
ps aux
```
In this example PID is "1"

4. Create coredump of dotnet process and exit from container:
```bash
createdump -u -f /tmp/coredump 1
exit
```
Where 1 is PID of dotnet process

5. Open coredump with debugger:
```bash
docker run --rm -it -v /tmp/coredump:/tmp/coredump 6opuc/lldb-netcore
```

6. Print managed heap statistics using command `dumpheap -stat`
```
...
00007f5cc5419ee8      331       252058 System.Char[]
00007f5cc542cd20     4405       502152 System.String
00000000015c2050     5108     45193814      Free
00007f5cc541a0e8     2635    216166941 System.Byte[]
```
We see from statistics that most part of memory is used by `System.Byte[]`(mt=00007f5cc541a0e8).

7. Print all instances of `System.Byte[]`(mt=00007f5cc541a0e8) using command `dumpheap -mt 00007f5cc541a0e8`
```
00007f5caed153c0 00007f5cc541a0e8    84024
00007f5caed2e388 00007f5cc541a0e8    84024
00007f5caed47350 00007f5cc541a0e8    84024
00007f5caed60318 00007f5cc541a0e8    84024
00007f5caed792e0 00007f5cc541a0e8    84024
00007f5caed922a8 00007f5cc541a0e8    84024
00007f5caedab270 00007f5cc541a0e8    84024
00007f5caedc4238 00007f5cc541a0e8    84024
00007f5caeddd200 00007f5cc541a0e8    84024
...
00007f5cb7a592c0 00007f5cc541a0e8    84024
00007f5cb7a6dcd0 00007f5cc541a0e8     1048
00007f5cb7a72ad8 00007f5cc541a0e8      795
00007f5cb7a72df8 00007f5cc541a0e8       24
```
We see that there are many arrays of the same size(84024)

8. Find who is holding reference to one of those arrays(in our example it is 00007f5caed153c0) using command `gcroot 00007f5caed153c0`
```
Thread 1:
    00007FFC6B6B0600 00007F5CC5653D07 Runner.MemoryLeak.Run(System.String[])
        rbp-10: 00007ffc6b6b0640
            ->  00007F5CA8020EE8 Runner.MemoryLeak
            ->  00007F5CB7A59280 System.EventHandler
            ->  00007F5CB4745410 System.Object[]
            ->  00007F5CAED15340 System.EventHandler
            ->  00007F5CAED15328 Runner.MemoryLeak+EventSubscriber
            ->  00007F5CAED153C0 System.Byte[]
```
We can find roots for other arrays the same way and we will see that all roots are the same: 
```
-> an instance of type Runner.MemoryLeak inside Runner.MemoryLeak.Run method
-> an event handler
-> an instance of Runner.MemoryLeak+EventSubscriber
-> our byte array
```

9. Lets look at Runner.MemoryLeak+EventSubscriber source code https://github.com/6opuc/lldb-netcore-use-cases/blob/master/src/Runner/MemoryLeak.cs:
```
class MemoryLeak : ITestCase
    {
        public event EventHandler AnEvent;

        public void Run(string[] args)
        {
            ...
            while (true)
            {
                ...
                var eventSource = new EventSubscriber(this);
            }
            ...
        }

        class EventSubscriber
        {
            ...
            public EventSubscriber(MemoryLeak source)
            {
                source.AnEvent += Source_AnEvent;
                ...
            }
            ...
        }
    }
```
We see that our EventSubscriber has event subscription code in its constructor, but there is no unsubscription code.
Instances of EventSubscriber are created in a loop and all created instances are hold by event handlers of MemoryLeak.AnEvent.

