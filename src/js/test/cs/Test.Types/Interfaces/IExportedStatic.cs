using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedStatic
{
    Record? Record { get; set; }
    Task<IExportedInstanced> GetInstanceAsync (string arg);
}
