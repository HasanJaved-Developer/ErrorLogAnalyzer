# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ErrorAnalyserApi + McpServer (spawned as a child process by the API) + the SDKs they depend on
COPY src/ErrorAnalyserApi/ErrorAnalyserApi.csproj ErrorAnalyserApi/
COPY src/McpServer/McpServer.csproj McpServer/
COPY src/DataAccess/DataAccess.csproj DataAccess/

RUN dotnet restore ErrorAnalyserApi/ErrorAnalyserApi.csproj
RUN dotnet restore McpServer/McpServer.csproj

COPY . .
RUN dotnet publish src/ErrorAnalyserApi/ErrorAnalyserApi.csproj -c Release -o /app
RUN dotnet publish src/McpServer/McpServer.csproj -c Release -o /app/mcpserver

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install required libraries for Microsoft.Data.SqlClient on Linux
RUN apt-get update && apt-get install -y \
    libkrb5-3 \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .
COPY --from=build /app/mcpserver /app/mcpserver
RUN chmod +x /app/mcpserver/McpServer

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "ErrorAnalyserApi.dll"]
