# This Dockerfile sets up a .NET application environment with Playwright dependencies.
# 
# Stages:
# 1. Base: Uses the .NET 9.0 SDK image and installs necessary Playwright dependencies.
# 2. Build: Copies the project file, restores dependencies, and builds the project.
# 3. Publish: Publishes the application in Release mode.
# 4. Final: Sets up the final runtime environment, installs Playwright CLI and its dependencies, and sets the entry point.
# 
# Instructions:
# - The base stage installs system dependencies required by Playwright.
# - The build stage restores and builds the .NET project.
# - The publish stage publishes the .NET project.
# - The final stage sets up the runtime environment, installs Playwright CLI, and sets the entry point for the application.
# 
# Optional:
# - You can set environment variables like API_KEY if needed.

FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS base
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2 libpangocairo-1.0-0 \
    libgtk-3-0 libxshmfence1

FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS build
WORKDIR /src
COPY ["PdfGeneratorApi.csproj", "./"]
RUN dotnet restore "./PdfGeneratorApi.csproj"
COPY . .
RUN dotnet build "PdfGeneratorApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PdfGeneratorApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the project file into the final image
COPY ["PdfGeneratorApi.csproj", "./"]

RUN dotnet tool install --global Microsoft.Playwright.CLI
RUN /root/.dotnet/tools/playwright install

# Set an environment variable (optional)
# ENV API_KEY=your-api-key-here

ENTRYPOINT ["dotnet", "PdfGeneratorApi.dll"]
