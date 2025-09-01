# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all .csproj files and restore dependencies first
# This leverages Docker layer caching
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]
COPY ["src/ApiGateway/ApiGateway.csproj", "src/ApiGateway/"]


# Copy the solution file and Directory.Packages.props
COPY ["MySolution.sln", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "MySolution.sln"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build/WebAPI

WORKDIR "/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build/ApiGateway

# Stage 2: Publish the applications
FROM build AS publish
RUN dotnet publish "/src/WebAPI/WebAPI.csproj" -c Release -o /app/publish/WebAPI --no-restore
RUN dotnet publish "/src/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish/ApiGateway --no-restore

# Stage 3: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output for WebAPI
COPY --from=publish /app/publish/WebAPI .
# Entrypoint will be set in docker-compose

# Copy published output for ApiGateway
# We will create another image from this stage for the gateway
COPY --from=publish /app/publish/ApiGateway /app_gateway