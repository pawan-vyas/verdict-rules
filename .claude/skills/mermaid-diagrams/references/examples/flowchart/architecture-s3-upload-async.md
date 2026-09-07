# Example: Direct S3 Upload with Async Processing

**Demonstrates**: Parentheses notation `(N)` with proper spacing, presigned URL pattern, async processing triggers, event-driven workflows, Stadium for the external browser, Cylinder for every real data store (SQL database, job queue, S3 bucket, SNS topic), and Subroutine for the Lambda function (a named, invoked callable unit) rather than a plain Rectangle for all four.

```mermaid
graph TB
    subgraph Client["💻 Client Tier"]
        direction TB
        Browser(["🌐 Web Browser<br/>(Angular/JS)"])
    end
    
    subgraph Backend["⚙️ Backend Tier (.NET)"]
        direction TB
        API["🔧 Web API<br/>(.NET 8/9)<br/>Port: 443"]
        Database[("🗄️ SQL Database<br/>(User mappings)")]
        ProcessQueue[("📬 Processing Queue<br/>(Job queue)")]
    end
    
    subgraph AWS["☁️ AWS Services"]
        direction TB
        S3[("📦 S3 Bucket<br/>(File storage)")]
        IAM["🔐 IAM<br/>(Credentials)"]
        Lambda[["⚡ Lambda<br/>(Processor)"]]
        SNS[("📢 SNS<br/>(Notifications)")]
    end
    
    %% Link 0: Browser -> API (Init upload)
    Browser -->|" (1)<br/>Request Upload<br/>(POST /init-upload)"| API
    %% Link 1: API -> Database (Create record)
    API -->|" (2)<br/>Create Record<br/>(userId, filename)"| Database
    %% Link 2: API -> IAM (Get credentials)
    API -->|" (3)<br/>Generate Presigned<br/>(AWS SDK)"| IAM
    %% Link 3: API -> Browser (Return presigned)
    API -->|" (4)<br/>Return Presigned URL<br/>(JSON response)"| Browser
    %% Link 4: Browser -> S3 (Upload file)
    Browser -->|" (5)<br/>Upload File<br/>(Direct POST/PUT)"| S3
    %% Link 5: Browser -> API (Confirm)
    Browser -->|" (6)<br/>Confirm Upload<br/>(POST /complete)"| API
    %% Link 6: API -> S3 (Verify)
    API -->|" (7)<br/>Verify Exists<br/>(HeadObject)"| S3
    %% Link 7: API -> Database (Update status)
    API -->|" (8)<br/>Update Status<br/>(completed)"| Database
    
    %% Link 8: API -> ProcessQueue (Queue async job)
    API -.->|" (9)<br/>Queue Job<br/>(Background)"| ProcessQueue
    %% Link 9: S3 -> Lambda (Event trigger)
    S3 -.->|" (10)<br/>Async Trigger<br/>(On Upload)"| Lambda
    %% Link 10: Lambda -> SNS (Publish notification)
    Lambda -.->|" (11)<br/>Pub/Sub<br/>(Notification)"| SNS
    
    %% Link Index:
    %% 0: Browser -> API (init upload)
    %% 1: API -> Database (create record)
    %% 2: API -> IAM (generate presigned)
    %% 3: API -> Browser (return presigned URL)
    %% 4: Browser -> S3 (upload file, direct)
    %% 5: Browser -> API (confirm upload)
    %% 6: API -> S3 (verify exists)
    %% 7: API -> Database (update status)
    %% 8: API -> ProcessQueue (queue async job)
    %% 9: S3 -> Lambda (event trigger, async)
    %% 10: Lambda -> SNS (publish notification, async)

    %% Styling for service nodes
    style Browser fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style API fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Database fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style ProcessQueue fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000
    style S3 fill:#FF9F43,stroke:#E88422,stroke-width:2px,color:#000
    style IAM fill:#74C0FC,stroke:#4DABF7,stroke-width:2px,color:#000
    style Lambda fill:#FFA94D,stroke:#FF8C00,stroke-width:2px,color:#000
    style SNS fill:#FF8787,stroke:#E64545,stroke-width:2px,color:#000
    
    %% Styling for arrows
    %% Synchronous operations (bold, 3px)
    linkStyle 0 stroke:#4A9EFF,stroke-width:3px
    linkStyle 1 stroke:#51CF66,stroke-width:3px
    linkStyle 3 stroke:#FFB84D,stroke-width:3px
    linkStyle 4 stroke:#FF9F43,stroke-width:3px
    linkStyle 5 stroke:#4A9EFF,stroke-width:3px
    linkStyle 7 stroke:#51CF66,stroke-width:3px
    
    %% AWS Service Connections (lighter, 2px)
    linkStyle 2 stroke:#74C0FC,stroke-width:2px
    linkStyle 6 stroke:#FFC078,stroke-width:2px
    
    %% Asynchronous/Event-driven (dotted, 2px)
    linkStyle 8 stroke:#FF6B6B,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 9 stroke:#FFA94D,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 10 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
```

> **Direct S3 Upload Flow**:
> 1. **Initiate Upload**: Browser requests upload permissions via REST API
> 2. **Create Mapping**: API creates user-file mapping record in database for tracking
> 3. **Generate Credentials**: API requests presigned URL from AWS IAM for temporary S3 access
> 4. **Return URL**: API returns presigned URL to browser enabling direct upload
> 5. **Direct Upload**: Browser uploads file directly to S3 using presigned URL (bypassing backend)
> 6. **Confirm Upload**: Browser notifies API that upload completed
> 7. **Verify Upload**: API verifies file exists in S3 bucket using HeadObject call
> 8. **Update Status**: API marks upload as completed in database
>
> **Async Processing Flow**:
> 9. **Queue Job**: API queues async job for background processing (non-blocking)
> 10. **Event Trigger**: S3 automatically triggers Lambda function when file uploaded (event-driven)
> 11. **Publish Notification**: Lambda publishes notification to SNS for pub/sub subscribers
>
> **Design Pattern**: This demonstrates the presigned URL pattern for large file uploads, reducing backend load by enabling direct client-to-S3 communication. The backend only handles coordination and verification, not data transfer. Async processing (dotted arrows) shows event-driven workflows that don't block the upload flow.

---

