# Anti-Patterns (What to Avoid)

### ❌ Avoid: Inline Comments on Arrows

**Wrong:**
```
Frontend -->|"Label"| Backend %% This breaks rendering
```

**Correct:**
```
%% Link 0: Frontend -> Backend
Frontend -->|"Label"| Backend
```

### ❌ Avoid: Low Contrast Colors

**Wrong:**
```
style Node fill:#f0f0f0  # Too light for dark mode
```

**Correct:**
```
style Node fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
```

### ❌ Avoid: Emoji Overload

**Wrong:**
```
Frontend["💻🌐🚀🎨 Frontend Portal"]
```

**Correct:**
```
Frontend["💻 Frontend Portal"]
```

### ❌ Avoid: Everything Is a Rectangle

This is the most common failure mode and the easiest to slip back into out of habit. Shape carries meaning —
defaulting every node to `["Text"]` throws that away even when the colors are otherwise correct.

**Wrong:**
```
Gateway["Load Balancer"]
Service["App Service"]
Store["Database"]
Fn["Notifier Function"]
```

**Correct:**
```
Gateway>"Load Balancer"]
Service["App Service"]
Store[("Database")]
Fn[["Notifier Function"]]
```

See [Node Shape Vocabulary](shapes.md) for the full role-to-shape mapping.

### ❌ Avoid: Crossing Arrows

**Fix with:** Proper connection ordering and subgraph organization

### ❌ Avoid: Inconsistent Arrow Styles

**Fix with:** Use linkStyle with clear documentation

### ❌ Avoid: Long Subgraph Titles

**Wrong:**
```
subgraph Group["This is a very long title with lots of information (and more in parentheses)"]
```

**Correct:**
```
subgraph Group["💿 Persistent Volumes"]
    Node["📁 volume-name<br/>(Details here)"]
end
```

---

