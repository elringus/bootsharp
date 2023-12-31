import { MarkdownOptions, MarkdownRenderer } from "vitepress";
import { AppendIconToExternalLinks } from "./md-link";

export default {
    config: installPlugins,
    attrs: { disable: true } // https://github.com/vuejs/vitepress/issues/2440
} satisfies MarkdownOptions;

function installPlugins(md: MarkdownRenderer) {
    md.use(AppendIconToExternalLinks);
}
