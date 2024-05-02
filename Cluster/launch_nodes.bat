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
set /a width_max_index=%1-1
set /a height_max_index=%2-1

docker network create cluster_distr-network

REM width and height are hardcoded for now
for /l %%x in (0,1,%width_max_index%) do (
    for /l %%y in (0,1,%height_max_index%) do (
        docker run --name node_x_%%x_y_%%y --network cluster_distr-network -d -e WIDTH=2 -e HEIGHT=2 -e OFFSET_X=%%x -e OFFSET_Y=%%y node_cs_aot
    )
)

