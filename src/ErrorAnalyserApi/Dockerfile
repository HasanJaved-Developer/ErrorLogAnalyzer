# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ErrorAnalyserApi + the SDKs it depends on
COPY ErrorAnalyserApi/ErrorAnalyserApi.csproj ErrorAnalyserApi/

RUN dotnet restore ErrorAnalyserApi/ErrorAnalyserApi.csproj

COPY . .
RUN dotnet publish ErrorAnalyserApi/ErrorAnalyserApi.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install required libraries for Microsoft.Data.SqlClient on Linux
RUN apt-get update && apt-get install -y \
    libkrb5-3 \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "ErrorAnalyserApi.dll"]
