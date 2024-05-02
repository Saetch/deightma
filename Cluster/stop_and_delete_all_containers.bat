@echo off

REM Stop all running Docker containers
for /f "tokens=*" %%i in ('docker ps -a -q') do (
    docker stop %%i
    docker rm %%i
)