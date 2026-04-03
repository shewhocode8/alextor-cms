# alextor-cms
<img width="1660" height="825" alt="image" src="https://github.com/user-attachments/assets/9b5319a0-7cb6-4017-8ec6-01a994c23488" />

**ALEXTOR - CASE MANAGEMENT SYSTEM		**						
								
Priority Legend & Scope								
Must Have 	Core system requirement. Cannot launch without it.							
Should Have  	Important for full usability. Deliver in early sprints after core.							
Nice to Have  	Enhances experience. Backlog / post-launch.							
								
Strategic Direction								
								
This is deployed on Huwaei Cloud. All backend All backend services, database (PostgreSQL-compatible RDS), object storage (OBS), API gateway (APIG), secrets management, logging (LTS), and AI inference are provisioned within Huawei Cloud. A new Module 16 — Cloud Infrastructure covers all Huawei Cloud-specific requirements. Impacted existing modules: Auth (IAM), Matters (RDS storage), Settings (cloud config), AI (ModelArts endpoint, vector DB on RDS/DCS).								
								
								
Document Management System - OneDrive Integration								
								
It includes its own DMS capabilities (naming convention enforcement, metadata, access control, indexing, retrieval) with Microsoft 365 OneDrive as the physical file storage layer for the MVP. LawOffice360 is the control layer — it manages folders, naming, metadata, and permissions. Files reside in OneDrive. A new Module 8 — Document Management System covers all DMS requirements in full. Impacted existing modules: Matters (auto-create case folder in OneDrive), App Integrations (Graph API), Settings (DMS configuration), AI (document pipeline from OneDrive).								
								
								
								
Integration Direction - Microsoft Graph API								
								
OneDrive operations are executed via Microsoft Graph API v1.0: folder creation, file upload, metadata, permission management. The integration layer handles throttling, retries, and error queuing. Permission changes in ALEXTOR-CMS are synchronised to OneDrive folder permissions in real time via Graph API. This is covered in Module 8 and Module 11.								
								
								
								
MVP Focus								
								
The MVP scope is: case/matter management, document management (OneDrive), dashboard + tracking, deadlines + tasks, AI drafting and summarisation. Complexity is minimised; speed, usability, and stability are prioritised. MVP-critical requirements are marked Must Have. Nice to Have items are deferred post-launch.								
								
								
								
Security & Privacy by Design								
								
All components follow Privacy and Security by Design. A new Module 17 — Security & Privacy by Design captures cross-cutting security requirements: dual-layer RBAC + case-based access control, immutable audit logging, secure API practices (JWT, input validation, rate limiting), controlled AI access to case data, and privacy principles (minimal data collection, deletion on offboarding). These requirements apply to and are enforced across every other module.								
								
								
								
								
Conversational AI Assistant: Architecture Overview								
The AI Assistant is a deeply integrated, knowledge-base-driven conversational system embedded inside the Case Management System. It is not a generic chatbot — it is a legal intelligence layer that reads, understands, and reasons over the firm's own data: every uploaded document, logged communication, matter record, task, contact, and activity.								
								
How it works — the RAG pipeline								
The system uses Retrieval-Augmented Generation (RAG): before generating any response, the AI retrieves the most relevant content from the firm's knowledge base and passes it as context to the language model. This grounds every answer in actual firm data rather than the model's general training.								
								
The knowledge base has two layers:								
•  Vector database (semantic layer) — Documents, notes, email logs, and unstructured text are chunked, embedded, and stored in a tenant-isolated vector store. The AI performs semantic similarity search to find relevant passages regardless of exact keyword matches.								
•  Live structured data API (real-time layer) — For precise data such as task counts, deadlines, contact fields, or activity logs, the AI issues backend API calls to the live database ensuring answers are always current.								
								
Tenant isolation and security								
Every AI query is strictly scoped to the authenticated user's company_id. Each firm's vector data lives in its own isolated namespace — cross-tenant retrieval is architecturally impossible. RBAC rules are applied before any query: the AI can only return data the current user has permission to see.								
								
Key capabilities added								
•  Legal document interpretation: summarisation, clause extraction, risk identification, document comparison, timeline extraction								
•  Matter Q&A: any natural-language question about a matter answered from live and indexed data								
•  Document generation: draft legal documents from templates, auto-fill from matter data, generate executive summaries								
•  Intelligent search: semantic search across all modules simultaneously with source citations								
•  Analytics assistance: natural-language report generation, productivity insights, workload forecasting								
•  Session management: automatic context detection per module, multi-turn memory, conversation naming								
•  Compliance: immutable AI audit log, hallucination disclaimer, confidentiality mode, private deployment option								

