using System.Threading.Tasks;

namespace Test.Types;

public class ExportedStatic : IExportedStatic
{
    public Record? Record { get; set; }

    public async Task<IExportedInstanced> GetInstanceAsync (string arg)
    {
        await Task.Delay(1);
        return new ExportedInstanced(arg);
    }
}
