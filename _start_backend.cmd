@echo off
set DOTNET_CLI_HOME=C:\OneSProject\.dotnet-home
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set ASPNETCORE_URLS=https://localhost:7164;http://localhost:5232
dotnet run --no-build > _run_out.log 2> _run_err.log

