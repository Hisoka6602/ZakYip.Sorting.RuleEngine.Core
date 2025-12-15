using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB仓储基类 - 提供通用CRUD操作，避免代码重复
/// LiteDB repository base class - Provides common CRUD operations to avoid code duplication
/// </summary>
/// <typeparam name="TEntity">实体类型 / Entity type</typeparam>
/// <typeparam name="TKey">主键类型 / Primary key type</typeparam>
public abstract class BaseLiteDbRepository<TEntity, TKey> where TEntity : class
{
    protected readonly ILiteDatabase Database;
    protected readonly string CollectionName;
    protected static readonly ISystemClock Clock = new SystemClock();

    protected BaseLiteDbRepository(ILiteDatabase database, string collectionName)
    {
        Database = database;
        CollectionName = collectionName;
        EnsureIndexes();
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
        var entity = collection.FindById(new BsonValue(id));
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
        var result = collection.Delete(new BsonValue(id));
        return Task.FromResult(result);
    }

    protected ILiteCollection<TEntity> GetCollection()
    {
        return Database.GetCollection<TEntity>(CollectionName);
    }
}
