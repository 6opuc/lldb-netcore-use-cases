# Process hang with high CPU usage
Our dockerized dotnet application hangs and we don't know why.
We know, that process is using a lot of CPU time(CPU usage near 100% or uses 1 CPU core).

## Steps to reproduce
1. Run test app:
```bash
docker run -it 6opuc/lldb-netcore-use-cases InfiniteLoop
```

2. Check, that our app is using a lot of CPU time(%CPU=~100.0):
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
	--net=container:f827a6b79300 \
	--pid=container:f827a6b79300 \
	-v /tmp:/tmp \
	6opuc/lldb-netcore \
	/bin/bash
```
Where f827a6b79300 is id of container with our application.

3. Find PID of dotnet process we need to analyze(dotnet Runner.dll ...):
```bash
ps aux
```
In this example PID is "1"

4. Find id(PID) of thread with highest CPU utilization(%CPU=~100.0)
```bash
top -p 1 -H
```
Where 1 is id of out process.
In this example thread id is 16.

5. Create coredump of dotnet process and exit from container:
```bash
createdump -u -f /tmp/coredump 1
exit
```
Where 1 is PID of dotnet process

6. Open coredump with debugger:
```bash
docker run --rm -it -v /tmp/coredump:/tmp/coredump 6opuc/lldb-netcore
```

7. Print list of all threads using command `thread list`.
```
* thread #1: tid = 1, 0x00007feb80ca117f libpthread.so.0`__pthread_cond_wait + 191, name = 'dotnet', stop reason = signal SIGABRT
  thread #2: tid = 7, 0x00007feb8013c469 libc.so.6`syscall + 25, stop reason = signal SIGABRT
  thread #3: tid = 8, 0x00007feb8013c469 libc.so.6`syscall + 25, stop reason = signal SIGABRT
  thread #4: tid = 11, 0x00007feb801378bd libc.so.6`__poll + 45, stop reason = signal SIGABRT
  thread #5: tid = 12, 0x00007feb80ca485d libpthread.so.0`__GI_open64 + 45, stop reason = signal SIGABRT
  thread #6: tid = 13, 0x00007feb80ca117f libpthread.so.0`__pthread_cond_wait + 191, stop reason = signal SIGABRT
  thread #7: tid = 14, 0x00007feb80ca1528 libpthread.so.0`__pthread_cond_timedwait + 296, stop reason = signal SIGABRT
  thread #8: tid = 15, 0x00007feb80ca1528 libpthread.so.0`__pthread_cond_timedwait + 296, stop reason = signal SIGABRT
  thread #9: tid = 16, 0x00007feb06373dcb, stop reason = signal SIGABRT
  thread #10: tid = 17, 0x00007feb80ca1528 libpthread.so.0`__pthread_cond_timedwait + 296, stop reason = signal SIGABRT
```
Find our thread by its 'tid': it should be equal to PID from `top -p 1 -H`(in our example it is 16).
In our example thread# is 9.

8. Switch to our thread:
```
thread select 9
```
Where 9 is our thread# from `thread list`.

9. Print thread stack trace using command `clrstack`:
```
OS Thread Id: 0x10 (9)
        Child SP               IP Call Site
00007FEAE657F900 00007FEB06373DCB Runner.InfiniteLoop+<>c.<Run>b__0_0()
00007FEAE657F920 00007FEB066A50B0 System.Threading.Thread.ThreadMain_ThreadStart()
00007FEAE657F930 00007FEB05D947FD System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object)
00007FEAE657FC90 00007feb7f53b17f [GCFrame: 00007feae657fc90]
00007FEAE657FD50 00007feb7f53b17f [DebuggerU2MCatchHandlerFrame: 00007feae657fd50]
```
We see that thread is busy somewhere inside Runner.InfiniteLoop.Run()

10. Look at source code of our method Runner.InfiniteLoop.Run: https://github.com/6opuc/lldb-netcore-use-cases/blob/master/src/Runner/InfiniteLoop.cs
```
var thread = new Thread(() =>
{
    while (true)
    {
    }
});
thread.Start();
thread.Join();
```
We see, that high CPU usage is caused by infinite loop inside thread worker method.
