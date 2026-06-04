# PROJECT CONTEXT & ARCHITECTURE: UNIVERSITY RAG CHATBOT SYSTEM

## 1. System Overview
This is an Internal Enterprise System for a University. It provides a RAG-based Chatbot (Retrieval-Augmented Generation) to help students study. 
- **Architecture Constraint:** Clean Architecture / 3-Tier.
- **Database:** PostgreSQL used for BOTH Relational Data AND Vector Embeddings (via `pgvector` extension). No third-party vector databases.
- **Auth Model:** Strict RBAC. No self-registration. All identities managed by Admin. First-time login requires mandatory password change (check by status is 0).
- **Audit Trail:** Core entities must track the last modification time (`updated_at`) and the user who made the modification (`updated_by`).

## 2. Roles & Permissions
1. ADMIN (Role = 0): Manages Users, Subjects, and assigns Subjects to Lecturers.
2. LECTURER (Role = 1): Can only view assigned Subjects. Uploads documents strictly to assigned Subjects.
3. STUDENT (Role = 2): Selects a Subject to chat. AI context MUST be strictly filtered by the selected `subject_id`.

## 3. Database Schema (PostgreSQL + pgvector)
Agent task: Use the following SQL script to design the Database Entities, ORM models (e.g., Entity Framework), and Repositories.
Notice: Map all `SMALLINT` status and role columns to Enums in the application code. Implement automatic interceptors/middlewares in the ORM to automatically populate `updated_at` and `updated_by` on update operations.