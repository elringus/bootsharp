using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedInstanced
{
    Record? Record { get; set; }
    string GetInstanceArg ();
    Task<string> GetRecordIdAsync (Record record);
}
