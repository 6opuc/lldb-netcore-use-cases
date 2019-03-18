# Process hang with idle CPU
Our dockerized dotnet application hangs and we don't know why.
We know, that process is idle(CPU usage near 0%).

## Steps to reproduce
1. Run test app:
```bash
docker run -it 6opuc/lldb-netcore-use-cases InfiniteWait
```

2. Check, that our app is idle(%CPU=0.0):
```bash
top -c -p $(pgrep -d',' -f dotnet)
```

## Steps to analyze problem
1. Get id of container with our application(dotnet Runner.dll ...):
```bash
docker ps
```

2. Run container with createdump utility:
```bash
docker run --rm -it \
	--cap-add sys_admin \
	--cap-add sys_ptrace \
	--net=container:0a0628d7600f \
	--pid=container:0a0628d7600f \
	-v /tmp:/tmp \
	6opuc/lldb-netcore \
	/bin/bash
```
where 0a0628d7600f is id of container with our application.

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
