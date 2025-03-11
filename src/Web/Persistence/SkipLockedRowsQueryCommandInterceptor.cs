using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Web.Persistence;

public class SkipLockedRowsQueryCommandInterceptor : DbCommandInterceptor
{
    private static string SkipLockedRowsSqlComment => $"-- {DbCommandTags.SkipLockedRows}";

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        MarkCommandToSkipLockedRowsIfQueryIsTaggedAccordingly(command);

        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        MarkCommandToSkipLockedRowsIfQueryIsTaggedAccordingly(command);

        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    private static void MarkCommandToSkipLockedRowsIfQueryIsTaggedAccordingly(DbCommand command)
    {
        if (command.CommandText.StartsWith(SkipLockedRowsSqlComment, StringComparison.Ordinal)) command.CommandText += " FOR UPDATE SKIP LOCKED";
    }
}