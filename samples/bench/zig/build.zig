const std = @import("std");

pub fn build(b: *std.Build) void {
    const lib = b.addExecutable(.{
        .name = "zig",
        .root_source_file = b.path("main.zig"),
        .target = b.resolveTargetQuery(.{
            .cpu_arch = .wasm32,
            .os_tag = .freestanding,
            .cpu_features_add = std.Target.wasm.featureSet(&.{
                .simd128,
                .relaxed_simd,
                .tail_call,
            }),
        }),
        .use_llvm = true,
        .use_lld = true,
        .optimize = b.standardOptimizeOption(.{}),
    });
    lib.entry = .disabled;
    lib.rdynamic = true;
    lib.want_lto = true;
    b.installArtifact(lib);
}
