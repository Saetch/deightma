FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5552

ENV ASPNETCORE_URLS=http://+:5552

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Node_cs.csproj", "./"]
RUN dotnet restore "Node_cs.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Node_cs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Node_cs.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apk add curl
ENTRYPOINT ["dotnet", "Node_cs.dll"]
