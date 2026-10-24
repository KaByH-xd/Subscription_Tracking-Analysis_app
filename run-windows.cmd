@echo off
cd /d "%~dp0SubscriptionTracker"
set NETMAJOR=9
echo .NET SDK major version: %NETMAJOR%
dotnet build -t:Run -f net%NETMAJOR%.0-windows10.0.19041.0 -p:TargetFrameworks=net%NETMAJOR%.0-windows10.0.19041.0
pause
