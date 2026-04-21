using System.Threading.Tasks;

namespace Test.Types;

public class ExportedInstanced (string instanceArg) : IExportedInstanced
{
    public Record? Record { get; set; }

    public string GetInstanceArg () => instanceArg;

    public async Task<string> GetRecordIdAsync (Record record)
    {
        await Task.Delay(1);
        return record.Id;
    }
}
