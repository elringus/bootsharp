using System.Text;

namespace Bootsharp.Publish;

internal class CodeBuilder (string content = "")
{
    private record struct Block (string? Separator, bool Empty = true);

    private readonly StringBuilder bld = new(content);
    private readonly Stack<Block> blocks = new();

    public void Clear ()
    {
        bld.Clear();
        blocks.Clear();
    }

    public override string ToString ()
    {
        return bld.ToString();
    }

    public CodeBuilder Append (string content)
    {
        bld.Append(content);
        return this;
    }

    public CodeBuilder Line (string content)
    {
        if (blocks.TryPeek(out var blk))
            OnBlockLine(blk);
        AppendLine(content);
        return this;
    }

    public CodeBuilder Join (string separator, IEnumerable<string> parts)
    {
        bld.AppendJoin(separator, parts);
        return this;
    }

    public CodeBuilder Enter (string header, string? lineSeparator = null)
    {
        Line(header);
        blocks.Push(new(lineSeparator));
        return this;
    }

    public CodeBuilder Exit (string footer)
    {
        blocks.Pop();
        AppendLine(footer);
        return this;
    }

    private void OnBlockLine (Block blk)
    {
        if (blk is { Empty: false, Separator: { } sep })
            bld.Insert(bld.Length - 1, sep);
        if (blk.Empty)
            blocks.Push(blocks.Pop() with { Empty = false });
    }

    private void AppendLine (string content)
    {
        bld.Append(' ', blocks.Count * 4);
        bld.Append(content);
        bld.Append('\n');
    }
}
