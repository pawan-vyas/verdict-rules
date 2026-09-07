# Concept: Python Generators, Iterators, and Context Managers (Sync + Async)

Language-runtime mechanics, not architecture — synthetic, but factually accurate to CPython's actual
behavior.

## Diagram 1: Generator/Iterator Execution Lifecycle

```mermaid
flowchart LR
    Caller["🔧 Calling Code"]
    GenObj("⏸️ Generator Object<br/>(created, not yet running)")
    GenBody["⚙️ Generator Body<br/>(runs to next yield)"]
    Done("🏁 StopIteration<br/>(generator exhausted)")

    %% Link 0: Calling the generator function only creates the object -- no body code runs yet
    Caller -->|"[1]<br/>Call gen_func()<br/>(no execution yet)"| GenObj
    %% Link 1: The first next()/for-loop iteration starts the body
    GenObj -->|"[2]<br/>First next()"| GenBody
    %% Link 2: Execution pauses at yield and hands a value back
    GenBody -->|"[3]<br/>yield value"| Caller
    %% Link 3: Each subsequent next() resumes exactly where it paused
    Caller -->|"[4]<br/>next()<br/>(resume)"| GenBody
    %% Link 4: When the function body returns instead of yielding, iteration ends
    GenBody -.->|"[5]<br/>Function Returns<br/>(raises StopIteration)"| Done
    %% Link 5: The caller (a for-loop, typically) catches StopIteration and stops
    Done -.->|"[6]<br/>Caught, Silently<br/>(ends for-loop)"| Caller

    style Caller fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style GenObj fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style GenBody fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Done fill:#D0D0D0,stroke:#A0A0A0,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: Caller -> Generator Object (call, creates only)
    %% 1: Generator Object -> Generator Body (first next())
    %% 2: Generator Body -> Caller (yield value)
    %% 3: Caller -> Generator Body (resume via next())
    %% 4: Generator Body -> Done (function returns -> StopIteration)
    %% 5: Done -> Caller (StopIteration caught, ends iteration)

    linkStyle 0 stroke:#8CE99A,stroke-width:2px
    linkStyle 1 stroke:#FFC078,stroke-width:3px
    linkStyle 2 stroke:#FFC078,stroke-width:3px
    linkStyle 3 stroke:#FFC078,stroke-width:3px
    linkStyle 4 stroke:#A0A0A0,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 5 stroke:#A0A0A0,stroke-width:2px,stroke-dasharray:3 3
```

> **Key Steps**:
> 1. **Calling Creates, It Doesn't Run**: Calling a generator function doesn't execute any of its body —
>    it just produces a generator object, paused before the first line. This is why the object is drawn
>    Rounded (a resulting state) rather than Rectangle (active compute) — nothing is computing yet.
> 2. **Each `next()` Resumes, Not Restarts**: The generator body runs from wherever it last paused at a
>    `yield`, not from the top — local variables, loop counters, and open resources are all still exactly
>    where they were.
> 3. **Exhaustion Is an Exception, Not a Value**: There's no sentinel "last value" — the generator body
>    simply returning (falling off the end, or hitting a bare `return`) is what raises `StopIteration`.
>    That's why this edge is dashed: it's a different *kind* of control transfer (exception-based) than
>    the solid `yield` edges, which are ordinary value returns.
>
> **Design Note**: A `for` loop over a generator is exactly this diagram running in a loop, with the
> `StopIteration` catch built in silently — that's *why* `for` loops can consume a generator without any
> visible exception handling in user code.

## Diagram 2: Context Manager Protocol — `with` vs. `async with`

```mermaid
flowchart TB
    subgraph Sync["🔁 Sync — with ctx() as x:"]
        direction TB
        S_Enter["⚙️ __enter__()"]
        S_Block{"❓ Block Raises?"}
        S_ExitClean["⚙️ __exit__(None, None, None)"]
        S_ExitExc["⚙️ __exit__(exc_type, exc, tb)"]
        S_Suppress{"❓ __exit__<br/>Returns True?"}
        S_Done("✅ Exits Normally")
        S_Propagate("🚫 Exception Propagates")
    end

    subgraph Async["⏳ Async — async with actx() as x:"]
        direction TB
        A_Enter["⚙️ await __aenter__()"]
        A_Block{"❓ Block Raises?"}
        A_ExitClean["⚙️ await __aexit__(None, None, None)"]
        A_ExitExc["⚙️ await __aexit__(exc_type, exc, tb)"]
        A_Suppress{"❓ __aexit__<br/>Returns True?"}
        A_Done("✅ Exits Normally")
        A_Propagate("🚫 Exception Propagates")
    end

    %% Link 0: Sync enter, then the block runs
    S_Enter --> S_Block
    %% Link 1: Sync block ran clean
    S_Block -->|"No"| S_ExitClean
    %% Link 2: Sync block raised
    S_Block -.->|"Yes"| S_ExitExc
    %% Link 3: Clean exit always exits normally
    S_ExitClean --> S_Done
    %% Link 4: Exceptional exit -- does __exit__ swallow it?
    S_ExitExc --> S_Suppress
    %% Link 5: __exit__ suppressed it
    S_Suppress -->|"Yes"| S_Done
    %% Link 6: __exit__ did not suppress it
    S_Suppress -.->|"No"| S_Propagate

    %% Link 7: Async enter, then the block runs
    A_Enter --> A_Block
    %% Link 8: Async block ran clean
    A_Block -->|"No"| A_ExitClean
    %% Link 9: Async block raised
    A_Block -.->|"Yes"| A_ExitExc
    %% Link 10: Clean async exit always exits normally
    A_ExitClean --> A_Done
    %% Link 11: Exceptional async exit -- does __aexit__ swallow it?
    A_ExitExc --> A_Suppress
    %% Link 12: __aexit__ suppressed it
    A_Suppress -->|"Yes"| A_Done
    %% Link 13: __aexit__ did not suppress it
    A_Suppress -.->|"No"| A_Propagate

    style S_Enter fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style S_ExitClean fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style S_ExitExc fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style S_Block fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style S_Suppress fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style S_Done fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style S_Propagate fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000

    style A_Enter fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style A_ExitClean fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style A_ExitExc fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style A_Block fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style A_Suppress fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style A_Done fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style A_Propagate fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000

    linkStyle 0 stroke:#FFC078,stroke-width:2px
    linkStyle 1 stroke:#8CE99A,stroke-width:2px
    linkStyle 2 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 3 stroke:#8CE99A,stroke-width:3px
    linkStyle 4 stroke:#D0BFFF,stroke-width:2px
    linkStyle 5 stroke:#8CE99A,stroke-width:3px
    linkStyle 6 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 7 stroke:#74C0FC,stroke-width:2px
    linkStyle 8 stroke:#8CE99A,stroke-width:2px
    linkStyle 9 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 10 stroke:#8CE99A,stroke-width:3px
    linkStyle 11 stroke:#D0BFFF,stroke-width:2px
    linkStyle 12 stroke:#8CE99A,stroke-width:3px
    linkStyle 13 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
```

> **Design Rationale**: The single most-missed fact about context managers is that `__exit__` (or
> `__aexit__`) runs on **every** path out of the block, exception or not — both lanes above converge
> through an exit call before reaching either terminal state, there's no path that skips it. The second
> most-missed fact: `__exit__`'s **return value** decides whether an exception escapes — returning a
> truthy value suppresses it, anything else (including returning nothing, i.e. `None`) lets it propagate.
> The sync and async lanes are structurally identical on purpose — `async with` doesn't change the
> protocol's logic at all, it only adds an `await` at the two points where the runtime might otherwise
> block (acquiring and releasing whatever resource is being managed, e.g. a network connection or an
> async lock).

## Diagram 3: How `@contextmanager` Bridges Generators and Context Managers

```mermaid
flowchart TB
    Decorated["📦 @contextlib.contextmanager<br/>def resource(): ..."]
    Call["🔧 Call resource(...)"]
    Wrapper("⏸️ Context Manager Object<br/>(wraps a paused generator)")
    WithEnter["⚙️ __enter__()<br/>calls next(gen)"]
    GenTop["⚙️ Generator Runs<br/>Top → first yield"]
    AsValue["📤 Yielded Value<br/>becomes the `as` target"]
    Block["⚙️ with-block Body Runs"]
    NoExc{"❓ Block Raised?"}
    ExitClean["⚙️ __exit__(None,None,None)<br/>calls next(gen) again"]
    ExitExc["⚙️ __exit__(exc,...)<br/>calls gen.throw(exc)"]
    GenFinishes("🏁 Generator Hits StopIteration<br/>(code after yield ran, cleanup done)")
    GenHandles("🛡️ Generator Catches the<br/>Thrown Exception, Returns")
    GenReraises("🚫 Generator Doesn't Catch It<br/>(propagates back out)")

    %% Link 0: Decoration turns the generator function into a context-manager factory
    Decorated -->|"[1]<br/>Decorates"| Call
    %% Link 1: Calling it produces a wrapper, generator not yet running
    Call -->|"[2]<br/>Returns"| Wrapper
    %% Link 2: Entering the with-statement invokes __enter__
    Wrapper -->|"[3]<br/>with ... as x:"| WithEnter
    %% Link 3: __enter__ runs the generator up to its one yield
    WithEnter --> GenTop
    %% Link 4: The yielded value is bound to x
    GenTop --> AsValue
    %% Link 5: The block executes with that value
    AsValue --> Block
    %% Link 6: On exit, did the block raise?
    Block --> NoExc
    %% Link 7: No -- resume the generator past its yield normally
    NoExc -->|"No"| ExitClean
    %% Link 8: Yes -- throw the exception into the generator at the yield point
    NoExc -.->|"Yes"| ExitExc
    %% Link 9: Clean resume should hit the generator's own end (cleanup after yield ran)
    ExitClean --> GenFinishes
    %% Link 10: Thrown exception -- generator's own try/finally around yield can catch it
    ExitExc -.->|"[4]<br/>Exception Delivered<br/>at the paused yield"| GenHandles
    %% Link 11: ...or the generator lets it propagate, unchanged or as a new exception
    ExitExc -.->|"[5]"| GenReraises

    style Decorated fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style Call fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Wrapper fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style WithEnter fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style GenTop fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style AsValue fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style Block fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style NoExc fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style ExitClean fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style ExitExc fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style GenFinishes fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style GenHandles fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style GenReraises fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: Decorated -> Call (decorates)
    %% 1: Call -> Wrapper (returns, not yet running)
    %% 2: Wrapper -> __enter__ (with-statement)
    %% 3: __enter__ -> generator runs to first yield
    %% 4: generator top -> yielded value bound to `as`
    %% 5: as-value -> block runs
    %% 6: block -> did it raise?
    %% 7: no -> clean exit, resume past yield
    %% 8: yes -> throw exception into generator at yield
    %% 9: clean exit -> generator finishes (StopIteration)
    %% 10: exceptional exit -> generator's own handler catches it
    %% 11: exceptional exit -> generator lets it propagate

    linkStyle 0 stroke:#8CE99A,stroke-width:2px
    linkStyle 1 stroke:#FFC078,stroke-width:2px
    linkStyle 2 stroke:#8CE99A,stroke-width:2px
    linkStyle 3 stroke:#FFC078,stroke-width:2px
    linkStyle 4 stroke:#FFC078,stroke-width:2px
    linkStyle 5 stroke:#FFC078,stroke-width:2px
    linkStyle 6 stroke:#FFC078,stroke-width:2px
    linkStyle 7 stroke:#8CE99A,stroke-width:2px
    linkStyle 8 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 9 stroke:#8CE99A,stroke-width:2px
    linkStyle 10 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 11 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
```

> **Design Rationale**: `@contextlib.contextmanager` is not magic — it's a thin wrapper that literally
> drives the underlying generator with `next()` and `.throw()`. The code *before* the generator's `yield`
> becomes `__enter__`; the code *after* the `yield` becomes `__exit__`. Critically, when the `with`-block
> raises, that exception is delivered by **throwing it into the generator at the exact point of the
> paused `yield`** — which is why wrapping a `try/finally` (or `try/except`) *around* the `yield` inside
> the generator is the correct, idiomatic way to guarantee cleanup runs on both the happy path and the
> error path. `contextlib.asynccontextmanager` follows the identical logic for `async def` generator
> functions, using `.athrow()` instead of `.throw()` and awaiting each step.
