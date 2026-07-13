# Multi-stage build for OrderService.Api using official Microsoft .NET 10 Alpine images
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
COPY ["OrderService.Api/OrderService.Api.csproj", "OrderService.Api/"]
COPY ["OrderService.Application/OrderService.Application.csproj", "OrderService.Application/"]
COPY ["OrderService.Infrastructure/OrderService.Infrastructure.csproj", "OrderService.Infrastructure/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./OrderService.Api/OrderService.Api.csproj"
COPY . .
WORKDIR "/src/OrderService.Api"
RUN dotnet build "./OrderService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]