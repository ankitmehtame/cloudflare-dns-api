# Use the official .NET SDK image as a base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution file, all project files (*.csproj), and all source code
# We copy everything needed for build and restore in one go
COPY *.sln ./
COPY CloudflareDnsApi/*.csproj ./CloudflareDnsApi/
COPY CloudflareDnsApi.Tests/*.csproj ./CloudflareDnsApi.Tests/

COPY CloudflareDnsApi/. ./CloudflareDnsApi/
COPY CloudflareDnsApi.Tests/. ./CloudflareDnsApi.Tests/

# Build the entire solution
# This command will implicitly perform a restore before building
RUN dotnet build CloudflareDnsApi.sln -c Release

# Publish the main application project
# Use --no-build here as the build was already done in the previous step
RUN dotnet publish CloudflareDnsApi/CloudflareDnsApi.csproj -c Release -o out --no-build

# Create a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "CloudflareDnsApi.dll"]