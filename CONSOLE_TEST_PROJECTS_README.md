# WCS API Console Test Projects

This directory contains console test projects for testing WCS (Warehouse Control System) API integrations. Each project demonstrates how to use the respective API client with code-based configuration.

## Projects

### 1. ZakYip.Sorting.RuleEngine.WcsApiClient.ConsoleTest
Tests the generic WCS API client implementation.

**Configuration Constants**:
```csharp
private const string BASE_URL = "https://api.example.com";
private const int TIMEOUT_SECONDS = 30;
private const string API_KEY = "your-api-key-here";
```

**Usage**:
```bash
cd ZakYip.Sorting.RuleEngine.WcsApiClient.ConsoleTest
# Edit Program.cs to set your configuration
dotnet run
```

### 2. ZakYip.Sorting.RuleEngine.WdtWmsApiClient.ConsoleTest
Tests the Wang Dian Tong (WDT) WMS API client.

**Configuration Constants**:
```csharp
private const string BASE_URL = "https://api.wdt.com";
private const string APP_KEY = "your-app-key";
private const string APP_SECRET = "your-app-secret";
private const int TIMEOUT_SECONDS = 30;
```

**Usage**:
```bash
cd ZakYip.Sorting.RuleEngine.WdtWmsApiClient.ConsoleTest
# Edit Program.cs to set your WDT credentials
dotnet run
```

### 3. ZakYip.Sorting.RuleEngine.JushuitanErpApiClient.ConsoleTest
Tests the Jushuituan ERP API client.

**Configuration Constants**:
```csharp
private const string BASE_URL = "https://api.jushuitan.com";
private const string PARTNER_KEY = "your-partner-key";
private const string PARTNER_SECRET = "your-partner-secret";
private const string TOKEN = "your-token";
private const int TIMEOUT_SECONDS = 30;
```

**Usage**:
```bash
cd ZakYip.Sorting.RuleEngine.JushuitanErpApiClient.ConsoleTest
# Edit Program.cs to set your Jushuituan credentials
dotnet run
```

## Why Console Test Projects?

1. **Quick Testing**: Test API integrations without running the full application
2. **Code-Based Configuration**: No need for appsettings.json or external configuration files
3. **Debugging**: Easy to debug API calls and responses
4. **Documentation**: Serves as working examples of how to use each API client
5. **Isolation**: Test each integration independently

## Common Operations Tested

All console test projects demonstrate:
- **Scan Parcel**: Register a parcel barcode in the WCS
- **Upload Data**: Send parcel weight and dimensions
- **Request Chute**: Get a chute/gate assignment for sorting
- **Upload Image**: Send parcel images (where supported)

## Configuration Best Practices

### For Development/Testing:
1. Edit the constants directly in `Program.cs`
2. Use test endpoints and credentials
3. Run the console app to verify connectivity

### For Production:
Do NOT use these console projects in production. Instead:
1. Use the main service with API-based configuration
2. Store credentials securely (Azure Key Vault, environment variables, etc.)
3. Use the WcsApiConfigController to manage configurations via API

## Example Output

```
=== WCS API Console Test ===

URL: https://api.example.com

Scan: True - Parcel scanned successfully

Test completed. Press any key...
```

## Troubleshooting

### Connection Errors
- Verify the BASE_URL is correct
- Check network connectivity
- Ensure firewall allows outbound HTTPS

### Authentication Errors
- Verify API_KEY/credentials are correct
- Check if credentials have expired
- Confirm account has necessary permissions

### Timeout Errors
- Increase TIMEOUT_SECONDS if network is slow
- Check if the API endpoint is responsive
- Verify the API is not rate-limiting your requests

## Adding New Tests

To add a new test scenario:

```csharp
static async Task TestYourScenario(WcsApiClient client)
{
    Console.WriteLine("--- Test: Your Scenario ---");
    try
    {
        // Your test code here
        var result = await client.SomeMethodAsync(...);
        Console.WriteLine($"Result: {result.Success}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    Console.WriteLine();
}
```

Then call it from `Main`:
```csharp
await TestYourScenario(client);
```

## Integration with Main Application

The console test projects use the same API client classes as the main application:
- `Infrastructure.ApiClients.WcsApiClient`
- `Infrastructure.ApiClients.WdtWmsApiClient`
- `Infrastructure.ApiClients.JushuitanErpApiClient`

This ensures:
- **Consistency**: Same code paths as production
- **Reliability**: Tests verify actual implementation
- **Maintainability**: One codebase to maintain

## Security Notes

⚠️ **IMPORTANT**:
- Never commit real API keys/credentials to source control
- Use placeholder values in the repository
- Keep actual credentials in secure storage
- Rotate credentials regularly
- Use different credentials for development and production

## Next Steps

After successful console testing:
1. Configure the production WCS API via the API endpoints
2. Test in the full application environment
3. Monitor API performance and reliability
4. Set up proper error handling and logging
5. Implement retry policies for transient failures

## Support

For issues or questions:
1. Check the main documentation in `/IMPLEMENTATION_SUMMARY_WCS.md`
2. Review API client source code in `/Infrastructure/ApiClients/`
3. Check Swagger documentation when running the main service
4. Review logs for detailed error messages
