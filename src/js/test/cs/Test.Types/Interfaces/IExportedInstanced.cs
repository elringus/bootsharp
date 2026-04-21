using System.Threading.Tasks;

namespace Test.Types;

public interface IExportedInstanced
{
    Record? Record { get; set; }
    string GetInstanceArg ();
    Task<string> GetRecordIdAsync (Record record);
}
