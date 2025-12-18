using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB仓储基类 - 提供通用CRUD操作，避免代码重复
/// LiteDB repository base class - Provides common CRUD operations to avoid code duplication
/// </summary>
/// <typeparam name="TEntity">实体类型 / Entity type</typeparam>
/// <typeparam name="TKey">主键类型（string或long） / Primary key type (string or long)</typeparam>
public abstract class BaseLiteDbRepository<TEntity, TKey> where TEntity : class
{
    protected readonly ILiteDatabase Database;
    protected readonly string CollectionName;
    protected static readonly ISystemClock Clock = new SystemClock();

    protected BaseLiteDbRepository(ILiteDatabase database, string collectionName)
    {
        Database = database;
        CollectionName = collectionName;
        ConfigureIdMapping();
        EnsureIndexes();
    }

    /// <summary>
    /// 配置ID映射 - 由子类实现以指定LiteDB的主键字段
    /// Configure ID mapping - implemented by subclass to specify LiteDB primary key field
    /// </summary>
    protected virtual void ConfigureIdMapping()
    {
        // 默认不做任何配置，子类可以覆盖此方法来配置ID映射
        // Default: no configuration, subclasses can override to configure ID mapping
    }

    /// <summary>
    /// 确保索引创建 - 由子类实现
    /// Ensure indexes are created - implemented by subclass
    /// </summary>
    protected abstract void EnsureIndexes();

    /// <summary>
    /// 获取实体的主键值 - 由子类实现
    /// Get primary key value of entity - implemented by subclass
    /// </summary>
    protected abstract TKey GetEntityId(TEntity entity);

    /// <summary>
    /// 更新实体的时间戳 - 由子类实现
    /// Update entity timestamp - implemented by subclass
    /// </summary>
    protected abstract TEntity UpdateTimestamp(TEntity entity);

    public virtual Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var entities = collection.FindAll().ToList();
        return Task.FromResult<IEnumerable<TEntity>>(entities);
    }

    public virtual Task<TEntity?> GetByIdAsync(TKey id)
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var bsonValue = id is string strId ? new BsonValue(strId) : new BsonValue(id);
        var entity = collection.FindById(bsonValue);
        return Task.FromResult(entity);
    }

    public virtual Task<bool> AddAsync(TEntity entity)
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var result = collection.Insert(entity);
        return Task.FromResult(result != null);
    }

    public virtual Task<bool> UpdateAsync(TEntity entity)
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var updatedEntity = UpdateTimestamp(entity);
        var result = collection.Update(updatedEntity);
        return Task.FromResult(result);
    }

    public virtual Task<bool> DeleteAsync(TKey id)
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var bsonValue = id is string strId ? new BsonValue(strId) : new BsonValue(id);
        var result = collection.Delete(bsonValue);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 插入或更新实体（Upsert）- 原子操作，避免竞态条件
    /// Insert or update entity (Upsert) - Atomic operation to avoid race conditions
    /// </summary>
    public virtual Task<bool> UpsertAsync(TEntity entity)
    {
        var collection = Database.GetCollection<TEntity>(CollectionName);
        var updatedEntity = UpdateTimestamp(entity);
        var id = GetEntityId(updatedEntity);
        var bsonValue = id is string strId ? new BsonValue(strId) : new BsonValue(id);
        var result = collection.Upsert(bsonValue, updatedEntity);
        return Task.FromResult(result);
    }

    protected ILiteCollection<TEntity> GetCollection()
    {
        return Database.GetCollection<TEntity>(CollectionName);
    }
}
