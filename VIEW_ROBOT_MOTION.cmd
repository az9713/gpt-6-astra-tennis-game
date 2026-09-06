@echo off
cd /d "%~dp0"
set "tennisPlayer=Builds\RoboOpen-Windows-v0.3\RoboOpen.exe"
if not exist "%tennisPlayer%" set "tennisPlayer=Builds\RoboOpen-Windows\RoboOpen.exe"
start "Robo Open Motion Studio" "%tennisPlayer%" --motion-showcase -screen-fullscreen 0 -screen-width 1600 -screen-height 900
