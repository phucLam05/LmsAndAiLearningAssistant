-- 1. USERS TABLE
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_code VARCHAR(50) UNIQUE NOT NULL, -- Student ID or Lecturer ID
    email_encrypt VARCHAR(255), -- Symmetrically encrypted email
    email_hash VARCHAR(255), -- Hashed email for fast exact-match querying
    full_name VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role SMALLINT NOT NULL, -- 0: ADMIN, 1: LECTURER, 2: STUDENT
    status SMALLINT DEFAULT 1, -- 1: ACTIVE, 0: INACTIVE
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
);

-- 2. SUBJECTS TABLE
CREATE TABLE subjects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_code VARCHAR(50) UNIQUE NOT NULL, -- e.g., INT101
    name VARCHAR(255) NOT NULL,
    description TEXT,
    lecturer_id UUID REFERENCES users(id), -- The ONLY Lecturer assigned to this subject
    status SMALLINT DEFAULT 1, -- 1: ACTIVE, 0: INACTIVE
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_by UUID REFERENCES users(id)
);

-- 3. DOCUMENTS TABLE (Knowledge Base Metadata)
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_id UUID REFERENCES subjects(id) ON DELETE CASCADE,
    uploaded_by UUID REFERENCES users(id), -- Should match subjects.lecturer_id
    file_name VARCHAR(255) NOT NULL,
    file_url VARCHAR(500) NOT NULL, -- S3/Local path
    status SMALLINT DEFAULT 0, -- 0: PENDING, 1: PROCESSING, 2: SUCCESS, -1: FAILED
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_by UUID REFERENCES users(id) -- User who changed status or re-uploaded
);

-- 4. DOCUMENT CHUNKS & VECTOR EMBEDDINGS (The core of RAG)
CREATE TABLE document_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) ON DELETE CASCADE,
    subject_id UUID REFERENCES subjects(id) ON DELETE CASCADE, -- Denormalized for faster RAG filtering
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding vector(1536), 
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);