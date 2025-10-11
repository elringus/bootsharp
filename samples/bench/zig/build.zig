const std = @import("std");

pub fn build(b: *std.Build) void {
    const optimize = b.standardOptimizeOption(.{});

    const target_query = std.Target.Query{
        .cpu_arch = .wasm32,
        .os_tag = .freestanding,
        .cpu_features_add = std.Target.wasm.featureSet(&.{
            .simd128,
            .relaxed_simd,
            .tail_call,
        }),
    };
    const target = b.resolveTargetQuery(target_query);

    const lib = b.addExecutable(.{
        .name = "zig",
        .root_module = b.createModule(.{
            .root_source_file = b.path("main.zig"),
            .target = target,
            .optimize = optimize,
        }),
        .use_llvm = true,
        .use_lld = true,
    });

    lib.entry = .disabled;
    lib.rdynamic = true;
    lib.want_lto = true;

    b.installArtifact(lib);
}
