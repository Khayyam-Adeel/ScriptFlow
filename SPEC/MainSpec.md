
Problem Statment

A prescribing platform must send electronic prescriptions to an external pharmacy gateway that is slow and unreliable, while prescribers watch statuses update in real time.

Functional Requirments:

    At least three services: a Prescription API (ASP.NET Core), a Dispatch worker (background service integrating with a mock pharmacy gateway you also build — it must randomly delay, ack, nack, and drop messages), and a Notification service.

    • Reliable messaging between services (queue of your choice) with idempotent handlers, retries with backoff, and a poison-message/dead-letter path.

    • Prescription lifecycle as an explicit state machine: created → signed → dispatched → acknowledged / rejected / expired, including repeat prescriptions.

    • Angular frontend: create and repeat prescriptions, live status board (SignalR or polling), rejection handling workflow.

    • Performance chapter: seed 1M+ prescription rows in SQL Server and deliver three reporting queries (e.g. dispensing volumes, rejection rates by pharmacy, repeat-due lists) with before/after execution plans showing your index and query optimisations.

    • Structured logging and correlation IDs across all services

    • Database Must be in SQL and can be refernced from DatabaseSpec.md file

 Folder stracture has been in place Using N-Layer Artitacture and Event driven Artitature 
 Application must be in .net C# platform and all related components 
 folders are neatly arrenged in Each Folder name corresponds to its functionality

 
Acceptance Criteria
 Application should be able to create a prescription with minimum information.
 
 