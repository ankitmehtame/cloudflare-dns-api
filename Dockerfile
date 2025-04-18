# Use the official .NET SDK image as a base image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the solution file and restore dependencies
COPY *.sln ./
COPY CloudflareDnsApi/*.csproj ./CloudflareDnsApi/
COPY CloudflareDnsApi.Tests/*.csproj ./CloudflareDnsApi.Tests/

RUN dotnet restore

# Copy the rest of the application code
COPY CloudflareDnsApi/. ./CloudflareDnsApi/
COPY CloudflareDnsApi.Tests/. ./CloudflareDnsApi.Tests/

# Build the application
RUN dotnet build CloudflareDnsApi/CloudflareDnsApi.csproj -c Release --no-restore

# Publish the main application
RUN dotnet publish CloudflareDnsApi/CloudflareDnsApi.csproj -c Release -o out --no-build

# Create a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "CloudflareDnsApi.dll"]