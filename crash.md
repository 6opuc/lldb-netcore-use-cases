# Process crash
Our dockerized dotnet application crashed and we don't have logs.

## Steps to reproduce
1. Run test app:
```bash
docker run -it \
	--cap-add sys_ptrace \
	-e COMPlus_DbgEnableMiniDump=1 \
	-e COMPlus_DbgMiniDumpName=/app/coredump \
	-e COMPlus_DbgMiniDumpType=4 \
	6opuc/lldb-netcore-use-cases \
	UnhandledException
```

## Steps to analyze
1. Get id of crashed container with our application(dotnet Runner.dll ...):
```bash
docker ps -a
```

2. Copy crashed process working directory from container to temporary directory on host:
```bash
docker cp f7cc2ea3a84c:/app /tmp
```
Where f7cc2ea3a84c is id of container with our application and /app is crashed process working directory inside container filesystem.

3. Open coredump with debugger:
```bash
docker run --rm -it -v /tmp/app:/app -e COREDUMP_PATH=/app/coredump 6opuc/lldb-netcore
```

4. Print exception with command: `pe -lines`.
```
(lldb) sos PrintException -lines
Exception object: 00007f7960021300
Exception type:   System.DivideByZeroException
Message:          Attempted to divide by zero.
InnerException:   <none>
StackTrace (generated):
    SP               IP               Function
    00007FFDCD8CA120 00007F797D3A3B58 Runner.dll!Runner.UnhandledException.Calc(Input)+0x38 [/sources/src/Runner/UnhandledException.cs @ 23]
    00007FFDCD8CA150 00007F797D3A3A90 Runner.dll!Runner.UnhandledException.Run(System.String[])+0x160 [/sources/src/Runner/UnhandledException.cs @ 17]
    00007FFDCD8CA1C0 00007F797D3A253E Runner.dll!Runner.Program.Main(System.String[])+0x20e [/sources/src/Runner/Program.cs @ 27]
StackTraceString: <none>
HResult: 80020012
```
We see exception which caused application crash(System.DivideByZeroException)

5. Print stack trace with arguments: `clrstack -a`
```
OS Thread Id: 0x1 (1)
        Child SP               IP Call Site
00007FFDCD8C8EC0 00007f79f7cd7b5a [FaultingExceptionFrame: 00007ffdcd8c8ec0]
00007FFDCD8CA120 00007F797D3A3B58 Runner.UnhandledException.Calc(Input) [/sources/src/Runner/UnhandledException.cs @ 23]
    PARAMETERS:
        this (0x00007FFDCD8CA138) = 0x00007f7960021028
        input (0x00007FFDCD8CA130) = 0x00007f79600212e8
    LOCALS:
        0x00007FFDCD8CA12C = 0x0000000000000000
00007FFDCD8CA150 00007F797D3A3A90 Runner.UnhandledException.Run(System.String[]) [/sources/src/Runner/UnhandledException.cs @ 17]
    PARAMETERS:
        this (0x00007FFDCD8CA1A0) = 0x00007f7960021028
        args (0x00007FFDCD8CA198) = 0x00007f796001cfa0
    LOCALS:
        0x00007FFDCD8CA190 = 0x00007f7960021288
        0x00007FFDCD8CA188 = 0x00007f7960021288
        0x00007FFDCD8CA184 = 0x0000000000000002
        0x00007FFDCD8CA178 = 0x00007f79600212e8
00007FFDCD8CA1C0 00007F797D3A253E Runner.Program.Main(System.String[]) [/sources/src/Runner/Program.cs @ 27]
    PARAMETERS:
        args (0x00007FFDCD8CA260) = 0x00007f796001cfa0
    LOCALS:
        0x00007FFDCD8CA258 = 0x00007f7960020a78
        0x00007FFDCD8CA250 = 0x00007f796001cfc0
        0x00007FFDCD8CA248 = 0x00007f7960021028
        0x00007FFDCD8CA244 = 0x0000000000000000
        0x00007FFDCD8CA240 = 0x0000000000000000
00007FFDCD8CA538 00007f79f656e17f [GCFrame: 00007ffdcd8ca538]
00007FFDCD8CA940 00007f79f656e17f [GCFrame: 00007ffdcd8ca940]
```
We see that exception was thrown in Runner.UnhandledException.Calc(Input) and object which was passed as argument into method is available at address 0x00007f79600212e8

6. Dump argument 'input' from last method call(Runner.UnhandledException.Calc) using command: `dumpobj 0x00007f79600212e8`
```
Name:        Runner.UnhandledException+Input
MethodTable: 00007f797d6a14c0
EEClass:     00007f797d608398
Size:        24(0x18) bytes
File:        /app/Runner.dll
Fields:
              MT    Field   Offset                 Type VT     Attr            Value Name
00007f797d17f660  400000a        8         System.Int32  1 instance                3 A
00007f797d17f660  400000b        c         System.Int32  1 instance                0 B
```
We see object property values: A=3 and B=0.

7. Look at source code of our method Runner.UnhandledException.Calc: https://github.com/6opuc/lldb-netcore-use-cases/blob/master/src/Runner/UnhandledException.cs
```
private void Calc(Input input)
{
    var result = input.A / input.B;
}
```
So our method failed because of devision by zero when we passed input.B=0.
