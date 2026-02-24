(function () {
    const editors = {};
    const outputEditors = {};

    function getEditor(editorElementId) {
        const editor = editors[editorElementId];
        if (!editor) {
            throw new Error("Editor is not initialized: " + editorElementId);
        }
        return editor;
    }

    window.sqExpressSqlEditor = {
        initialize: function (editorElementId, dotNetRef, initialValue) {
            const editor = ace.edit(editorElementId);
            editor.setTheme("ace/theme/tomorrow");
            editor.session.setMode("ace/mode/sql");
            editor.setFontSize(14);
            editor.setShowPrintMargin(false);
            editor.setOption("wrap", true);
            editor.setOption("tabSize", 4);
            editor.setOption("useSoftTabs", true);
            editor.setValue(initialValue || "", -1);

            editor.session.on("change", function () {
                dotNetRef.invokeMethodAsync("OnSqlChangedFromJs", editor.getValue());
            });

            editors[editorElementId] = editor;
        },

        getValue: function (editorElementId) {
            return getEditor(editorElementId).getValue();
        },

        setValue: function (editorElementId, value) {
            getEditor(editorElementId).setValue(value || "", -1);
        },

        dispose: function (editorElementId) {
            const editor = editors[editorElementId];
            if (!editor) {
                return;
            }

            editor.destroy();
            delete editors[editorElementId];
        }
    };

    window.sqExpressCodeOutput = {
        initialize: function (editorElementId, initialValue) {
            const editor = ace.edit(editorElementId);
            editor.setTheme("ace/theme/tomorrow_night");
            editor.session.setMode("ace/mode/csharp");
            editor.setFontSize(13);
            editor.setShowPrintMargin(false);
            editor.setOption("wrap", false);
            editor.setOption("tabSize", 4);
            editor.setOption("useSoftTabs", true);
            editor.setOption("highlightActiveLine", false);
            editor.setOption("highlightGutterLine", false);
            editor.setReadOnly(true);
            editor.renderer.setShowGutter(true);
            editor.setValue(initialValue || "", -1);

            outputEditors[editorElementId] = editor;
        },

        setValue: function (editorElementId, value) {
            const editor = outputEditors[editorElementId];
            if (!editor) {
                return;
            }

            editor.setValue(value || "", -1);
        },

        dispose: function (editorElementId) {
            const editor = outputEditors[editorElementId];
            if (!editor) {
                return;
            }

            editor.destroy();
            delete outputEditors[editorElementId];
        }
    };

    window.sqExpressSettingsStore = {
        get: function (key) {
            try {
                return window.localStorage.getItem(key);
            } catch (e) {
                return null;
            }
        },
        set: function (key, value) {
            try {
                window.localStorage.setItem(key, value || "");
            } catch (e) {
                // ignore when storage is unavailable
            }
        }
    };
})();
