using System.Threading.Tasks;

namespace Test.Types;

public class ExportedInstanced (string instanceArg) : IExportedInstanced
{
    public event RecordChanged<IExportedInstanced>? OnRecordChanged;

    public Record? Record { get; set => OnRecordChanged?.Invoke(this, field = value); }

    public string GetInstanceArg () => instanceArg;

    public async Task<string> GetRecordIdAsync (Record record)
    {
        await Task.Delay(1);
        return record.Id;
    }
}
