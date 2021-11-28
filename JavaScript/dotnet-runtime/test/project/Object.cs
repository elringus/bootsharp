using System.Threading.Tasks;
using DotNetJS;
using Microsoft.JSInterop;

namespace Test;

public static class Object
{
    public class Instance
    {
        private string var;

        [JSInvokable]
        public void SetVar (string value) => var = value;

        [JSInvokable]
        public string GetVar () => var;

        [JSInvokable]
        public string SetFromOther (DotNetObjectReference<Instance> objRef) => var = objRef.Value.var;
    }

    [JSInvokable]
    public static DotNetObjectReference<Instance> CreateInstance ()
    {
        return DotNetObjectReference.Create(new Instance());
    }

    [JSInvokable]
    public static IJSObjectReference GetAndReturnJSObject ()
    {
        return JS.Invoke<IJSObjectReference>("getObject");
    }

    [JSInvokable]
    public static async Task InvokeOnJSObjectAsync (IJSObjectReference obj, string function, params object[] args)
    {
        await obj.InvokeVoidAsync(function, args);
    }
}
