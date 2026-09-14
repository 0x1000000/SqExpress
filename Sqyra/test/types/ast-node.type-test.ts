import { exprBooleanAnd, exprBooleanEq, exprInt32Literal, type ExprBooleanAnd } from "../../src/index.js";

const equality = exprBooleanEq({ left: exprInt32Literal({ value: 1 }), right: exprInt32Literal({ value: 2 }) });
const node = exprBooleanAnd({ left: equality, right: equality });
const typed: ExprBooleanAnd = node;
void typed;

// @ts-expect-error AST nodes are readonly.
node.left = equality;
// @ts-expect-error Boolean AND requires Boolean-expression children.
exprBooleanAnd({ left: exprInt32Literal({ value: 1 }), right: equality });
