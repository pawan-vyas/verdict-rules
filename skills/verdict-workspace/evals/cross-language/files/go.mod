// A Go project manifest, used by the unsupported-language eval.
//
// Verdict ships no Go SDK. The skill's first step tells an agent to say so
// plainly rather than improvise an API from another language's shape, and this
// file is what makes that instruction reachable: without a manifest there is
// nothing to read, and the agent is guessing rather than deciding.
module example.com/entitlements

go 1.22
