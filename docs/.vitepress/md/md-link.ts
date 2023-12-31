import type { MarkdownRenderer } from "vitepress";
import type { RenderRule } from "markdown-it/lib/renderer";

export function AppendIconToExternalLinks(md: MarkdownRenderer) {
    const renderToken: RenderRule = (tokens, idx, options, env, self) => self.renderToken(tokens, idx, options);
    const defaultLinkOpenRenderer = md.renderer.rules.link_open || renderToken;
    const defaultLinkCloseRenderer = md.renderer.rules.link_close || renderToken;
    let externalLinkOpen = false;

    md.renderer.rules.link_open = (tokens, idx, options, env, self) => {
        const token = tokens[idx];
        const href = token.attrGet("href");

        if (href) {
            token.attrJoin("class", "vp-link");
            if (/^((ht|f)tps?):\/\/?/.test(href))
                externalLinkOpen = true;
        }

        return defaultLinkOpenRenderer(tokens, idx, options, env, self);
    };

    md.renderer.rules.link_close = (tokens, idx, options, env, self) => {
        if (externalLinkOpen) {
            externalLinkOpen = false;
            return `<span class="external-link-icon">&nbsp;↗</span>${self.renderToken(tokens, idx, options)}`;
        }
        return defaultLinkCloseRenderer(tokens, idx, options, env, self);
    };
}
