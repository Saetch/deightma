docker build -f .\Dockerfiles\aot\Dockerfile --tag node_cs_aot .
docker build -f .\Dockerfiles\aot\Dockerfile-deb --tag node_cs_aot-deb .
docker build -d .\Dockerfiles\rt\Dockerfile --tag node_cs_rt .