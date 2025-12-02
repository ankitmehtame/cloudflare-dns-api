[![Build](https://github.com/ankitmehtame/cloudflare-dns-api/actions/workflows/docker-publish.yml/badge.svg?branch=main)](https://github.com/ankitmehtame/cloudflare-dns-api/actions/workflows/docker-publish.yml)
![GHCR Image Version (latest)](https://ghcr-badge.egpl.dev/ankitmehtame/cloudflare-dns-api/latest_tag?color=%2344cc11&ignore=&label=version&trim=)

# Cloudflare DNS API Wrapper

A simple REST API that acts as a wrapper for the Cloudflare DNS API to update `A` or `CNAME` DNS records.

## Features

- Update `A` or `CNAME` DNS records.
- Retrieve the content of a specific DNS record.
- Swagger UI for interactive API documentation.
- Containerized deployment with Docker.

## Configuration

To run this application, you need to configure the following environment variables:

- `CLOUDFLARE_API_TOKEN`: Your Cloudflare API token with DNS edit permissions.
- `CLOUDFLARE_ZONE_ID`: The Zone ID of the DNS zone you want to manage.

## API Endpoints

The API provides the following endpoints:

### Update DNS Record

- **POST** `/api/dns`

Updates an existing DNS record.

**Request Body:**

```json
{
  "name": "subdomain.yourdomain.com",
  "type": "A",
  "content": "192.0.2.1"
}
```

Supported `type` values are `A` and `CNAME`.

### Get DNS Record

- **GET** `/api/dns/{name}`

Retrieves the content of a specific DNS record by its fully qualified domain name.

**Example:**

`GET /api/dns/subdomain.yourdomain.com`

**Success Response:**

```
"192.0.2.1"
```

## Build and Run

### Local

1.  **Build the application:**
    ```sh
    dotnet build
    ```

2.  **Run the application:**
    ```sh
    dotnet run --project CloudflareDnsApi/CloudflareDnsApi.csproj
    ```

The API will be available at `http://localhost:5000` (or the configured port). The Swagger UI is available at the root URL.

### Docker

1.  **Build the Docker image:**
    ```sh
    docker build -t cloudflare-dns-api .
    ```

2.  **Run the Docker container:**
    ```sh
    docker run -d \
      -p 8080:8080 \
      -e CLOUDFLARE_API_TOKEN="your_api_token" \
      -e CLOUDFLARE_ZONE_ID="your_zone_id" \
      cloudflare-dns-api
    ```

The API will be accessible at `http://localhost:8080`.

## Testing

The project uses xUnit for testing. To run the tests, execute the following command:

```sh
dotnet test
```
