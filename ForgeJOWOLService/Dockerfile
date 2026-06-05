# --- STAGE 1: Build & Compile Native AOT ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install native compilation dependencies required for AOT linking
RUN apt-get update && apt-get install -y --no-install-recommends \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy the entire workspace into the builder stage first
COPY . .

# Run restore and publish together so the assets file captures linux-x64 targets
RUN dotnet publish "ForgeJOWOLService.csproj" \
    -c Release \
    -r linux-x64 \
    -o /app \
    /p:PublishAot=true

# --- STAGE 2: Ultra-Minimal Runtime Wrapper ---
FROM gcr.io/distroless/cc-debian12:latest
WORKDIR /app

COPY --from=build /app/ForgeJOWOLService .
COPY appsettings.json .

ENTRYPOINT ["./ForgeJOWOLService"]