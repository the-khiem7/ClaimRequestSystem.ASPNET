# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 5001


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all csproj files and restore as distinct layers
COPY ["ClaimRequest.API/ClaimRequest.API.csproj", "ClaimRequest.API/"]
COPY ["ClaimRequest.Data/ClaimRequest.DAL.csproj", "ClaimRequest.Data/"]
COPY ["ClaimRequest.Service/ClaimRequest.BLL.csproj", "ClaimRequest.Service/"]
COPY ["ClaimRequest.UnitTest/ClaimRequest.UnitTest.csproj", "ClaimRequest.UnitTest/"]

# Restore packages
RUN dotnet restore "ClaimRequest.API/ClaimRequest.API.csproj"
RUN dotnet restore "ClaimRequest.Data/ClaimRequest.DAL.csproj"
RUN dotnet restore "ClaimRequest.Service/ClaimRequest.BLL.csproj"
RUN dotnet restore "ClaimRequest.UnitTest/ClaimRequest.UnitTest.csproj"

# Copy everything else
COPY . .

# Build the projects
WORKDIR "/src"
RUN dotnet build "ClaimRequest.API/ClaimRequest.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ClaimRequest.API/ClaimRequest.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClaimRequest.API.dll"]