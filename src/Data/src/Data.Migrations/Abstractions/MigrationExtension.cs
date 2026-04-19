using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Model;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

//See https://github.com/fluentmigrator/fluentmigrator/issues/640

/// <summary>
/// The base migration class for custom SQL queries and data updates/deletions, extended for custom functionality.
/// </summary>
public abstract class MigrationExtension : Migration
{
    /// <summary>
    /// Gets the starting point for merging database rows.
    /// </summary>
    public IMergeExpressionRoot Merge
    {
        get
        {
            //Extract the IMigrationContext from the base type through reflection
            var context = (IMigrationContext)typeof(MigrationBase)
                .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(this)!;

            return new MergeExpressionRoot(context);
        }
    }
}

/// <summary>
/// The root expression for a MERGE operation.
/// </summary>
public interface IMergeExpressionRoot
{
    /// <summary>
    /// Merge data into a table, inserting it if it does not exist, or updating it if it does exist.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The expression builder.</returns>
    IMergeDataOrInSchemaSyntax IntoTable(string tableName);
}

/// <summary>
/// The root expression for a MERGE operation.
/// </summary>
internal class MergeExpressionRoot : IMergeExpressionRoot
{
    /// <summary>
    /// The migration context.
    /// </summary>
    private readonly IMigrationContext _context;

    /// <summary>
    /// Construct the builder.
    /// </summary>
    /// <param name="context">The migration context.</param>
    public MergeExpressionRoot(IMigrationContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public IMergeDataOrInSchemaSyntax IntoTable(string tableName)
    {
        var expression = new MergeDataExpression { TableName = tableName };
        _context.Expressions.Add(expression);
        return new MergeDataExpressionStartBuilder(expression);
    }
}

/// <summary>
/// The expression for a MERGE operation, after a table and schema have been selected.
/// </summary>
public interface IMergeDataSyntax
{
    /// <summary>
    /// Adds a new row to insert or update.
    /// </summary>
    /// <typeparam name="T">The anonymous data type.</typeparam>
    /// <param name="dataAsAnonymousType">The anonymous data being merged.</param>
    /// <returns>The expression builder.</returns>
    IMergeDataOrMatchSyntax<T> Row<T>(T dataAsAnonymousType);
}

/// <summary>
/// The expression for a MERGE operation, after a table has been selected.
/// </summary>
public interface IMergeDataOrInSchemaSyntax : IMergeDataSyntax
{
    /// <summary>
    /// Indicates the schema of the target table.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <returns>The expression builder.</returns>
    IMergeDataSyntax InSchema(string schemaName);
}

/// <summary>
/// The expression for a MERGE operation, after some data has been added.
/// </summary>
/// <typeparam name="T">The already-defined anonymous data type.</typeparam>
public interface IMergeDataOrMatchSyntax<T>
{
    /// <summary>
    /// Adds a new row to insert or update.
    /// </summary>
    /// <param name="dataAsAnonymousType">The anonymous data being merged.</param>
    /// <returns>The expression builder.</returns>
    IMergeDataOrMatchSyntax<T> Row(T dataAsAnonymousType);

    /// <summary>
    /// Indicates the key by which the row will be merged.
    /// </summary>
    /// <typeparam name="M">The key of the inserted type.</typeparam>
    /// <param name="f">The key selector expression.</param>
    void Match<M>(Func<T, M> f);
}

/// <summary>
/// The base class upon which merge statements build.
/// </summary>
internal abstract class MergeDataExpressionBuilderBase : ISupportAdditionalFeatures
{
    /// <summary>
    /// The merge expression.
    /// </summary>
    protected readonly MergeDataExpression _expression;

    /// <inheritdoc/>
    public IDictionary<string, object> AdditionalFeatures => _expression.AdditionalFeatures;

    /// <summary>
    /// Construct the builder.
    /// </summary>
    /// <param name="expression">The merge expression.</param>
    protected MergeDataExpressionBuilderBase(MergeDataExpression expression)
    {
        _expression = expression;
    }

    /// <summary>
    /// Extract property-value pairs from the provided anonymous data.
    /// </summary>
    /// <param name="dataAsAnonymousType">The anonymous data.</param>
    /// <returns>The property-value pairs.</returns>
    protected static IDictionary<string, object?> ExtractData(object dataAsAnonymousType)
    {
        var data = new Dictionary<string, object?>();

        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(dataAsAnonymousType);

        foreach (PropertyDescriptor property in properties)
        {
            data.Add(property.Name, property.GetValue(dataAsAnonymousType));
        }

        return data;
    }
}

/// <summary>
/// A merge that has not yet defined its data structure.
/// </summary>
internal class MergeDataExpressionStartBuilder : MergeDataExpressionBuilderBase, IMergeDataOrInSchemaSyntax
{
    /// <summary>
    /// Construct the builder.
    /// </summary>
    /// <param name="expression">The merge expression.</param>
    public MergeDataExpressionStartBuilder(MergeDataExpression expression) : base(expression)
    {
    }

    /// <inheritdoc/>
    public IMergeDataOrMatchSyntax<T> Row<T>(T dataAsAnonymousType)
    {
        //We now know the type of the anonymous data
        var typed = new MergeDataExpressionTypedBuilder<T>(_expression);
        return typed.Row(dataAsAnonymousType);
    }

    /// <inheritdoc/>
    public IMergeDataSyntax InSchema(string schemaName)
    {
        _expression.SchemaName = schemaName;
        return this;
    }
}

/// <summary>
/// A merge that has defined its anonymous data structure.
/// </summary>
/// <typeparam name="T">The anonymous data structure.</typeparam>
internal class MergeDataExpressionTypedBuilder<T> : MergeDataExpressionBuilderBase, IMergeDataOrMatchSyntax<T>
{
    /// <summary>
    /// Construct the builder.
    /// </summary>
    /// <param name="expression">The merge expression.</param>
    public MergeDataExpressionTypedBuilder(MergeDataExpression expression) : base(expression)
    {
    }

    /// <inheritdoc/>
    public IMergeDataOrMatchSyntax<T> Row(T dataAsAnonymousType)
    {
        if (dataAsAnonymousType is null)
            throw new ArgumentNullException(nameof(dataAsAnonymousType));

        //Pull the properties off the anonymous object
        IDictionary<string, object?> data = ExtractData(dataAsAnonymousType);

        //Define an insertion and set its properties
        var dataDefinition = new InsertionDataDefinition();

        dataDefinition.AddRange(data);

        //Add this insertion to the merge
        _expression.Rows.Add(dataDefinition);

        return this;
    }

    /// <inheritdoc/>
    public void Match<M>(Func<T, M> f)
    {
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(M));
        foreach (PropertyDescriptor property in properties)
        {
            _expression.MatchColumns.Add(property.Name);
        }
    }
}

/// <summary>
/// A migration operation indicating a merge of data, either inserting or updating.
/// </summary>
internal class MergeDataExpression : MigrationExpressionBase
{
    /// <summary>
    /// The rows to insert or update.
    /// </summary>
    private readonly List<InsertionDataDefinition> _rows = new List<InsertionDataDefinition>();

    /// <summary>
    /// The target schema, if specified.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// The target table.
    /// </summary>
    public string TableName { get; set; } = null!;

    private readonly Dictionary<string, object> _additionalFeatures = new Dictionary<string, object>();

    /// <summary>
    /// The columns on which to check the target table for an existing row.
    /// </summary>
    private readonly List<string> _matchColumns = new List<string>();

    /// <summary>
    /// The rows to insert or update.
    /// </summary>
    public List<InsertionDataDefinition> Rows
    {
        get { return _rows; }
    }

    /// <inheritdoc/>
    public IDictionary<string, object> AdditionalFeatures
    {
        get { return _additionalFeatures; }
    }

    /// <summary>
    /// The columns on which to check the target table for an existing row.
    /// </summary>
    public List<string> MatchColumns
    {
        get { return _matchColumns; }
    }

    /// <summary>
    /// Executes the merge operation.
    /// </summary>
    /// <param name="processor">The migration processor.</param>
    public override void ExecuteWith(IMigrationProcessor processor)
    {
        //Access the table in the connection
        var existingDataSet = processor.ReadTableData(SchemaName, TableName);
        var existingTable = existingDataSet.Tables[0];

        foreach (var row in _rows)
        {
            //Test of the row exists by reading from the table
            var exists = existingTable.Rows.OfType<DataRow>().Any(r =>
            {
                return _matchColumns.Select(mc =>
                {
                    var ex = r[mc];
                    var nw = row.Where(p => p.Key == mc).Select(p => p.Value).SingleOrDefault();
                    if (ex == null || nw == null)
                    {
                        return ex == nw;
                    }
                    return ex.Equals(nw);
                }).All(m => m);
            });

            //Perform either an update or an insert, based on if the data exists
            if (exists)
            {
                ExecuteUpdateWith(processor, row);
            }
            else
            {
                ExecuteInsertWith(processor, row);
            }
        }
    }

    /// <summary>
    /// Performs an update on existing data.
    /// </summary>
    /// <param name="processor">The migration processor.</param>
    /// <param name="row">The row to update.</param>
    private void ExecuteUpdateWith(IMigrationProcessor processor, List<KeyValuePair<string, object>> row)
    {
        var update = new UpdateDataExpression
        {
            SchemaName = SchemaName,
            TableName = TableName,
            IsAllRows = false,
            Set = row.Where(p => !_matchColumns.Contains(p.Key)).ToList(),
            Where = _matchColumns.Select(mc =>
            {
                var v = row.Where(p => p.Key == mc).Select(p => p.Value).SingleOrDefault();
                return new KeyValuePair<string, object?>(mc, v);
            }).ToList()
        };

        processor.Process(update);
    }

    /// <summary>
    /// Performs an insert of new data.
    /// </summary>
    /// <param name="processor">The migration processor.</param>
    /// <param name="row">The row to insert.</param>
    private void ExecuteInsertWith(IMigrationProcessor processor, InsertionDataDefinition row)
    {
        var insert = new InsertDataExpression
        {
            SchemaName = SchemaName,
            TableName = TableName
        };

        foreach (var af in _additionalFeatures)
        {
            insert.AdditionalFeatures.Add(af.Key, af.Value);
        }

        insert.Rows.Add(row);

        processor.Process(insert);
    }
}
