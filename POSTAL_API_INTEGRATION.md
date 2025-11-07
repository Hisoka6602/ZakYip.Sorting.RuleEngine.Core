# Postal API Integration Summary

## Overview
This implementation adds support for integrating with China Postal services, distinguishing between two types of postal facilities:
1. **邮政分揽投机构** (Postal Collection/Delivery Institution) - Handles parcel collection and delivery operations
2. **邮政处理中心** (Postal Processing Center) - Handles parcel sorting and routing operations

## Architecture

### Domain Layer

#### Entities
- **PostalApiResponse**: Standardized response format for all postal API calls
  - Success status
  - Response code
  - Response message
  - Response data (JSON)
  - Response timestamp

- **PostalParcelData**: Complete parcel information entity
  - Barcode/tracking number
  - Weight (grams)
  - Dimensions (length, width, height in mm)
  - Volume (cubic cm)
  - Sender/recipient addresses
  - Destination code
  - Scan timestamp

#### Interfaces
- **IPostCollectionApiAdapter**: Interface for postal collection/delivery institution APIs
  - UploadWeighingDataAsync: Upload parcel weight and dimension data
  - QueryParcelAsync: Query parcel status and information
  - UploadScanDataAsync: Upload parcel scan events

- **IPostProcessingCenterApiAdapter**: Interface for postal processing center APIs
  - UploadWeighingDataAsync: Upload parcel weight and dimension data
  - QueryParcelRoutingAsync: Query parcel routing information
  - UploadSortingResultAsync: Upload sorting results (destination, chute)
  - UploadScanDataAsync: Upload parcel scan events

### Infrastructure Layer

#### Adapters
Both adapters are HTTP-based and follow the existing pattern used by WCS, WDT WMS, and Jushuituan ERP API clients:

- **PostCollectionApiAdapter**: Implementation for postal collection institution
  - Base endpoint: `/api/post/collection`
  - Endpoints:
    - `/weighing/upload` - Upload weight data
    - `/parcel/query` - Query parcel info
    - `/scan/upload` - Upload scan data

- **PostProcessingCenterApiAdapter**: Implementation for postal processing center
  - Base endpoint: `/api/post/processing`
  - Endpoints:
    - `/weighing/upload` - Upload weight data
    - `/routing/query` - Query routing info
    - `/sorting/result` - Upload sorting result
    - `/scan/upload` - Upload scan data

### Features
- ✅ Complete error handling with try-catch blocks
- ✅ Comprehensive logging (Debug, Information, Warning, Error levels)
- ✅ JSON serialization with camelCase naming
- ✅ Cancellation token support for async operations
- ✅ HTTP status code handling
- ✅ Structured response objects

### Testing
Created 14 comprehensive unit tests covering:
- Success scenarios for all API methods
- Error handling (4xx status codes)
- Exception handling (network errors)
- Response validation

Tests use Moq framework for HTTP client mocking and follow existing test patterns.

## Usage Example

```csharp
// Postal Collection Institution
var collectionAdapter = new PostCollectionApiAdapter(httpClient, logger);

var parcelData = new PostalParcelData
{
    Barcode = "POST123456",
    Weight = 1500.5m,
    Length = 300,
    Width = 200,
    Height = 150,
    Volume = 9000,
    DestinationCode = "SH001"
};

var response = await collectionAdapter.UploadWeighingDataAsync(parcelData);

// Postal Processing Center
var processingAdapter = new PostProcessingCenterApiAdapter(httpClient, logger);

var routingResponse = await processingAdapter.QueryParcelRoutingAsync("POST123456");

var sortingResponse = await processingAdapter.UploadSortingResultAsync(
    barcode: "POST123456",
    destinationCode: "SZ001",
    chuteNumber: "CH05"
);
```

## Integration Points

The adapters can be registered in the DI container alongside existing adapters:

```csharp
services.AddHttpClient<IPostCollectionApiAdapter, PostCollectionApiAdapter>(client =>
{
    client.BaseAddress = new Uri("https://api.postal-collection.example.com");
});

services.AddHttpClient<IPostProcessingCenterApiAdapter, PostProcessingCenterApiAdapter>(client =>
{
    client.BaseAddress = new Uri("https://api.postal-processing.example.com");
});
```

## Future Enhancements (Out of Scope)

The following improvements were noted in code review but are beyond the minimal change scope:
1. **Resilience Patterns**: Add Polly-based retry and circuit breaker policies (similar to HttpThirdPartyAdapter)
2. **Time Provider**: Inject IDateTimeProvider for better testability
3. **Configuration**: Move API endpoints to configuration files
4. **Response Models**: Create strongly-typed response models instead of raw JSON strings

## Files Changed

### Domain
- `Domain/ZakYip.Sorting.RuleEngine.Domain/Constants/ApiConstants.cs` - Added postal API constants
- `Domain/ZakYip.Sorting.RuleEngine.Domain/Entities/PostalApiResponse.cs` - New
- `Domain/ZakYip.Sorting.RuleEngine.Domain/Entities/PostalParcelData.cs` - New
- `Domain/ZakYip.Sorting.RuleEngine.Domain/Interfaces/IPostCollectionApiAdapter.cs` - New
- `Domain/ZakYip.Sorting.RuleEngine.Domain/Interfaces/IPostProcessingCenterApiAdapter.cs` - New

### Infrastructure
- `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Adapters/Post/PostCollectionApiAdapter.cs` - New
- `Infrastructure/ZakYip.Sorting.RuleEngine.Infrastructure/Adapters/Post/PostProcessingCenterApiAdapter.cs` - New

### Tests
- `Tests/ZakYip.Sorting.RuleEngine.Tests/ApiClients/PostCollectionApiAdapterTests.cs` - New (7 tests)
- `Tests/ZakYip.Sorting.RuleEngine.Tests/ApiClients/PostProcessingCenterApiAdapterTests.cs` - New (7 tests)

## Build and Test Results
- ✅ Build: Successful with 0 warnings, 0 errors
- ✅ Tests: All 14 new tests pass
- ✅ Existing Tests: No regression (all existing tests still pass)
