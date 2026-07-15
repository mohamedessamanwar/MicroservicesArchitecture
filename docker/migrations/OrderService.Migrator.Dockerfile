# Multi-stage build for OrderService EF Core Migration Bundle using .NET 10 Alpine
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for caching restoration
COPY ["OrderService.Api/OrderService.Api.csproj", "OrderService.Api/"]
COPY ["OrderService.Application/OrderService.Application.csproj", "OrderService.Application/"]
COPY ["OrderService.Infrastructure/OrderService.Infrastructure.csproj", "OrderService.Infrastructure/"]
COPY ["OrderService.Domain/OrderService.Domain.csproj", "OrderService.Domain/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./OrderService.Api/OrderService.Api.csproj"

# Install dotnet-ef tool
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROLL_FORWARD=Major

# Copy remaining source code
COPY . .
WORKDIR "/src/OrderService.Api"

# Generate self-contained EF Core Migration Bundle for Alpine Linux musl x64
RUN dotnet ef migrations bundle \
    --project ../OrderService.Infrastructure \
    --startup-project . \
    --context AppDbContext \
    --output /migration/order-migrate \
    --self-contained \
    -r linux-musl-x64 \
    --configuration Release

# Final lightweight runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
ENV DOTNET_ROLL_FORWARD=Major
WORKDIR /OrderService.Api
COPY --from=build /src/OrderService.Api/appsettings*.json ./
WORKDIR /migration
COPY --from=build /migration .
COPY --from=build /src/OrderService.Api/appsettings*.json ./
ENTRYPOINT ["/migration/order-migrate"]
