dotnet publish .\Client\ -c Release
docker-compose -f .\Cluster\docker-compose.yml build
docker build -f .\Cluster\Node_cs\Dockerfiles\aot\Dockerfile --tag node_cs_aot .\Cluster\Node_cs\
docker-compose -f .\Cluster\docker-compose.yml up -d 
.\Cluster\launch_nodes.bat 3 2
dotnet .\Client\bin\Release\net8.0\Client.dll