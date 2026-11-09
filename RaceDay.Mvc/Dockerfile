
# Runtime image: used to run the finished MVC application.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

USER $APP_UID

WORKDIR /app

EXPOSE 8080

# SDK image: used to restore, build and publish the application.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project files first to support Docker build caching.
COPY ["RaceDay.Mvc/RaceDay.Mvc.csproj", "RaceDay.Mvc/"]

COPY ["RaceDay.Contracts/RaceDay.Contracts.csproj", "RaceDay.Contracts/"]

# Restore NuGet packages.
RUN dotnet restore "RaceDay.Mvc/RaceDay.Mvc.csproj"

# Copy the remaining source code.
COPY . .

# Build the MVC application.
WORKDIR "/src/RaceDay.Mvc"

RUN dotnet build "RaceDay.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the MVC application.
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "RaceDay.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Create the final runtime image.
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
