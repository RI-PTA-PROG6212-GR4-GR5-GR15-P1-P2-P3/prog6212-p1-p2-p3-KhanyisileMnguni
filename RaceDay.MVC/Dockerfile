# syntax=docker/dockerfile:1

# ---------- Stage 1: build and publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so the NuGet restore layer is cached until the packages change.
COPY RaceDay.MVC.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- Stage 2: small runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Where to find the RaceDay API. "host.docker.internal" means "the computer running Docker",
# so the API running on your machine is reachable from inside the container.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
    ApiSettings__BaseUrl=http://host.docker.internal:5285/

EXPOSE 8080
COPY --from=build /app/publish .

# Run as the built-in non-root user.
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
