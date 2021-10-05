---
post_title: 'Accelerate to eighty eight: SQL Server temporal tables in EF Core 6.0'
username: jeremy-likness
microsoft_alias: jeliknes
TODO: featured_image: planetarydocs.jpg
categories: .NET Core, Azure, Entity Framework, ASP.NET, SQL Server
summary: TODO
TODO: desired_publication_date: '2021-09-08'
---

[EF Core 6.0 Release Candidate 1](https://docs.microsoft.com/ef/core/what-is-new/ef-core-6.0/plan) was [released to NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/6.0.0-rc.1.21452.10) a couple of weeks ago. This release is the first of two “go live” release candidates that are supported in production. Use them with the [release candidates for .NET 6.0](https://devblogs.microsoft.com/dotnet/announcing-net-6-release-candidate-1/), which also have "go live" licenses.

## SQL Server Temporal Tables

Before EF Core 6.0, the most requested unimplemented feature was [support for SQL Server temporal tables](https://github.com/dotnet/efcore/issues/4693). With EF Core 6.0, that support is now here! 

[SQL Server temporal tables](https://docs.microsoft.com/sql/relational-databases/tables/temporal-tables?view=sql-server-ver15) automatically keep track of all the data ever stored in a table, even after that data has been updated or deleted. This is achieved by creating a parallel "history table" into which timestamped historical data is stored whenever a change is made to the main table. This allows historical data to be queried, such as for auditing, or restored, such as for recovery after accidental mutation or deletion.

EF Core now supports:

* The creation of temporal tables using EF Core migrations
* Transformation of existing tables into temporal tables, again using migrations
* Querying historical data
* Restoring data from some point in the past





I built a reference app that uses [Azure Cosmos DB with EF Core on Blazor Server](https://blog.jeremylikness.com/blog/azure-cosmos-db-with-ef-core-on-blazor-server/). It includes search capability, cross-referenced entities and an interface to create, read, and update. I recently upgraded to the latest EF Core 6.0 version and was able to simplify and remove quite a bit of code!

![Screenshot of Planetary Docs](planetarydocs.jpg)

## Feature overview

Here are some of the features requested that we added to the EF Core 6.0 Azure Cosmos DB provider.

### Implicit ownership

EF Core was built as an object _relational_ mapper. In relational databases, complex relationships are expressed by storing related entities in separate tables and referencing them with foreign keys. EF Core assumes non-primitive entity types encountered in a parent are expressed as foreign key relationships. The relationships are configured using `HasMany` or `HasOne` and the instances are assumed to exist independently with a configured relationship. In document databases, the default behavior for entity types is to assume they are embedded documents owned by the parent. In other words, the complex type's data exists wholly within the context of the parent. In previous versions of EF Core, this behavior had to be configured explicitly for it to work with the Azure Cosmos DB provider. In EF Core 6.0, ownership is implicit. This saves configuration and ensures the behavior is consistent with NoSQL approaches from other providers.

For example, in [Planetary Docs](https://github.com/JeremyLikness/PlanetaryDocs) there are authors and tags. The entities "own" a list of summaries that point to the URL and titles of related documents. This way, when a user asks "What documents have tag X" I only need one document loaded to answer the question (I load tag X, then iterate it's owned collection of titles). Using EF Core 5, I had to explicitly claim ownership:

```csharp
tagModel.OwnsMany(t => t.Documents);
authorModel.OwnsMany(t => t.Documentts);
```

In EF Core 6, the ownership is implicit so there is no need to configure the entities except to specify partition keys.

### Support for primitive collections

In relational databases, primitive collections are often modeled by either promoting them to complex types or converting them to a serialized artifact to store in a single column. Consider a blog post that can have a list of tags. One common approach would be to create an entity that represents a tag:

```csharp
public class Tag 
{
    public int Id { get; set; }
    public string Text { get; set; }
}
```

The tag is then referenced:

```csharp
public ICollection<Tag> Tags { get; set; }
```

The primitive is promoted to a complex type and stored in a separate table. An alternative is to collapse the tags into a single field that contains a comma-delimited list. This approach requires a value converter to marshal the list into the field for updates and decompose the field into the list for read. It also makes it difficult and expensive to answer questions like, "How many posts are tagged X?" Using EF Core 5, I chose the single column approach. I serialized the list to JSON when writing and deserialized when reading. This is the serialization code:

```csharp
private static string ToJson<T>(T item) => JsonSerializer.Serialize(item);
private static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json);
```

I configured EF Core to make the conversions:

```csharp
docModel.Property(d => d.Tags)
    .HasConversion(
        t => ToJson(t),
        t => FromJson<List<string>>(t));
```

And the resulting document looked like this:

```json
{
    "tags" : "[\"one\", \"two\", \"three\"]"
}
```

With EF Core 6.0, I simply deleted the code to take advantage of the built-in handling of primitive types. This results in a document like this:

```json
{
    "tags" : [ 
        "one",
        "two",
        "three"
    ]
}
```

This results in a schema change that Azure Cosmos DB has no problem handling. The C# code, on the other hand, will throw when a current model using tags as an array encounters a legacy record that used tags as a field. How do we handle this when EF Core doesn't have the concept of NoSQL migrations?

### Raw SQL

A popular request is to allow developers to write their own SQL for data access. This is exactly the feature I needed to handle my code migration. For the raw SQL to work, it must project to an existing model. It is an extension of the `DbSet<T>` for the entity. In my case, it enabled an in-place migration. After updating the code, attempting to load a document would fail. The document had a single string property for "tag" but the C# model is an array, so the JSON serializer would throw an exception. To remedy this, I used a built-in feature of Azure Cosmos DB that will [parse a string into an array](https://docs.microsoft.com/azure/cosmos-db/sql/sql-query-stringtoarray). Using a query, I project the entity to a document that matches the current schema and then save it back. This is the migration code:

```csharp
var docs = await Documents.FromSqlRaw(
    "select c.id, c.Uid, c.AuthorAlias, c.Description, c.Html, c.Markdown, c.PublishDate, c.Title, STRINGTOARRAY(c.Tags) as Tags from c").ToListAsync();
foreach (var doc in docs)
{
    Entry(doc).State = EntityState.Modified;
}
```

This feature empowers developers to craft complex queries that may not be supported by the LINQ provider.

### Additional enhancements

In addition to what I already covered, these enhancements also made it in.

- For many-to-many relationships, EF Core now [implicitly uses the partition key on the join type](https://github.com/dotnet/efcore/issues/23491).
- You are able to [configure time-to-live (TTL)](https://github.com/dotnet/efcore/issues/17307) for documents at the instance, type, and collection levels.
- You can [configure container facets](https://github.com/dotnet/efcore/issues/17301) such as throughput, size, etc. through EF Core APIs.
- We now [log diagnostic events](https://github.com/dotnet/efcore/issues/17298) specific to Cosmos DB including query cost.
- We [added support](https://github.com/dotnet/efcore/issues/16144) for the `DISTINCT` operator in queries.
- The LINQ provider now [translates certain methods](https://github.com/dotnet/efcore/issues/16143) such as string manipulation and mathematical operators to their native Cosmos DB counterparts.

## Summary

I'm excited about the changes coming and hope that you are, too. Are you using the Cosmos DB provider? Are you considering it now that we've added these features? Is there something critical you need that we missed? Let me know in the comments below. Thank you!
