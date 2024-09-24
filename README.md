[![.NET](https://github.com/neozhu/PdfGeneratorApi/actions/workflows/dotnet.yml/badge.svg)](https://github.com/neozhu/PdfGeneratorApi/actions/workflows/dotnet.yml)

# PDF Generator API

This project is a Minimal APIs application built with ASP.NET Core and Playwright. It provides an API to generate PDFs from HTML content or URLs, with additional features such as hiding elements, adding watermarks (text or image), and stamping images onto the generated PDF. The API includes Swagger documentation and supports API Key authentication. It is containerized for easy deployment using Docker.

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Environment Variables](#environment-variables)
- [Running the Application](#running-the-application)
  - [Using .NET CLI](#using-net-cli)
  - [Using Docker](#using-docker)
- [API Documentation](#api-documentation)
  - [Endpoint](#endpoint)
  - [Request Parameters](#request-parameters)
  - [Example Request](#example-request)
- [Usage Examples](#usage-examples)
  - [Generating a PDF from HTML Content](#generating-a-pdf-from-html-content)
  - [Adding a Text Watermark](#adding-a-text-watermark)
  - [Adding an Image Watermark](#adding-an-image-watermark)
  - [Adding a Stamp Image](#adding-a-stamp-image)
- [Security](#security)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Generate PDF**: Create PDFs from HTML content or web pages.
- **Hide Elements**: Specify CSS selectors to hide elements in the PDF.
- **Add Watermarks**:
  - Text or image watermarks.
  - Image watermarks can be uploaded.
- **Add Stamps**:
  - Stamp images can be uploaded.
- **Swagger Integration**: Interactive API documentation and testing.
- **API Key Authentication**: Simple API key authentication using environment variables.
- **Containerization**: Dockerfile included for easy container deployment.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (optional, for container deployment)

### Installation

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/neozhu/PdfGeneratorApi.git
   cd PdfGeneratorApi
   ```

2. **Restore Dependencies**:

   ```bash
   dotnet restore
   ```

## Configuration

Set the `ApiKey` in your `appsettings.json` file to secure your API:

```json
{
  "ApiKey": "your-api-key-here",
  // ... other settings
}
```

Alternatively, you can set the `ApiKey` in the `appsettings.Development.json` file for development purposes:

```json
{
  "ApiKey": "your-api-key-here"
}
```

**Note**: Do not commit sensitive information like API keys to version control repositories. For production environments, consider using environment variables or a secure secrets management solution to store sensitive data.


## Running the Application

### Using .NET CLI

1. **Run the Application**:

   ```bash
   dotnet run
   ```

2. **Access Swagger UI** (in development mode):

   Open your browser and navigate to `http://localhost:5000/swagger` to view the API documentation and interact with the API.

### Using Docker

1. **Build the Docker Image**:

   ```bash
   docker build -t pdfgeneratorapi .
   ```

2. **Run the Docker Container**:

   ```bash
   docker run -d -p 5000:80 -e AapiKey=your-api-key-here pdfgeneratorapi
   ```

3. **Access Swagger UI**:

   Open your browser and navigate to `http://localhost:5000/swagger`.

### Using Docker Compose

You can also run the application using Docker Compose with the following configuration:

```yaml
version: '3.8'

services:
  pdfgeneratorapi:
    image: blazordevlab/pdfgeneratorapi:latest
    container_name: pdfgeneratorapi_container
    ports:
      - "8980:8080"  
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ApiKey=your-api-key-here
    restart: unless-stopped
```

**Steps to Run with Docker Compose**:

1. **Create a `docker-compose.yml` File**:

   Save the above configuration into a file named `docker-compose.yml` in your project directory.

2. **Run Docker Compose**:

   ```bash
   docker-compose up -d
   ```

3. **Access Swagger UI**:

   Open your browser and navigate to `http://localhost:8980/swagger` to interact with the API documentation.

**Note**:

- Replace `your-api-key-here` with your actual API key.
- Ensure that the port mapping in the `ports` section matches your desired host and container ports.
- The `restart: unless-stopped` policy ensures that the container restarts automatically unless you stop it manually.

## API Documentation

### Endpoint

- **POST** `/generate-pdf`

### Request Parameters

- **Url** (`string`, optional): The web page URL to convert to PDF.
- **HtmlContent** (`string`, optional): The HTML content to convert to PDF.
- **HideSelectors** (`string`, optional): CSS selectors of elements to hide.
- **WatermarkText** (`string`, optional): Text to use as a watermark.
- **WatermarkImageFile** (`IFormFile`, optional): Image file to use as a watermark.
- **StampImageFile** (`IFormFile`, optional): Stamp image file.

### Example Request

```bash
curl -X POST http://localhost:5000/generate-pdf \
     -H "X-API-KEY: your-api-key-here" \
     -F "HtmlContent=<h1>Hello, World!</h1>" \
     -F "WatermarkText=Confidential" \
     -F "WatermarkPosition=Center" \
     -o output.pdf
```

## Usage Examples

### Generating a PDF from HTML Content

```bash
curl -X POST http://localhost:5000/generate-pdf \
     -H "X-API-KEY: your-api-key-here" \
     -F "HtmlContent=<p>This is a sample PDF generated from HTML content.</p>" \
     -o sample.pdf
```

### Adding a Text Watermark

```bash
curl -X POST http://localhost:5000/generate-pdf \
     -H "X-API-KEY: your-api-key-here" \
     -F "Url=https://www.example.com" \
     -F "WatermarkText=Confidential" \
     -o confidential.pdf
```

### Adding an Image Watermark

Uploading an image file:

```bash
curl -X POST http://localhost:5000/generate-pdf \
     -H "X-API-KEY: your-api-key-here" \
     -F "HtmlContent=<p>PDF with uploaded image watermark.</p>" \
     -F "WatermarkImageFile=@/path/to/watermark.png" \
     -o uploaded_image_watermark.pdf
```

### Adding a Stamp Image

```bash
curl -X POST http://localhost:5000/generate-pdf \
     -H "X-API-KEY: your-api-key-here" \
     -F "HtmlContent=<p>PDF with stamp image.</p>" \
     -F "StampImageFile=@/path/to/watermark.png" \
     -F "StampPosition=RightBottom" \
     -o stamped.pdf
```

## Security

- **API Key Authentication**: The API requires an `X-API-KEY` header with a valid API key for all requests.
- **Environment Variables**: API keys and other sensitive settings should be stored in environment variables and not in source code or configuration files.

## Contributing

Contributions are welcome! Please follow these steps:

1. **Fork the Repository**
2. **Create a Feature Branch**:

   ```bash
   git checkout -b feature/YourFeature
   ```

3. **Commit Your Changes**:

   ```bash
   git commit -m "Add your feature"
   ```

4. **Push to the Branch**:

   ```bash
   git push origin feature/YourFeature
   ```

5. **Create a Pull Request**

## License

This project is licensed under the [MIT License](LICENSE).

