# Multi-stage build for Payment.Api using official Microsoft .NET 10 Alpine images
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
COPY ["Payment.Api/Payment.Api.csproj", "Payment.Api/"]
COPY ["Payment.Application/Payment.Application.csproj", "Payment.Application/"]
COPY ["Payment.Infrastructure/Payment.Infrastructure.csproj", "Payment.Infrastructure/"]
COPY ["Payment.Core/Payment.Core.csproj", "Payment.Core/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./Payment.Api/Payment.Api.csproj"
COPY . .
WORKDIR "/src/Payment.Api"
RUN dotnet build "./Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.Api.dll"]

