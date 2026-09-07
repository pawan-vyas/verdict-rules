# Link Indexing & Documentation

### Why Index Links?

- Easy maintenance
- Quick debugging
- Clear styling mapping
- Future updates

### Comment Structure

**Option 1: Separate Comment Lines (Recommended - Mermaid Compatible)**

```
%% Link 0: Frontend -> Backend (HTTP REST)
Frontend -->|"HTTP REST API"| Backend
%% Link 1: Frontend -> Proxy (WebSocket)
Frontend -->|"WebSocket"| Proxy
```

**Option 2: Grouped Comments**

```
%% Application Layer Connections (Links 0-4)
Frontend -->|"HTTP"| Backend
Frontend -->|"WS"| Proxy
Backend -->|"DB"| Database
```

### LinkStyle Documentation

**Always include descriptive comments:**

```
%% Styling for arrows by connection type
%% Application Layer Connections (bold, 3px)
linkStyle 0 stroke:#4A9EFF,stroke-width:3px
linkStyle 1 stroke:#FF6B6B,stroke-width:3px

%% Azure Service Connections (lighter shades, 2px)
linkStyle 5 stroke:#74C0FC,stroke-width:2px

%% Infrastructure (dashed)
linkStyle 8 stroke:#8CE99A,stroke-width:2px,stroke-dasharray:5 5
```

**Note**: Never add inline comments after linkStyle statements (e.g., `%% description`) as this breaks Mermaid parsing. Use separate comment lines above instead.

### Link Index Reference Template

Create a comment block at the end listing all links:

```
%% Link Index:
%% 0: Frontend -> Backend (HTTP REST)
%% 1: Frontend -> Proxy (WebSocket)
%% 2: Backend -> Database (CRUD)
%% 3: Backend -> Monitoring (Traces)
%% ... etc
```

---

