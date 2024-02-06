# Serialization

To override default JSON serializer options used for marshalling the interop data, use `JS.Runtime.ConfigureJson` method before the program entry point is invoked. For example, below will add `JsonStringEnumConverter` converter to allow serializing enums via strings:

```csharp
static class Program
{
    static Program () // Static constructor is invoked before 'Main'
    {
        JS.Runtime.ConfigureJson(options =>
            options.Converters.Add(new JsonStringEnumConverter())
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        );
    }

    public static void Main () { }
}
