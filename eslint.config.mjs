// eslint.config.mjs
import { Linter } from 'eslint';
import tsParser from '@typescript-eslint/parser';
import reactPlugin from 'eslint-plugin-react';
import reactHooksPlugin from 'eslint-plugin-react-hooks';

/** @type {Linter.FlatConfig[]} */
export default [
  {
    // For JavaScript, JSX, TypeScript, and TSX files
    files: ['**/*.js', '**/*.jsx', '**/*.ts', '**/*.tsx'],
    languageOptions: {
      parser: tsParser, // Use @typescript-eslint/parser for both JavaScript and TypeScript files
      parserOptions: {
        ecmaVersion: 2021, // Use ECMAScript 2021 features
        sourceType: 'module', // Support ES modules
        ecmaFeatures: {
          jsx: true, // Enable JSX parsing for JavaScript/JSX files
        },
        project: './tsconfig.json', // Specify tsconfig.json for TypeScript files
      },
    },
    plugins: {
      react: reactPlugin, // React plugin object
      'react-hooks': reactHooksPlugin, // React Hooks plugin object
    },
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'UI/Functions/WebRequest',
              importNames: [
                'default',
                'webRequest',
                'getBlob',
                'getTextResponse',
                'getText',
                'getJson',
                'getList',
                'getOne',
              ],
              message: "Do not import from 'UI/Functions/WebRequest' directly. Use the generated API in an 'Api/' module instead.",
            },
          ],
        },
      ],
      // React specific rules
      'react/jsx-uses-react': 'off', // React 17 JSX Transform no longer requires React import
      'react/react-in-jsx-scope': 'off', // React 17 JSX Transform no longer requires React in scope

      // React Hooks rules
      'react-hooks/rules-of-hooks': 'error', // Enforce rules of hooks
      'react-hooks/exhaustive-deps': 'warn', // Ensure all dependencies are in useEffect's dependency array
    }
  },
];

// At the root level of your config, add ignorePatterns
export const ignorePatterns = ['node_modules/**', 'TypeScript/**'];