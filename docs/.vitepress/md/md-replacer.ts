// Based on https://github.com/rlidwka/markdown-it-regexp.

import { inherits } from "node:util";
import { MarkdownEnv, MarkdownRenderer } from "vitepress";

let instanceId = 0;

export function Replacer(regexp: RegExp, replace: (match: string[], env: MarkdownEnv) => string) {
    let self: any = (md: any) => self.init(md);
    self.__proto__ = Replacer.prototype;
    self.regexp = new RegExp("^" + regexp.source, regexp.flags);
    self.replace = replace;
    self.id = `md-replacer-${instanceId}`;
    instanceId++;
    return self;
}

inherits(Replacer, Function);

Replacer.prototype.init = function (md: MarkdownRenderer) {
    md.inline.ruler.push(this.id, this.parse.bind(this));
    md.renderer.rules[this.id] = this.render.bind(this);
};

Replacer.prototype.parse = function (state: any, silent: any) {
    let match = this.regexp.exec(state.src.slice(state.pos));
    if (!match) return false;

    state.pos += match[0].length;
    if (silent) return true;

    let token = state.push(this.id, "", 0);
    token.meta = { match: match };
    return true;
};

Replacer.prototype.render = function (tokens: any, id: any, options: any, env: MarkdownEnv) {
    return this.replace(tokens[id].meta.match, env);
};
