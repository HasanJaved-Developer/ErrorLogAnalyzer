# ------------ Build stage ------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ApiIntegrationMvc + the SDKs it depends on
COPY src/ApiIntegrationMvc/ApiIntegrationMvc.csproj ApiIntegrationMvc/

RUN dotnet restore ApiIntegrationMvc/ApiIntegrationMvc.csproj

COPY . .
RUN dotnet publish src/ApiIntegrationMvc/ApiIntegrationMvc.csproj -c Release -o /app

# ------------ Runtime stage ------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install required libraries for Microsoft.Data.SqlClient on Linux
RUN apt-get update && apt-get install -y \
    libkrb5-3 \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app ./

EXPOSE 8080
EXPOSE 8081

# Replace with the actual assembly name if different
ENTRYPOINT ["dotnet", "ApiIntegrationMvc.dll"]