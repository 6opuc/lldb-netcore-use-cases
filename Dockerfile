FROM mcr.microsoft.com/dotnet/core/sdk:2.2.105 AS builder
RUN git clone  --depth 1 https://github.com/6opuc/lldb-netcore-use-cases.git /sources
RUN dotnet publish /sources/src/Runner/Runner.csproj -o /app -c Debug

FROM mcr.microsoft.com/dotnet/core/runtime:2.2.3
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "Runner.dll"]

