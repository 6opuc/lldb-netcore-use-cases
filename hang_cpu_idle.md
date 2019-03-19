# Process hang with idle CPU
Our dockerized dotnet application hangs and we don't know why.
We know, that process is idle(CPU usage near 0%).

## Steps to reproduce
1. Run test app:
```bash
docker run -it 6opuc/lldb-netcore-use-cases InfiniteWait
```

2. Check, that our app is idle(%CPU=~0.0):
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
	--net=container:501ed06f7844 \
	--pid=container:501ed06f7844 \
	-v /tmp:/tmp \
	6opuc/lldb-netcore \
	/bin/bash
```
where 501ed06f7844 is id of container with our application.

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

6. Print main thread(selected by default) stack trace using command: `clrstack`
```
OS Thread Id: 0x1 (1)
        Child SP               IP Call Site
00007FFCA0E8A1E0 00007f951481017f [HelperMethodFrame_1OBJ: 00007ffca0e8a1e0] Internal.Runtime.Augments.RuntimeThread.JoinInternal(Int32)
00007FFCA0E8A310 00007F9499A0CDCF Internal.Runtime.Augments.RuntimeThread.Join()
00007FFCA0E8A320 00007F949A2259FF System.Threading.Thread.Join()
00007FFCA0E8A330 00007F9499EF37FA Runner.InfiniteWait.Run(System.String[])
00007FFCA0E8A3A0 00007F9499EF209E Runner.Program.Main(System.String[])
00007FFCA0E8A710 00007f95130b3edf [GCFrame: 00007ffca0e8a710]
00007FFCA0E8AB10 00007f95130b3edf [GCFrame: 00007ffca0e8ab10]
```
We see from stack trace that main thread stuck at our Runner.InfiniteWait.Run method waiting for another thread finish its job.

7. Look at stack traces of all threads. We are looking for "Runner.InfiniteWait" in stack traces. Run command `eestack`
```
...
Thread   8
...
00007F9503FFE690 00007f9512f9a9ae libcoreclr.so!AwareLock::Enter() + 0xde, calling libcoreclr.so!AwareLock::EnterEpilogHelper(Thread*, int)
00007F9503FFE6E0 00007f9513021f5a libcoreclr.so!JIT_MonEnter_Helper(Object*, unsigned char*, void*) + 0x14a, calling libcoreclr.so!ObjHeader::EnterObjMonitor()
00007F9503FFE800 00007f95130b4bc4 libcoreclr.so!ThePreStub + 0x5c, calling libcoreclr.so!PreStubWorker
00007F9503FFE850 00007f9513021ed4 libcoreclr.so!JIT_MonEnter_Helper(Object*, unsigned char*, void*) + 0xc4, calling libcoreclr.so!LazyMachStateCaptureState
00007F9503FFE8A0 00007f95130222a6 libcoreclr.so!JIT_MonReliableEnter_Portable + 0x36, calling libcoreclr.so!JIT_MonEnter_Helper(Object*, unsigned char*, void*)
00007F9503FFE8C0 00007f9499ef390f (MethodDesc 00007f949928cdf8 + 0x4f Runner.InfiniteWait+<>c__DisplayClass0_0.<Run>b__0()), calling (MethodDesc 00007f94994c00e0 + 0 System.Threading.Monitor.Enter(System.Object, Boolean ByRef))
00007F9503FFE8F0 00007f949a2250b0 (MethodDesc 00007f949a1f3420 + 0x40 System.Threading.Thread.ThreadMain_ThreadStart())
...
```
We see from stack trace, that thread 8 stuck at System.Threading.Monitor.Enter

8. Look at source code of our method Runner.InfiniteWait.Run: https://github.com/6opuc/lldb-netcore-use-cases/blob/master/src/Runner/InfiniteWait.cs
```
    var locker = new object();
    lock (locker)
    {
	var thread = new Thread(() =>
	{
	    lock(locker)
	    {
	    }
	});
	thread.Start();
	thread.Join();
    }
```
We see, that two threads are using one object for synchronization and:
- main thread acquired lock on object and got stuck waiting for worker thread to finish
- worker thread got stuck waiting for main thread to release lock on the same object

## TODO
There is no `syncblk` command in current version of sosplugin for dotnet(2.2.3). It is available only in preview version. These instructions should be rewritten, when `syncblk` will be available in stable branches.
