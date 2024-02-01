namespace Test.Types;

public class ExportedStatic : IExportedStatic
{
    public IExportedInstanced GetInstance (string arg)
    {
        return new ExportedInstanced(arg);
    }
}
