# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY BancoAlimentar.AlimentaEstaIdeia.Web/*.csproj ./BancoAlimentar.AlimentaEstaIdeia.Web/
COPY BancoAlimentar.AlimentaEstaIdeia.Repository/*.csproj ./BancoAlimentar.AlimentaEstaIdeia.Repository/
COPY BancoAlimentar.AlimentaEstaIdeia.Model/*.csproj ./BancoAlimentar.AlimentaEstaIdeia.Model/
COPY BancoAlimentar.AlimentaEstaIdeia.RepositoryTests/*.csproj ./BancoAlimentar.AlimentaEstaIdeia.RepositoryTests/
COPY BancoAlimentar.AlimentaEstaIdeia.WebTests/*.csproj ./BancoAlimentar.AlimentaEstaIdeia.WebTests/
RUN dotnet restore

# Copy everything else and build
COPY BancoAlimentar.AlimentaEstaIdeia.Web/. ./BancoAlimentar.AlimentaEstaIdeia.Web/
COPY BancoAlimentar.AlimentaEstaIdeia.Repository/. ./BancoAlimentar.AlimentaEstaIdeia.Repository/
COPY BancoAlimentar.AlimentaEstaIdeia.Model/. ./BancoAlimentar.AlimentaEstaIdeia.Model/
COPY BancoAlimentar.AlimentaEstaIdeia.RepositoryTests/. ./BancoAlimentar.AlimentaEstaIdeia.RepositoryTests/
COPY BancoAlimentar.AlimentaEstaIdeia.WebTests/. ./BancoAlimentar.AlimentaEstaIdeia.WebTests/

WORKDIR /app/BancoAlimentar.AlimentaEstaIdeia.Web

RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/BancoAlimentar.AlimentaEstaIdeia.Web/out ./
ENTRYPOINT ["dotnet", "BancoAlimentar.AlimentaEstaIdeia.Web.dll"]