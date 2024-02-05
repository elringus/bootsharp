using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedStatic
{
    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
