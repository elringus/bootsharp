using System.Threading.Tasks;

namespace Test.Types;

public class ExportedStatic : IExportedStatic
{
    public event IExportedStatic.RecordChanged? OnRecordChanged;

    public Record? Record { get; set => OnRecordChanged?.Invoke(field = value); }

    public async Task<IExportedInstanced> GetInstanceAsync (string arg)
    {
        await Task.Delay(1);
        return new ExportedInstanced(arg);
    }
}
