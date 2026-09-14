# Multi-stage build for ProductService.Api using official Microsoft .NET 10 Alpine images
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
ARG APP_UID=1000
ENV APP_UID=$APP_UID
ENV DOTNET_ROLL_FORWARD=Major
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 4024

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ProductService.Api/ProductService.Api.csproj", "ProductService.Api/"]
COPY ["ProductService.Application/ProductService.Application.csproj", "ProductService.Application/"]
COPY ["ProductService.Infrastructure/ProductService.Infrastructure.csproj", "ProductService.Infrastructure/"]
COPY ["ProductService.Domain/ProductService.Domain.csproj", "ProductService.Domain/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./ProductService.Api/ProductService.Api.csproj" --disable-parallel
COPY . .
WORKDIR "/src/ProductService.Api"
RUN dotnet build "./ProductService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProductService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.Api.dll"]
