# Stage 1: Restore - Tải tất cả các dependency
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src
COPY ["MySolution.sln", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]
COPY ["src/ApiGateway/ApiGateway.csproj", "src/ApiGateway/"]
RUN dotnet restore "MySolution.sln"

# Stage 2: Build - Biên dịch toàn bộ source code
FROM restore AS build
COPY . .
# Build toàn bộ solution, tận dụng cache từ stage trước
RUN dotnet build "MySolution.sln" -c Release --no-restore

# Stage 3: Publish WebAPI - Chuẩn bị file chạy cho WebAPI
FROM build AS publish-webapi
RUN dotnet publish "src/WebAPI/WebAPI.csproj" -c Release -o /app/publish --no-build

# Stage 4: Publish ApiGateway - Chuẩn bị file chạy cho ApiGateway
FROM build AS publish-apigateway
RUN dotnet publish "src/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish --no-build

# === Final Images ===

# Final Stage for WebAPI
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS web-api-final
WORKDIR /app
COPY --from=publish-webapi /app/publish .

# Final Stage for ApiGateway
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api-gateway-final
WORKDIR /app
COPY --from=publish-apigateway /app/publish .