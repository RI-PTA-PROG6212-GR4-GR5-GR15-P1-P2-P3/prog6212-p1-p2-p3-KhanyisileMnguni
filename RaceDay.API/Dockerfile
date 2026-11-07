# Stage 1: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["RaceDay.API/RaceDay.API.csproj", "RaceDay.API/"]
RUN dotnet restore "RaceDay.API/RaceDay.API.csproj"

COPY . .
WORKDIR /src/RaceDay.API
RUN dotnet publish "RaceDay.API.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
