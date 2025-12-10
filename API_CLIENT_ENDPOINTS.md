# ApiClient Configuration and Testing Endpoints

## Overview
This document describes the new API endpoints added for configuring and testing ApiClient instances (JushuitanErp, WdtWms, WdtErpFlagship).

## Configuration Endpoints

### 1. JushuitanErp Configuration

#### Get Configuration
- **Endpoint**: `GET /api/apiclientconfig/jushuitanerp`
- **Description**: Retrieve current JushuitanErp API client configuration
- **Response**: Returns current configuration with masked secrets (only first 3 and last 3 characters shown)

#### Update Configuration
- **Endpoint**: `PUT /api/apiclientconfig/jushuitanerp`
- **Description**: Update JushuitanErp API client configuration
- **Request Body**:
```json
{
  "url": "https://openapi.jushuitan.com/open/orders/weight/send/upload",
  "timeOut": 5000,
  "appKey": "your_app_key",
  "appSecret": "your_app_secret",
  "accessToken": "your_access_token",
  "version": 2,
  "isUploadWeight": true,
  "type": 1,
  "isUnLid": false,
  "channel": "sorting_system",
  "defaultWeight": -1
}
```

### 2. WdtWms Configuration

#### Get Configuration
- **Endpoint**: `GET /api/apiclientconfig/wdtwms`
- **Description**: Retrieve current WdtWms API client configuration

#### Update Configuration
- **Endpoint**: `PUT /api/apiclientconfig/wdtwms`
- **Description**: Update WdtWms API client configuration
- **Request Body**:
```json
{
  "url": "https://api.wdt.com/endpoint",
  "sid": "your_sid",
  "appKey": "your_app_key",
  "appSecret": "your_app_secret",
  "method": "wms.logistics.Consign.weigh",
  "timeOut": 5000,
  "mustIncludeBoxBarcode": false,
  "defaultWeight": 0.0
}
```

### 3. WdtErpFlagship Configuration

#### Get Configuration
- **Endpoint**: `GET /api/apiclientconfig/wdterpflagship`
- **Description**: Retrieve current WdtErpFlagship API client configuration

#### Update Configuration
- **Endpoint**: `PUT /api/apiclientconfig/wdterpflagship`
- **Description**: Update WdtErpFlagship API client configuration
- **Request Body**:
```json
{
  "url": "https://api.wdt.com/flagship/endpoint",
  "key": "your_key",
  "appsecret": "your_appsecret",
  "sid": "your_sid",
  "method": "wms.stockout.Sales.weighingExt",
  "v": "1.0",
  "salt": "your_salt",
  "packagerId": 12345,
  "packagerNo": "PKG001",
  "operateTableName": "table_name",
  "force": false,
  "timeOut": 5000
}
```

## Test Endpoints

### 1. Test JushuitanErp API
- **Endpoint**: `POST /api/apiclienttest/jushuitanerp`
- **Description**: Test JushuitanErp API with sample data
- **Request Body**:
```json
{
  "barcode": "TEST123456789",
  "weight": 1250,
  "length": 30,
  "width": 20,
  "height": 10
}
```
- **Response**: Returns detailed test result including request/response data, timing, and formatted CURL command
- **Logging**: Test requests are automatically logged to the ApiRequestLog table

### 2. Test WdtWms API
- **Endpoint**: `POST /api/apiclienttest/wdtwms`
- **Description**: Test WdtWms API with sample data
- **Request Body**: Same as JushuitanErp test

### 3. Test WdtErpFlagship API
- **Endpoint**: `POST /api/apiclienttest/wdterpflagship`
- **Description**: Test WdtErpFlagship API with sample data
- **Request Body**: Same as JushuitanErp test

## Test Response Format
```json
{
  "success": true,
  "code": "200",
  "message": "Request successful",
  "data": "{...}",
  "parcelId": "TEST123456789",
  "requestUrl": "https://api.example.com/endpoint",
  "requestBody": "{...}",
  "responseBody": "{...}",
  "errorMessage": null,
  "requestTime": "2025-11-08T10:00:00Z",
  "responseTime": "2025-11-08T10:00:01Z",
  "durationMs": 234,
  "responseStatusCode": 200,
  "formattedCurl": "curl -X POST ..."
}
```

## Security Notes
- Sensitive configuration data (keys, secrets, tokens) are masked in GET responses
- Test requests are logged with full request/response data for debugging
- No authentication is implemented on these endpoints - consider adding authorization if needed

## Usage Examples

### Using curl to update configuration:
```bash
curl -X PUT "https://your-api/api/apiclientconfig/jushuitanerp" \
  -H "Content-Type: application/json" \
  -d '{
    "appKey": "your_app_key",
    "appSecret": "your_app_secret",
    "accessToken": "your_token"
  }'
```

### Using curl to test API:
```bash
curl -X POST "https://your-api/api/apiclienttest/jushuitanerp" \
  -H "Content-Type: application/json" \
  -d '{
    "barcode": "TEST123456789",
    "weight": 1250
  }'
```

## Implementation Details
- Configuration changes are applied immediately in-memory
- No persistent storage for configuration - changes are lost on application restart
- Consider implementing persistent configuration storage for production use
- Test endpoints use the actual API clients to make real HTTP calls
- All test activity is logged to the ApiRequestLog table with detailed information
