using System.Threading.Tasks;

namespace Test.Types;

public class ExportedInstanced (string instanceArg) : IExportedInstanced
{
    public string GetInstanceArg () => instanceArg;

    public async Task<string> GetVehicleIdAsync (Vehicle vehicle)
    {
        await Task.Delay(1);
        return vehicle.Id;
    }
}
