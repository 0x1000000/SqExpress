(function () {
    const editors = {};

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
})();
