# Example: Multi-Tier Web Application

**Demonstrates**: Bracket notation `[N]`, synchronous solid arrows, asynchronous dotted arrows, hierarchical subgraphs, proper color palette, link indexing, and Cylinder for every genuine data store (database, secrets vault, blob storage, event/message queues, persistent volumes) rather than defaulting them to Rectangle alongside the compute services.

```mermaid
graph TB
    subgraph ACI["☁️ Azure Container Instance"]
        direction TB
        
        subgraph Frontend_Layer["🌐 Frontend Tier"]
            Frontend["💻 Frontend Portal<br/>(nginx)<br/>Port: 80/443"]
        end
        
        subgraph App_Layer["⚙️ Application Tier"]
            Backend["🔧 Backend API<br/>(.NET 9)<br/>Port: 8080"]
            Proxy["🔌 WebSocket Proxy<br/>(Node.js)<br/>Port: 5002"]
        end
        
        subgraph Data_Layer["💾 Data & Services Tier"]
            Database[("🗄️ Database<br/>(PostgreSQL)<br/>Port: 5432")]
            Monitor["📊 Monitoring<br/>(Prometheus)<br/>Port: 9090"]
        end
        
        %% Link 0: Frontend -> Backend (HTTP REST)
        Frontend -->|"[1]<br/>HTTP REST API<br/>(CRUD Operations)"| Backend
        %% Link 1: Frontend -> Proxy (WebSocket)
        Frontend -->|"[2]<br/>WebSocket<br/>(Real-time Data)"| Proxy
        %% Link 2: Backend -> Database (CRUD)
        Backend -->|"[3]<br/>SQL Queries<br/>(CRUD)"| Database
        %% Link 3: Backend -> Monitor (Metrics)
        Backend -->|"[4]<br/>Metrics Export<br/>(Prometheus)"| Monitor
    end
    
    subgraph Azure["🔷 Azure Supporting Services"]
        KeyVault[("🔐 Azure Key Vault<br/>(Secrets)")]
        Storage[("☁️ Azure Blob Storage<br/>(Files)")]
        EventHub[("📡 Azure Event Hub<br/>(Event Streaming)")]
        ServiceBus[("📬 Azure Service Bus<br/>(Messaging)")]
    end
    
    subgraph Volumes["💿 Persistent Volumes"]
        DBVol[("📁 database_data<br/>(DB persistence)")]
        AppVol[("📁 app_data<br/>(Application data)")]
    end
    
    %% Link 4: Backend -> KeyVault (Secrets)
    Backend -->|"[5]<br/>Read Secrets<br/>(Managed Identity)"| KeyVault
    %% Link 5: Backend -> Storage (Files)
    Backend -->|"[6]<br/>Download Files<br/>(HTTP)"| Storage
    
    %% Link 6: Backend -> EventHub (Async event publishing)
    Backend -.->|"[7]<br/>Event Publish<br/>(Fire & Forget)"| EventHub
    %% Link 7: Backend -> ServiceBus (Async messaging)
    Backend -.->|"[8]<br/>Queue Message<br/>(Async)"| ServiceBus
    
    %% Link 8: Database -> DBVol (Volume mount - dashed)
    Database -.->|"[9]<br/>Volume Mount<br/>(Persistence)"| DBVol
    %% Link 9: Backend -> AppVol (Volume mount - solid, direct dependency)
    Backend -->|"[10]<br/>Volume Mount<br/>(App Data)"| AppVol
    
    %% Styling for service nodes
    style Frontend fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style Backend fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Proxy fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000
    style Database fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Monitor fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style KeyVault fill:#74C0FC,stroke:#4DABF7,stroke-width:2px,color:#000
    style Storage fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style EventHub fill:#FF8787,stroke:#E64545,stroke-width:2px,color:#000
    style ServiceBus fill:#FF8787,stroke:#E64545,stroke-width:2px,color:#000
    style DBVol fill:#8CE99A,stroke:#37B24D,stroke-width:2px,color:#000
    style AppVol fill:#FFC078,stroke:#E69500,stroke-width:2px,color:#000
    
    %% Link Index:
    %% 0: Frontend -> Backend (HTTP REST)
    %% 1: Frontend -> Proxy (WebSocket)
    %% 2: Backend -> Database (SQL CRUD)
    %% 3: Backend -> Monitor (metrics export)
    %% 4: Backend -> KeyVault (read secrets)
    %% 5: Backend -> Storage (download files)
    %% 6: Backend -> EventHub (publish event, async)
    %% 7: Backend -> ServiceBus (queue message, async)
    %% 8: Database -> DBVol (volume mount, infra)
    %% 9: Backend -> AppVol (volume mount, direct)
    
    %% Styling for arrows by connection type
    %% Application Layer (bold, 3px)
    linkStyle 0 stroke:#4A9EFF,stroke-width:3px
    linkStyle 1 stroke:#FF6B6B,stroke-width:3px
    linkStyle 2 stroke:#51CF66,stroke-width:3px
    linkStyle 3 stroke:#B47EFF,stroke-width:3px
    
    %% Azure Services - Synchronous (lighter shades, 2px)
    linkStyle 4 stroke:#74C0FC,stroke-width:2px
    linkStyle 5 stroke:#96E6B3,stroke-width:2px
    
    %% Azure Services - Asynchronous (dotted, 2px)
    linkStyle 6 stroke:#FFA94D,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 7 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    
    %% Volume Mounts (service color shades)
    linkStyle 8 stroke:#8CE99A,stroke-width:2px,stroke-dasharray:5 5
    linkStyle 9 stroke:#FFC078,stroke-width:2px
```

> **Data Flow**:
> 1. **Frontend Requests**: User interacts with web portal, sending HTTP REST requests for CRUD operations and establishing WebSocket for real-time updates
> 2. **Backend Processing**: API server queries PostgreSQL database for data operations and exports performance metrics to Prometheus
> 3. **Azure Integration**: Backend retrieves secrets from Key Vault using Managed Identity and downloads files from Blob Storage
> 4. **Event-Driven Operations**: Backend publishes events to Event Hub and queues messages to Service Bus asynchronously (non-blocking)
> 5. **Persistent Storage**: Database and backend mount volumes for data persistence and application state
>
> **Architecture Notes**: Three-tier design with clear separation between presentation (Frontend), business logic (Backend/Proxy), and data services (Database/Monitor). Azure services provide secure secrets management, file storage, and event streaming. Asynchronous dotted arrows show fire-and-forget operations that don't block the main request flow.

---

