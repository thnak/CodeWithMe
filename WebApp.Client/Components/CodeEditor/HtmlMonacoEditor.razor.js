﻿export class HtmlMonacoEditor {
    static AssemblyName = "WebApp.Client"
    static RootElement = null;
    static containerId = "";
    static htmlCodeValue = "";

    static htmlEditor = null;
    static cleanupObserver = null;


    static initEditor(containerId, htmlCode = '') {
        this.containerId = containerId;
        this.htmlCodeValue = htmlCode;
        if (this.htmlEditor) {
            console.log('Editor already initialized');
            return;
        }

        require.config({paths: {'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.51.0/min/vs'}});

        require(['vs/editor/editor.main'], () => {
            this.RootElement = document.getElementById(this.containerId);
            if (this.RootElement === null) {

                return;
            }

            this.htmlEditor = monaco.editor.create(this.RootElement, {
                value: this.htmlCodeValue,
                language: 'html',
                theme: "vs-dark",
                automaticLayout: true
            });

            // Add a keydown event listener for the HTML Editor
            this.htmlEditor.onKeyDown(async (e) => {
                const htmlContent = this.htmlEditor.getValue();
                try {
                    await DotNet.invokeMethodAsync(this.AssemblyName, 'HtmlMonanoChangeListener', htmlContent);
                } catch (e) {
                    console.log(e);
                }
                console.log('HTML Content on KeyDown:', htmlContent);
            });


            console.log(`Monaco Editor initialized for container: ${this.containerId}`);


            async function loadAllCSS() {
                let cssContent = '';

                // Get CSS from <style> tags
                document.querySelectorAll('style').forEach(styleTag => {
                    cssContent += styleTag.innerHTML;
                });

                // Fetch CSS from <link> tags with rel="stylesheet"
                const linkPromises = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map(linkTag => {
                    return fetch(linkTag.href)
                        .then(response => response.text())
                        .then(css => {
                            cssContent += css;
                        })
                        .catch(err => console.error('Error loading CSS:', err));
                });

                // Wait for all external CSS files to load
                await Promise.all(linkPromises);

                return cssContent;
            }

            // Function to extract CSS classes from the CSS editor content
            function extractCSSClasses(cssCode) {
                const classRegex = /\.([a-zA-Z0-9_-]+)\s*\{/g;
                const variableRegex = /--([a-zA-Z0-9_-]+)\s*:/g;

                const classes = [];
                let match;
                while ((match = classRegex.exec(cssCode)) !== null) {
                    classes.push(match[1]);
                }
                while ((match = variableRegex.exec(cssCode)) !== null) {
                    classes.push(`--${match[1]}`);
                }

                return classes;
            }

            // Register a completion provider for the HTML editor
            monaco.languages.registerCompletionItemProvider('html', {
                provideCompletionItems: async function () {
                    let cssContent = await DotNet.invokeMethodAsync('WebApp.Client', 'GetCurrentStyle');
                    const allCSSContent = extractCSSClasses(await loadAllCSS());
                    const cssClasses = extractCSSClasses(cssContent);
                    cssClasses.push(...allCSSContent);

                    const suggestions = cssClasses.map(cssClass => ({
                        label: cssClass,
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: cssClass,
                        range: null
                    }));
                    return {suggestions: suggestions};
                }
            });


            // Setup cleanup observer
            this.createCleanupObserver();
        });
    }

    static getValue() {
        if (this.htmlEditor) {
            return this.htmlEditor.getValue();
        }
        return '';
    }

    static setValue(newValue) {
        if (this.htmlEditor) {
            this.htmlEditor.setValue(newValue);
        }
    }

    static updateSize() {
        // make editor as small as possible
        if (this.htmlEditor) {
            this.htmlEditor.layout({width: 0, height: 0});

            // wait for next frame to ensure last layout finished
            window.requestAnimationFrame(() => {
                // get the parent dimensions and re-layout the editor
                this.htmlEditor.layout();
            });
        }
    }

    static disposeEditor() {

        if (this.htmlEditor) {
            this.htmlEditor.dispose();
            this.htmlEditor = null;
        }

        console.log('Disposing of editor');
    }

    static createCleanupObserver() {
        const target = document.getElementById(this.containerId);

        this.cleanupObserver = new MutationObserver((mutations) => {
            const targetRemoved = mutations.some((mutation) => {
                const nodes = Array.from(mutation.removedNodes);
                return nodes.indexOf(target) !== -1;
            });

            if (targetRemoved) {
                console.log('Target element removed. Performing cleanup.');

                // Cleanup resources here
                this.disposeEditor(); // Ensure editor is disposed

                // Disconnect and delete MutationObserver
                this.cleanupObserver && this.cleanupObserver.disconnect();
                this.cleanupObserver = null;
            }
        });

        // Observe changes to the container's parent node
        if (target && target.parentNode) {
            this.cleanupObserver.observe(target.parentNode, {childList: true});
        }
    }
}

window.HtmlMonacoEditor = HtmlMonacoEditor;