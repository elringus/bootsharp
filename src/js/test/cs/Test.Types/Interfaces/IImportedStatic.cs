using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedStatic
{
    Record? Record { get; set; }
    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
