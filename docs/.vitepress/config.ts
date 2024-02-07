import { defineConfig } from "vitepress";
import proc from "node:child_process";
import imgit from "imgit/vite";
import md from "./md";

// https://vitepress.dev/reference/site-config
export default defineConfig({
    title: "Bootsharp",
    titleTemplate: ":title • Bootsharp",
    appearance: "dark",
    cleanUrls: true,
    lastUpdated: true,
    markdown: md,
    vite: { plugins: [imgit({ width: 688 })] },
    head: [
        ["link", { rel: "icon", href: "/favicon.svg" }],
        ["link", { rel: "preload", href: "/fonts/inter.woff2", as: "font", type: "font/woff2", crossorigin: "" }],
        ["link", { rel: "preload", href: "/fonts/jb.woff2", as: "font", type: "font/woff2", crossorigin: "" }],
        ["meta", { name: "twitter:card", content: "summary_large_image" }]
    ],
    themeConfig: {
        logo: { src: "/favicon.svg" },
        logoLink: "/",
        socialLinks: [{ icon: "github", link: "https://github.com/elringus/bootsharp" }],
        search: { provider: "local" },
        lastUpdated: { text: "Updated", formatOptions: { dateStyle: "medium" } },
        sidebarMenuLabel: "Menu",
        darkModeSwitchLabel: "Appearance",
        returnToTopLabel: "Return to top",
        outline: { label: "On this page", level: "deep" },
        docFooter: { prev: "Previous page", next: "Next page" },
        nav: [
            { text: "Guide", link: "/guide/", activeMatch: "/guide/" },
            { text: "Reference", link: "/api/", activeMatch: "/api/" },
            {
                text: proc.execSync("git describe --abbrev=0 --tags").toString(), items: [
                    { text: "Changes", link: "https://github.com/elringus/bootsharp/releases/latest" },
                    { text: "Contribute", link: "https://github.com/elringus/bootsharp/labels/help%20wanted" }
                ]
            }
        ],
        editLink: {
            pattern: "https://github.com/elringus/bootsharp/edit/main/docs/:path",
            text: "Edit this page on GitHub"
        },
        sidebar: {
            "/guide/": [
                {
                    text: "Guide",
                    items: [
                        { text: "Introduction", link: "/guide/" },
                        { text: "Getting Started", link: "/guide/getting-started" },
                        { text: "Type Declarations", link: "/guide/declarations" },
                        { text: "Namespaces", link: "/guide/namespaces" },
                        { text: "Events", link: "/guide/events" },
                        { text: "Serialization", link: "/guide/serialization" },
                        { text: "Interop Interfaces", link: "/guide/interop-interfaces" },
                        { text: "Interop Instances", link: "/guide/interop-instances" },
                        { text: "Emit Preferences", link: "/guide/emit-prefs" },
                        { text: "Build Configuration", link: "/guide/build-config" },
                        { text: "Sideloading Binaries", link: "/guide/sideloading" },
                        { text: "AOT Compilation", link: "/guide/aot" },
                        { text: "Assembly Trimming", link: "/guide/trimming" },
                        { text: "Multi-threading", link: "/guide/multi-threading" }
                    ]
                },
                {
                    text: "Extensions",
                    items: [
                        { text: "Dependency Injection", link: "/guide/extensions/dependency-injection" },
                        { text: "File System", link: "/guide/extensions/file-system" }
                    ]
                }
            ],
            "/api/": [{ text: "Reference", items: (await import("./../api/typedoc-sidebar.json")).default }]
        }
    },
    sitemap: { hostname: "https://bootsharp.com" }
});
