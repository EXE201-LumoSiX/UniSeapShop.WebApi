# MinIO Configuration Troubleshooting Guide

## Root Cause Analysis

The `NullReferenceException` in BlobService was caused by the MinIO SDK failing to properly parse error responses from the MinIO server when attempting to check if the bucket exists. The stack trace revealed:

```
at Minio.MinioClient.ParseErrorNoContent(ResponseResult response)
at Minio.MinioClient.ParseError(ResponseResult response)
at Minio.Handlers.DefaultErrorHandler.Handle(ResponseResult response)
at Minio.RequestExtensions.ExecuteTaskAsync(...)
at Minio.MinioClient.BucketExistsAsync(BucketExistsArgs args, ...)
```

### Root Causes

1. **SSL/TLS Certificate Validation Issues**: The MinIO endpoint is accessed via HTTPS (`https://cdn.fpt-devteam.fun`), and SSL certificate validation might be failing.

2. **Network Connectivity**: The Docker container might not be able to reach the external MinIO endpoint properly.

3. **SDK Error Handling**: When the MinIO SDK encounters a connection error, it attempts to parse the error response but encounters a null reference.

## Solution Implemented

### 1. Enhanced Error Handling
- Added specific catching of `MinioException` to provide better diagnostics
- Improved logging of MinIO-specific errors including response content
- Wrapped MinIO exceptions with more descriptive messages

### 2. SSL Certificate Validation Control
Added a new environment variable `MINIO_INSECURE` to allow skipping SSL certificate validation in development/testing environments:

```yaml
environment:
  - MINIO_USE_SSL=true
  - MINIO_INSECURE=false  # Set to true only for development/testing
```

### 3. Improved Configuration

Updated the BlobService initialization to:
- Properly handle SSL configuration with the new insecure flag
- Provide better logging at each step of the initialization
- Clearly distinguish between API endpoint and Console URL

## Configuration Reference

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MINIO_API_ENDPOINT` | `cdn.fpt-devteam.fun` | MinIO S3-compatible API endpoint (without protocol) |
| `MINIO_CONSOLE_URL` | `minio.fpt-devteam.fun` | MinIO web console URL (for UI integration) |
| `MINIO_ACCESS_KEY` | Required | MinIO access key for authentication |
| `MINIO_SECRET_KEY` | Required | MinIO secret key for authentication |
| `MINIO_USE_SSL` | `true` | Use HTTPS/SSL for the API endpoint |
| `MINIO_INSECURE` | `false` | Skip SSL certificate validation (dev/test only) |

### docker-compose.yml

```yaml
environment:
  - MINIO_API_ENDPOINT=cdn.fpt-devteam.fun
  - MINIO_CONSOLE_URL=minio.fpt-devteam.fun
  - MINIO_ACCESS_KEY=minioadmin
  - MINIO_SECRET_KEY=minioadmin
  - MINIO_USE_SSL=true
  - MINIO_INSECURE=false
```

## Debugging Endpoints

### 1. Configuration Check
```bash
GET /api/blob/list
```
Returns current MinIO configuration and any detected issues.

### 2. Diagnostic Test
```bash
GET /api/blob/debug
```
Attempts to connect to MinIO, create a test file, and returns detailed diagnostic information.

### 3. Health Check
```bash
GET /api/blob/health
```
Quick check if BlobService is properly initialized.

## Troubleshooting Steps

### Issue: NullReferenceException during bucket operations

1. **Check Configuration**
   ```bash
   curl http://localhost:5000/api/blob/list
   ```
   Verify all environment variables are properly set.

2. **Test Connectivity**
   ```bash
   curl http://localhost:5000/api/blob/debug
   ```
   This will attempt an actual MinIO operation and return detailed error information.

3. **SSL Certificate Issues (if using self-signed certificates)**
   
   In docker-compose.yml, temporarily set:
   ```yaml
   - MINIO_INSECURE=true
   ```
   
   **⚠️ WARNING: Only use in development/testing. Never in production.**

4. **Check Credentials**
   
   Verify that `MINIO_ACCESS_KEY` and `MINIO_SECRET_KEY` match your MinIO configuration.

5. **Network Connectivity**
   
   From the Docker container, test connectivity:
   ```bash
   docker exec -it uniseapshop.webapi bash
   ping cdn.fpt-devteam.fun
   curl -I https://cdn.fpt-devteam.fun/
   ```

## Production Recommendations

1. **Use Proper SSL Certificates**: Ensure your MinIO endpoint has valid SSL certificates signed by a trusted CA.

2. **Keep MINIO_INSECURE=false**: Never disable SSL certificate validation in production.

3. **Use Strong Credentials**: Change default `minioadmin` credentials in production.

4. **Monitor Logs**: Watch for MinIO connection errors in application logs.

5. **Health Checks**: Regularly call the `/api/blob/health` endpoint to verify the service is running properly.

## Related Code Changes

- `BlobService.cs`: Enhanced error handling and SSL configuration
- `BlobController.cs`: Added diagnostic endpoints
- `docker-compose.yml`: Added MINIO_INSECURE configuration option
- `IBlobService.cs`: Added GetDiagnosticInfo() method

## For More Information

- MinIO Documentation: https://docs.min.io/
- MinIO .NET SDK: https://github.com/minio/minio-dotnet
