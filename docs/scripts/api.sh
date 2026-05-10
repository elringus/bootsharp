# https://typedoc-plugin-markdown.org/themes/vitepress/quick-start

DOCS_DIR=$(cd "$(dirname "$0")/.." && pwd)
JS_DIR=$(cd "$DOCS_DIR/../src/js" && pwd)

echo '{
    "entryPoints": [
        "src/index.mts"
    ],
    "tsconfig": "tsconfig.json",
    "out": "../../docs/api",
    "name": "Bootsharp",
    "readme": "none",
    "githubPages": false,
    "useCodeBlocks": true,
    "hideGenerator": true,
    "hideBreadcrumbs": true,
    "textContentMappings": {
        "title.indexPage": "API Reference",
        "title.memberPage": "{name}",
    },
    "plugin": ["typedoc-plugin-markdown", "typedoc-vitepress-theme"]
}' > "$JS_DIR/typedoc.json"

(cd "$JS_DIR" && NODE_PATH="$DOCS_DIR/node_modules" "$DOCS_DIR/node_modules/.bin/typedoc" --skipErrorChecking)
sed -i -z "s/API Reference/API Reference\nAuto-generated with [typedoc-plugin-markdown](https:\/\/typedoc-plugin-markdown.org)./" "$DOCS_DIR/api/index.md"
rm "$JS_DIR/typedoc.json"
