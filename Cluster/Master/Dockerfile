# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the Docker image
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the remaining project files and build the application
COPY . .
RUN dotnet build -c Release -o /app/build
# Update package lists and install clang
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev
# Publish the application
RUN dotnet publish -p:PublishAot=true -p:StripSymbols=true -c Debug -o /app/publish -r linux-musl-x64

# Switch to the runtime image
FROM alpine:3.19 AS runtime
RUN apk add libc6-compat --no-cache icu-libs

# Set the working directory inside the Docker image
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish/* .
# Set the entry point for the container
ENTRYPOINT [ "/app/Master" ]