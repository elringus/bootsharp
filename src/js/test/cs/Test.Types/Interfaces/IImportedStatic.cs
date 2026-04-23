using System;
using System.Threading.Tasks;

namespace Test.Types;

public interface IImportedStatic
{
    event Action<Record?> OnRecordChanged;

    Record? Record { get; set; }

    Task<IImportedInstanced> GetInstanceAsync (string arg);
}
