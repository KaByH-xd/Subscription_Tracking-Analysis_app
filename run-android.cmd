@echo off
rem Start an Android emulator or connect a phone with USB debugging first
rem TEMP is moved to an ASCII path: Android tools fail on non-Latin user names
if not exist C:\3kurs\tmp mkdir C:\3kurs\tmp
set TEMP=C:\3kurs\tmp
set TMP=C:\3kurs\tmp
cd /d "%~dp0SubscriptionTracker"
dotnet build -t:Run -f net9.0-android -p:TargetFrameworks=net9.0-android
pause
