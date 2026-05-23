using System;
using System.Threading.Tasks;

namespace Test.Library;

public class ExportedInstanced (string instanceArg) : IExportedInstanced
{
    public event RecordChanged<IExportedInstanced>? OnRecordChanged;

    public Record? Record { get; set => OnRecordChanged?.Invoke(this, field = value); }

    public ExportedInnerInstanced Inner { get; } = new();
    public string GetInstanceArg () => instanceArg;

    public async Task<string> GetRecordIdAsync (Record record)
    {
        await Task.Delay(1);
        return record.Id;
    }

    public async Task<IBidirectional> GetBiAsync (Func<IBidirectional>? factory = null)
    {
        await Task.Delay(1);
        return factory?.Invoke() ?? new BidirectionalCS();
    }
}
