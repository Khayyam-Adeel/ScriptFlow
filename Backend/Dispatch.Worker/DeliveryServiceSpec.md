Dispatch Worker

Specification:

Purpose
Consumes prescription events and dispatches prescriptions to pharmacies.

Responsibilities
Receive Queue Message
Dispatch
Retry
Dead Letter Queue
Publish Acknowledgement


Functional Requirements:

FR-001
Consume PrescriptionSigned
FR-002
Call Pharmacy
FR-003
Retry
FR-004
Idempotency
FR-005
Dead Letter Queue
FR-006
Publish Acknowledgement

Non Functional: 
High Reliability
Retry
Backoff
Logging
Correlation IDs
Inputs
RabbitMQ
PrescriptionSigned
Outputs
PrescriptionAcknowledged
PrescriptionRejected

Design: 
Contains
Consumer Diagram
Retry Flow
Sequence Diagram
RabbitMQ Topology
Idempotency Design
Class Diagram
Error Flow
Background Service Flow