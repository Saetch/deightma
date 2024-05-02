REM this batch file is supposed to launch a bunch of nodes in a loop!
REM it is supposed to be called from the command line with the number of nodes to launch as an argument


REM check if the number of arguments is correct
if "%1"=="" (
    echo Usage: %0 width_of_the_grid height_of_the_grid
    exit /b
)


if "%2"=="" (
    echo Usage: %0 width_of_the_grid height_of_the_grid
    exit /b
)


REM set the number of nodes to launch
set width=%1
set height=%2

docker network create cluster_distr-network

for /l %%x in (0,1,%width%) do (
    for /l %%y in (0,1,%height%) do (
        docker run --name node_x_%%x_y_%%y --network cluster_distr-network -d -e WIDTH=%width% -e HEIGHT=%height% -e OFFSET_X=%%x -e OFFSET_Y=%%y node_cs_aot
    )
)

