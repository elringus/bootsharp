#pragma warning disable CA1050
// Global namespace required for EventExtensions to be picked by the spliced CS.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bootsharp;
using Test.Library;

[SpecializeImport(typeof(IComparer<>))]
public abstract class ComparerImport<T> (int id) : SpecializedImport(id), IComparer<T>
{
    public abstract int Compare (T? x, T? y);
}

[SpecializeExport(typeof(IComparer<>))]
public class ComparerExport<T> (IComparer<T> cmp) : SpecializedExport(cmp)
{
    public int Compare (T? x, T? y) => cmp.Compare(x, y);
}

public abstract class Event<T> where T : Delegate
{
    protected List<T> Handlers { get; } = [];
    public void Subscribe (T handler) => Handlers.Add(handler);
    public void Unsubscribe (T handler) => Handlers.Remove(handler);
    public abstract void Replay (T handler);
}

/// <summary>
/// Specialized event handler.
/// </summary>
/// <param name="nul">Nul parameter doc.</param>
/// <param name="str">Str parameter doc.</param>
/// <param name="opt">Opt parameter doc.</param>
public delegate void SpecialHandler (string? nul, string str, int? opt = null);

public sealed class SpecialHandlerEvent : Event<SpecialHandler>
{
    public (string? nul, string str, int? opt)? Last { get; private set; }

    public void Broadcast (string? nul, string str, int? opt = null)
    {
        Last = (nul, str, opt);
        foreach (var handler in Handlers)
            handler(nul, str, opt);
    }

    public override void Replay (SpecialHandler handler)
    {
        if (Last is { } last)
            handler(last.nul, last.str, last.opt);
    }
}

public static class EventExtensions
{
    extension (Event<SpecialHandler> evt)
    {
        public void Broadcast (string? nul, string str, int? opt = null) =>
            ((SpecialHandlerEvent)evt).Broadcast(nul, str, opt);
    }

    extension (Event<IBidirectional.SpecialHandler> evt)
    {
        public void Broadcast (IBidirectional? bi) =>
            ((IBidirectional.SpecialEvent)evt).Broadcast(bi);
    }
}

[SpecializeImport(typeof(Event<>),
    CS:
    """
    protected override object Unwrap () {
        if (Event != null) return Event;
        ImportedByEvent.Add(Event = new $full(), this);
        Subscribe(Event.Broadcast);
        return Event;
    }
    """,
    JSCtor:
    """
    const event = new Event();
    event._id = _id;
    this.subscribe(event.broadcast.bind(event));
    return event;
    """,
    Decl:
    """
    export interface $name {
        readonly last?: Parameters<$T0>;
        subscribe(handler: $T0): string;
        unsubscribe(handler: $T0): void;
        broadcast: $T0;
    }
    """)]
public abstract class EventImport<T> (int id) : SpecializedImport(id) where T : Delegate
{
    protected internal static readonly ConditionalWeakTable<Event<T>, SpecializedImport> ImportedByEvent = new();
    protected Event<T>? Event;

    public abstract void Subscribe (T handler);
}

[SpecializeExport(typeof(Event<>))]
public sealed class EventExport<T> (Event<T> evt) : SpecializedExport(Resolve(evt)) where T : Delegate
{
    public void Subscribe (T handler)
    {
        evt.Subscribe(handler);
        evt.Replay(handler);
    }

    private static object Resolve (Event<T> evt) =>
        EventImport<T>.ImportedByEvent.TryGetValue(evt, out var imported) ? imported : evt;
}
