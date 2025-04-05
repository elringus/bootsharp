const std = @import("std");

var arena = std.heap.ArenaAllocator.init(std.heap.wasm_allocator);
const ally = arena.allocator();

const opt = .{
    .parse = std.json.ParseOptions{
        .ignore_unknown_fields = true,
    },
    .stringify = std.json.StringifyOptions{
        .whitespace = .minified,
    },
};

pub const Data = struct {
    info: []const u8,
    ok: bool,
    revision: i32,
    messages: []const []const u8,
};

extern "x" fn getNumber() i32;
extern "x" fn getStruct() u64;

export fn echoNumber() i32 {
    return getNumber();
}

export fn echoStruct() u64 {
    _ = arena.reset(.retain_capacity);
    const input = decodeString(getStruct());
    const json = std.json.parseFromSlice(Data, ally, input, opt.parse) catch unreachable;
    var output = std.ArrayList(u8).init(ally);
    std.json.stringify(json.value, opt.stringify, output.writer()) catch unreachable;
    return encodeString(output.items);
}

export fn fi(n: i32) i32 {
    if (n <= 1) return n;
    return fi(n - 1) + fi(n - 2);
}

fn decodeString(ptr_and_len: u64) []const u8 {
    const ptr = @as(u32, @truncate(ptr_and_len));
    const len = @as(u32, @truncate(ptr_and_len >> 32));
    return @as([*]const u8, @ptrFromInt(ptr))[0..len];
}

fn encodeString(str: []const u8) u64 {
    return (@as(u64, str.len) << 32) | @intFromPtr(str.ptr);
}
