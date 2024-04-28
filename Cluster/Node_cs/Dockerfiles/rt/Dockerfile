# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the Docker image
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the remaining project files and build the application
COPY . .
RUN dotnet build -c Release
# Update package lists and install clang
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev
# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Switch to the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory inside the Docker image
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Node_cs.dll"]