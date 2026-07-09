The Prescription API manages the lifecycle of prescriptions from creation to dispatch. Also it create user login sign-up functionality , create patient/Provider profiles 
It provides REST APIs for the Angular frontend and publishes events to RabbitMQ.
Prescription must have unique SCID which consists of first latter as 9 then 5 Digit of EPSEntityno i.e J0BGV then 5 Alphanumaric characters

PreRequisites:
MEdications containing Medicine name ,SCTID, FORM , Duration, take vlaue, frequency, Quantity and direction
Medication must have medicine data mapped with MEdicine
Patient Profile should include Firstname, last name , Address, NHI, 
Provider Profile shoudl include Firstname , lastname , Type i.e Doctor, nurse, student , NZMC no
Practice location from where provider is Accessing the system must have Healt point identitfier HPI no. FZZ99-B Follow the pattern
 
Scope
 
✔ Create Prescription
✔ Prescripton can have 1 or mutiple medications
✔ Update Prescription
✔ Repeat Prescription
✔ Sign Prescription
✔ Retrieve Prescription 
✔ Publish Events
✖ Does NOT send notifications
✖ Does NOT contact pharmacies
 
Functional requirments :
FR-001
Doctor can create prescription.
-------------------------
FR-002
Doctor can sign prescription. or system should create a prescription
------------------------ 
FR-003
System shall validate patient.
-------------------------
FR-004
System shall publish PrescriptionSigned / Prescription created event.
Event should update prescription status as created> Pending, sent or failed, and dispened

-------------------------
FR-005
System shall store prescription.
 


Non-Functional Requirments :
 
Response time should be < 500ms
-----------------
JWT Authentication
-----------------
Structured Logging
----------------
Correlation ID
-----------------
REST API
-----------------
JSON Responses
 
Input/output:
/Prescription  
201 created


Event publish:
PrescriptionCreated
PrescriptionSigned
PrescriptionRepeated


Error handling:
400 Validation
401 Unauthorized
404 Patient Missing
409 Invalid State
 
 
Architecture:
Controller > Application > Domain > Infrastructure

Folder structure:
Class Diagram
DTO Diagram
Sequence Diagram
Database Diagram
State Machine
API Endpoints
Dependency Diagram
Exception Flow
Logging Flow