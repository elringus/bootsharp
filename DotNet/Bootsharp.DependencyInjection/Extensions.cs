﻿using Microsoft.Extensions.DependencyInjection;

namespace Bootsharp.DependencyInjection;

/// <summary>
/// Extension methods for dependency injection.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers JavaScript bindings generated by Bootsharp.
    /// </summary>
    public static IServiceCollection AddBootsharp (this IServiceCollection services)
    {
        foreach (var (impl, binding) in BindingRegistry.Exports)
            services.AddSingleton(impl, provider => {
                var handler = provider.GetService(binding.Api);
                if (handler is null) throw new Error($"Failed to run Bootsharp: '{binding.Api}' dependency is not registered.");
                return binding.Factory(provider.GetRequiredService(binding.Api));
            });
        foreach (var (api, binding) in BindingRegistry.Imports)
            services.AddSingleton(api, binding.Implementation);
        return services;
    }

    /// <summary>
    /// Initializes exported JavaScript bindings generated by Bootsharp.
    /// </summary>
    public static IServiceProvider RunBootsharp (this IServiceProvider provider)
    {
        foreach (var (impl, _) in BindingRegistry.Exports)
            provider.GetRequiredService(impl);
        return provider;
    }
}
