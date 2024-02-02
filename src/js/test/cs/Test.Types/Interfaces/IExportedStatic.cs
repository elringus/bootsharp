using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedStatic
{
    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
