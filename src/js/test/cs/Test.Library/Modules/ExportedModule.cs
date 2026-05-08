using System.Threading.Tasks;

namespace Test.Library;

public class ExportedModule : IExportedModule
{
    public event IExportedModule.RecordChanged? OnRecordChanged;

    public Record? Record { get; set => OnRecordChanged?.Invoke(field = value); }

    public async Task<IExportedInstanced> GetInstanceAsync (string arg)
    {
        await Task.Delay(1);
        return new ExportedInstanced(arg);
    }
}
