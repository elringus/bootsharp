using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedInstanced
{
    string GetInstanceArg ();
    Task<string> GetVehicleIdAsync (Vehicle vehicle);
}
