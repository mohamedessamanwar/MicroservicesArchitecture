# Multi-stage build for PaymentService EF Core Migration Bundle using .NET 10 Alpine
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for caching restoration
COPY ["Payment.Api/Payment.Api.csproj", "Payment.Api/"]
COPY ["Payment.Application/Payment.Application.csproj", "Payment.Application/"]
COPY ["Payment.Infrastructure/Payment.Infrastructure.csproj", "Payment.Infrastructure/"]
COPY ["Payment.Core/Payment.Core.csproj", "Payment.Core/"]
COPY ["Micro.Shared/Micro.Shared.csproj", "Micro.Shared/"]
RUN dotnet restore "./Payment.Api/Payment.Api.csproj"

# Install dotnet-ef tool
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"
ENV DOTNET_ROLL_FORWARD=Major

# Copy remaining source code
COPY . .
WORKDIR "/src/Payment.Api"

# Generate self-contained EF Core Migration Bundle for Alpine Linux musl x64
RUN dotnet ef migrations bundle \
    --project ../Payment.Infrastructure \
    --startup-project . \
    --context AppDbContext \
    --output /migration/payment-migrate \
    --self-contained \
    -r linux-musl-x64 \
    --configuration Release

# Final lightweight runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
ENV DOTNET_ROLL_FORWARD=Major
WORKDIR /Payment.Api
COPY --from=build /src/Payment.Api/appsettings*.json ./
WORKDIR /migration
COPY --from=build /migration .
COPY --from=build /src/Payment.Api/appsettings*.json ./
ENTRYPOINT ["/migration/payment-migrate"]
