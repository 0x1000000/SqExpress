import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import globals from "globals";
import tseslint from "typescript-eslint";
import unusedImports from "eslint-plugin-unused-imports";

export default tseslint.config(
  {
    ignores: [
      "**/node_modules/**",
      "**/dist/**",
      "**/build/**",
      "**/bin/**",
      "**/obj/**",
      "**/.tmp/**",
      "**/.npm-cache/**",
      "src/ast/generated/**",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    plugins: { "unused-imports": unusedImports },
    languageOptions: { globals: { ...globals.node, ...globals.browser } },
    rules: {
      "unused-imports/no-unused-imports": "error",
      // The package targets ES2020, before ErrorOptions.cause was introduced.
      "preserve-caught-error": "off",
      "prefer-const": ["error", { ignoreReadBeforeAssign: true }],
      "@typescript-eslint/no-empty-object-type": ["error", { allowObjectTypes: "always" }],
      "@typescript-eslint/no-unused-expressions": [
        "error",
        { allowShortCircuit: true, allowTernary: true },
      ],
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_", caughtErrorsIgnorePattern: "^_" },
      ],
    },
  },
  {
    files: ["test/types/**/*.ts"],
    rules: {
      "@typescript-eslint/no-unused-vars": "off",
      "@typescript-eslint/no-unused-expressions": "off",
    },
  },
  {
    files: ["src/exporters/index.ts"],
    // Export validation intentionally detects SQL NUL characters.
    rules: { "no-control-regex": "off" },
  },
  prettier,
);
