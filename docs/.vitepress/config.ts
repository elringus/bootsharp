import { defineConfig } from "vitepress";
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
            {
                text: "v0.1.0", items: [
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
                        { text: "Introduction", link: "/guide/" }
                    ]
                }
            ]
        }
    },
    sitemap: { hostname: "https://bootsharp.com" }
});
