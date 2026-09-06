@echo off
set "tennisPlayer=%~dp0Builds\RoboOpen-Windows-v0.3\RoboOpen.exe"
if not exist "%tennisPlayer%" set "tennisPlayer=%~dp0Builds\RoboOpen-Windows\RoboOpen.exe"
start "Robo Open" "%tennisPlayer%" -screen-fullscreen 0 -screen-width 1600 -screen-height 900
