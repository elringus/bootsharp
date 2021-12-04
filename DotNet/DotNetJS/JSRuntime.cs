using System;
using System.Reflection;
using System.Text.Json;
using Microsoft.JSInterop.WebAssembly;

namespace DotNetJS
{
    internal class JSRuntime : WebAssemblyJSRuntime
    {
        public void ConfigureJson (Action<JsonSerializerOptions> action)
        {
            action.Invoke(JsonSerializerOptions);
            ConfigureDefault(action);
        }

        private static void ConfigureDefault (Action<JsonSerializerOptions> action)
        {
            var defaultRuntime = GetDefaultRuntime();
            var defaultRuntimeOptions = GetJsonSerializerOptions(defaultRuntime);
            action.Invoke(defaultRuntimeOptions);
        }

        private static WebAssemblyJSRuntime GetDefaultRuntime ()
        {
            var assembly = Assembly.Load("Microsoft.AspNetCore.Components.WebAssembly");
            var type = assembly.GetType("Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime");
            var instance = type?.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            return instance as WebAssemblyJSRuntime ?? throw new InvalidOperationException("Failed to access default runtime.");
        }

        private static JsonSerializerOptions GetJsonSerializerOptions (WebAssemblyJSRuntime runtime)
        {
            var options = typeof(Microsoft.JSInterop.JSRuntime).GetProperty(nameof(JsonSerializerOptions),
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(runtime) as JsonSerializerOptions;
            return options ?? throw new InvalidOperationException("Failed to access JSON serializer options of JS runtime.");
        }
    }
}
