
# Stage 1: Build and publish
using Microsoft.EntityFrameworkCore.Infrastructure;

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the API project and restore dependencies
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore RaceDay.API/RaceDay.API.csproj

# Copy the remaining source code
COPY RaceDay.API/ RaceDay.API/

# Publish the API
RUN dotnet publish RaceDay.API/RaceDay.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]

# Stage 1: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the API project and restore dependencies
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore RaceDay.API/RaceDay.API.csproj

# Copy the remaining source code
COPY RaceDay.API/ RaceDay.API/

# Publish the API
RUN dotnet publish RaceDay.API/RaceDay.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
