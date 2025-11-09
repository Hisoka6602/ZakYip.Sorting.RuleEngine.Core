using ZakYip.Sorting.RuleEngine.DataSimulator.Configuration;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.DataSimulator.Generators;

/// <summary>
/// 数据生成器
/// Data generator
/// </summary>
public class DataGenerator
{
    private readonly DataGenerationConfig _config;
    private readonly Random _random;
    private int _parcelCounter;

    public DataGenerator(DataGenerationConfig config)
    {
        _config = config;
        _random = new Random();
        _parcelCounter = 0;
    }

    /// <summary>
    /// 生成包裹信息
    /// Generate parcel information
    /// </summary>
    public ParcelData GenerateParcel()
    {
        var id = Interlocked.Increment(ref _parcelCounter);
        return new ParcelData
        {
            ParcelId = $"PKG{DateTime.Now:yyyyMMddHHmmss}{id:D6}",
            CartNumber = $"CART{id % 100:D3}",
            Barcode = $"BC{DateTime.Now.Ticks}{id:D6}",
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// 生成DWS数据
    /// Generate DWS data
    /// </summary>
    public DwsData GenerateDwsData(string? barcode = null)
    {
        var weight = _random.Next(_config.WeightMin, _config.WeightMax);
        var length = _random.Next(_config.LengthMin, _config.LengthMax);
        var width = _random.Next(_config.WidthMin, _config.WidthMax);
        var height = _random.Next(_config.HeightMin, _config.HeightMax);
        var volume = (decimal)(length * width * height) / 1000; // Convert to cubic centimeters

        return new DwsData
        {
            Barcode = barcode ?? $"BC{DateTime.Now.Ticks}{_random.Next(1000, 9999)}",
            Weight = weight,
            Length = length,
            Width = width,
            Height = height,
            Volume = volume,
            ScannedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 生成完整的包裹和DWS数据对
    /// Generate complete parcel and DWS data pair
    /// </summary>
    public (ParcelData Parcel, DwsData Dws) GenerateCompletePair()
    {
        var parcel = GenerateParcel();
        var dws = GenerateDwsData(parcel.Barcode);
        return (parcel, dws);
    }

    /// <summary>
    /// 批量生成包裹数据
    /// Generate batch of parcel data
    /// </summary>
    public List<ParcelData> GenerateParcels(int count)
    {
        var parcels = new List<ParcelData>(count);
        for (int i = 0; i < count; i++)
        {
            parcels.Add(GenerateParcel());
        }
        return parcels;
    }

    /// <summary>
    /// 批量生成DWS数据
    /// Generate batch of DWS data
    /// </summary>
    public List<DwsData> GenerateDwsDataBatch(int count)
    {
        var dwsList = new List<DwsData>(count);
        for (int i = 0; i < count; i++)
        {
            dwsList.Add(GenerateDwsData());
        }
        return dwsList;
    }

    /// <summary>
    /// 批量生成完整数据对
    /// Generate batch of complete data pairs
    /// </summary>
    public List<(ParcelData Parcel, DwsData Dws)> GenerateCompletePairs(int count)
    {
        var pairs = new List<(ParcelData, DwsData)>(count);
        for (int i = 0; i < count; i++)
        {
            pairs.Add(GenerateCompletePair());
        }
        return pairs;
    }
}

/// <summary>
/// 包裹数据
/// Parcel data
/// </summary>
public class ParcelData
{
    public string ParcelId { get; set; } = string.Empty;
    public string CartNumber { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
