using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedInstanced
{
    string GetInstanceArg ();
    Task<string> GetVehicleIdAsync (Vehicle vehicle);
}
