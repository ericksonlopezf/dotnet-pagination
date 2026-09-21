# API Reference: NoSQL Providers (MongoDB, Cosmos DB, LinqToDB, Elasticsearch)

> Microsoft Learn-Style Reference for NoSQL document stores and specialized query providers.

---

## MongoDB: `EricksonLopez.Pagination.MongoDB`

### `MongoCursorPaginationExtensions` (static class)
- **`ToCursorPagedListAsync<TDocument>`**: Executes keyset pagination over `IMongoCollection<TDocument>` or `IMongoQueryable<TDocument>` using BSON filters.
- **`ToCursorPagedResponse<TDocument>`**: Generates `CursorPagedResponse<TDocument>` with Relay page info.

### `MongoObjectIdPaginationExtensions` (static class)
- **Specialization**: Leverages natural 12-byte `ObjectId` timestamp ordering for zero-overhead cursor seek operations without secondary sorting indices.

### `FindFluentPaginationExtensions` (static class)
- Keyset and offset pagination extensions for official `IFindFluent<TDocument, TProjection>` queries.

### `MongoAsyncEnumerableExtensions` (static class)
- Streams query results reactively as `IAsyncEnumerable<TDocument>`.

### `MongoQueryableFilterExtensions` (static class)
- Translates dynamic `FilterParameters` DSL into MongoDB `FilterDefinition<TDocument>` and LINQ expressions.

### `DefaultMongoCursorDecoderRegistry` (static class)
- Out-of-the-box decoders for MongoDB `BsonDocument`, `ObjectId`, and BSON string types.

---

## Azure Cosmos DB: `EricksonLopez.Pagination.Cosmos`

### `CosmosPaginationExtensions` (static class)
- **Continuation Tokens**: Extends Azure Cosmos DB `FeedIterator<T>` to manage pagination using native Cosmos continuation tokens.
- **`ToPagedListAsync<T>`**: Retrieves the next logical page batch and captures the opaque continuation token.

---

## LinqToDB: `EricksonLopez.Pagination.LinqToDB`

### `KeysetBuilder<T>` (sealed class)
- High-performance keyset builder compiling directly to LinqToDB SQL generation pipelines.

### `QueryableLinqToDBExtensions` (static class)
- Extensions for `LinqToDB.ITable<T>` and `IQueryable<T>` (`ToPagedListAsync`, `ApplyFilter`, `ApplySort`).

---

## Elasticsearch: `EricksonLopez.Pagination.Elasticsearch`

### `ElasticsearchCursorPaginationExtensions` (static class)
- **`SearchAfter`**: Integrates with official `Elastic.Clients.Elasticsearch` using the `search_after` cursor parameter for high-throughput pagination over Elasticsearch indices.

### `ElasticsearchCursorHelper` (static class)
- Serializes and deserializes composite sort arrays into URL-safe opaque cursor tokens.
