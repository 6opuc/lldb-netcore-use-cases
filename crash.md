!!!work in progress!!!


# Process crash
Our dockerized dotnet application crashed and we don't have logs.

## Steps to reproduce
1. Run test app:
```bash
docker run -it 6opuc/lldb-netcore-use-cases UnhandledException
```

## Steps to analyze
1. Get id of crashed container with our application(dotnet Runner.dll ...):
```bash
docker ps -a
```

2. Copy crashed process working directory from container to temporary directory on host:
```bash
docker cp 89e2d0741b69:/app /tmp
```
Where 89e2d0741b69 is id of container with our application and /app is crashed process working directory inside container filesystem.

3. Find crashed process coredump:
```bash
ls /tmp/app/core.*
```

4. Open coredump with debugger:
```bash
docker run --rm -it -v /tmp/app/core.1:/tmp/coredump 6opuc/lldb-netcore
```

5. Print exception with command: `pe -lines`.
```
TODO: currently no line numbers and method names
```

6. Print stack trace with arguments: `clrstack -a`
```
TODO: currently no line numbers, method names and arguments
```

7. Dump argument 'input' from last method call(Runner.UnhandledException.Calc)
```
TODO:
```
We see object properties: A=3 and B=0.

10. Look at source code of our method Runner.UnhandledException.Calc: https://github.com/6opuc/lldb-netcore-use-cases/blob/master/src/Runner/UnhandledException.cs
```
private void Calc(Input input)
{
    var result = input.A / input.B;
}
```
So our method failed because of devision by zero when we passed B=0.
