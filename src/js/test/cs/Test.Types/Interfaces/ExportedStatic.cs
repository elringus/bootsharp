using System.Threading.Tasks;

namespace Test.Types;

public class ExportedStatic : IExportedStatic
{
    public async Task<IExportedInstanced> GetInstanceAsync (string arg)
    {
        await Task.Delay(1);
        return new ExportedInstanced(arg);
    }
}
