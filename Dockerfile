# 基础镜像
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS base
WORKDIR /app

# 安装 Playwright 依赖
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

# 安装 Playwright 浏览器
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN playwright install --with-deps chromium

# 设置环境变量（可选）
# ENV API_KEY=your-api-key-here

ENTRYPOINT ["dotnet", "PdfGeneratorApi.dll"]
