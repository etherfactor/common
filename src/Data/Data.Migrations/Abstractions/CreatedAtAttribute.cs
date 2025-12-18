using FluentMigrator;

namespace EtherGizmos.Common.Abstractions;

/// <summary>
/// Generates a version number for the migration based on the date/time at which the migration was authored. Migrations will
/// be run in order, from oldest to newest, running all migrations that have not previously been run.
/// </summary>
internal class CreatedAtAttribute : MigrationAttribute
{
    /// <summary>
    /// Construct the attribute.
    /// </summary>
    /// <param name="year">The year in which the migration was authored.</param>
    /// <param name="month">The month in which the migration was authored.</param>
    /// <param name="day">The day on which the migration was authored.</param>
    /// <param name="hour">The hour in which the migration was authored.</param>
    /// <param name="minute">The minute in which the migration was authored.</param>
    /// <param name="description">A summary of the change.</param>
    /// <param name="trackingId">A tracking number linking to a feature or issue.</param>
    /// <param name="transactionBehavior">How the migration should handle migrations.</param>
    public CreatedAtAttribute(short year, short month, short day, short hour, short minute, string description,
        int trackingId = -1, TransactionBehavior transactionBehavior = TransactionBehavior.Default)
        : base(GetVersion(year, month, day, hour, minute), transactionBehavior, GetDescription(description, trackingId))
    {
    }

    /// <summary>
    /// Calculates the version number, based on the author date of the migration.
    /// </summary>
    /// <param name="year">The year in which the migration was authored.</param>
    /// <param name="month">The month in which the migration was authored.</param>
    /// <param name="day">The day on which the migration was authored.</param>
    /// <param name="hour">The hour in which the migration was authored.</param>
    /// <param name="minute">The minute in which the migration was authored.</param>
    /// <returns>The calculated version number.</returns>
    private static long GetVersion(short year, short month, short day, short hour, short minute)
    {
        return year * 100000000L + month * 1000000L + day * 10000L + hour * 100L + minute;
    }

    /// <summary>
    /// Generates a description string for the migration. Currently just prepends the tracking number.
    /// </summary>
    /// <param name="description">A summary of the change.</param>
    /// <param name="trackingId">A tracking number linking to a feature or issue.</param>
    /// <returns>The generated description.</returns>
    private static string? GetDescription(string description, int trackingId)
    {
        List<string> fragments = new List<string>();

        if (trackingId > 0)
        {
            fragments.Add($"#{trackingId}");
        }

        fragments.Add(description);

        return string.Join(" | ", fragments);
    }
}
