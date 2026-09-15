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
COPY ["PaymentService/Payment.Api/Payment.Api.csproj", "PaymentService/Payment.Api/"]
COPY ["PaymentService/Payment.Application/Payment.Application.csproj", "PaymentService/Payment.Application/"]
COPY ["PaymentService/Payment.Infrastructure/Payment.Infrastructure.csproj", "PaymentService/Payment.Infrastructure/"]
COPY ["PaymentService/Payment.Core/Payment.Core.csproj", "PaymentService/Payment.Core/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./PaymentService/Payment.Api/Payment.Api.csproj" --disable-parallel
COPY . .
WORKDIR "/src/PaymentService/Payment.Api"
RUN dotnet build "./Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PaymentService/Payment.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService/Payment.Api.dll"]

