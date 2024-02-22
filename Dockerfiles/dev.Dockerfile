##See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#
##Set the base image to the .NET 6 SDK
#FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
#WORKDIR /app
#
## Copy the project files and restore dependencies
##COPY HotReload.Sample.API/HotReload.Sample.API.csproj .
#
#COPY . .
##COPY app_backend/app_backend.csproj .
#RUN dotnet restore
#
## Copy the remaining project files
#
#WORKDIR /app/app_backend
## Start the application in watch mode
#ENTRYPOINT ["dotnet", "watch", "run" ,"--urls", "https://*:443;http://*:80"]

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY ["app_backend/app_backend.csproj", "app_backend/"]

RUN dotnet restore "app_backend/app_backend.csproj"
COPY . .

RUN dotnet build

WORKDIR "/app/app_backend"
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "https://*:443;http://*:80"]