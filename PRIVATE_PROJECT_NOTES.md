# PRIVATE PROJECT NOTES - LMS AI Learning Assistant

> File nay chi de ban va Codex doc lai khi lam tiep. Khong dua len GitHub.
> Cap nhat lan gan nhat: 2026-06-03.

## 1. Hien trang project hien tai

### Kien truc

Project dang theo mo hinh 4 tang kha ro:

- `Core`: Entities, DTOs, constants.
- `DAL`: `ApplicationDbContext`, repositories, EF Core migrations.
- `BLL`: Services va interfaces, noi chua business logic.
- `PL`: MVC controllers, views, view models, wwwroot.

Day la diem manh. Khi them feature moi nen tiep tuc giu rule:

- Controller chi nhan request, validate co ban, lay current user, goi service.
- Business logic nam o BLL.
- Database logic nam o DAL/repository.
- Entity/DTO nam o Core.

### Authentication hien tai

- Da co Register/Login/Logout bang cookie auth.
- Login dang set cookie persistent 7 ngay, nen khi chay lai app van vao dung tai khoan cu la hanh vi co chu dich, khong phai bug.
- Neu can demo dang nhap lai moi lan thi logout hoac clear cookie browser.
- Chua co forgot password/reset password.
- Chua co email verification.

### Upload file / Supabase Storage

Da co flow upload:

- User phai dang nhap moi vao `/Document` va `/Document/Upload`.
- Upload file len Supabase Storage bucket `documents`.
- Metadata luu vao DB bang `Documents`.
- File goc khong luu trong DB.
- File tren storage dung ten GUID, tranh trung.
- Co rollback: upload Supabase thanh cong nhung luu DB loi thi co gang xoa file vua upload.
- Moi document gan `UserId`, list/delete query theo current user.
- Da mo rong type upload: pdf, doc/docx, ppt/pptx, xls/xlsx, txt/csv/md/rtf/json/xml, image, audio, video, zip/rar/7z/tar/gz.
- Gioi han upload hien tai: 50MB.

Han che hien tai cua upload:

- Chua co chon mon hoc/chuong hoc khi upload.
- Chua co validation tai lieu co dung mon hay khong.
- Chua co man xem file goc tu Supabase bang signed URL.
- Chua co man xem noi dung da extract/chunk/embed.
- Validate file moi dua tren extension + browser MIME, chua kiem tra magic bytes/file signature.
- Cho upload archive/media la dung yeu cau linh hoat, nhung neu sau nay extract tu zip/rar thi can antivirus/scan va sandbox xu ly.

### RAG / Embedding

Da co nen tang:

- `DocumentChunk` entity co `Embedding` dang `pgvector`.
- PostgreSQL migration co extension vector.
- `Document.Status` co cac status nhu uploaded/processing/indexed/failed.

Chua co:

- Service extract text tu PDF/DOCX/PPTX.
- `DocumentPage` entity/table.
- Chunking service.
- Embedding service.
- Vector search service.
- Chatbot hoi dap dua tren chunk.
- Citation/nguon tai lieu goc.

### Role va Subscription

Hien tai da bat dau co code cho:

- `UserRoles`: Student, Lecturer, Admin.
- `SubscriptionPlans`: Free, Basic, Premium.
- `User` da co `Role`, `SubscriptionPlan`, `SubscriptionUpdatedAt`.
- Register cho chon role public: Student hoac Lecturer. Admin khong cho tu dang ky.
- Subscription flow co controller/view/service de doi Free/Basic/Premium.
- Migration `AddUserRoleSubscription` da duoc tao.

Can kiem tra tiep:

- Build lai sau khi doi role/subscription.
- Apply migration len Supabase.
- Test register user moi.
- Test login user cu sau khi migration them default value.
- Test trang `/Subscription`.
- Kiem tra claim `Role` va `SubscriptionPlan` co cap nhat dung sau khi doi goi.

## 2. Yeu cau/thay nhan xet can dua vao thiet ke

### 2.1 Admin/truong tao account cho sinh vien bang Excel

Can them workflow rieng cho Admin:

1. Admin upload file Excel danh sach sinh vien.
2. He thong parse Excel.
3. Validate cot bat buoc, vi du:
   - StudentCode
   - FullName
   - Email
   - Class/Group
   - Subject/Course neu co
4. Tao account hang loat:
   - Role = Student.
   - SubscriptionPlan = Free mac dinh.
   - Password tam thoi random.
   - EmailHash/EmailEncrypt nhu flow auth hien tai.
5. Gui email ve tung sinh vien:
   - Account/email dang nhap.
   - Mat khau tam thoi.
   - Link dang nhap.
   - Khuyen nghi doi mat khau lan dau.
6. Luu audit log import:
   - Ai import.
   - File nao.
   - Bao nhieu dong thanh cong/that bai.
   - Loi tung dong.

Can tang/lop:

- Core: `StudentImportDto`, `StudentImportResultDto`, co the them `UserImportBatch` entity neu can audit.
- DAL: repository user/import batch.
- BLL: `IUserImportService`, `UserImportService`, `IEmailService`.
- PL: Admin controller + view upload Excel.

Thu vien doc Excel co the dung:

- ClosedXML hoac EPPlus.

Chu y:

- Khong gui password plain text ve sau nay nua; password tam thoi chi gui lan dau.
- Nen bat user doi password lan dau: can field `MustChangePassword`.

### 2.2 Email va quen mat khau

Can them email workflow:

- SMTP config trong appsettings/env, khong hardcode.
- `IEmailService` trong BLL.
- Forgot password:
  1. User nhap email.
  2. He thong tao reset token.
  3. Gui link reset ve email dang ky.
  4. Token co expiry.
  5. Reset password thanh cong thi invalidate token.

Can entity co the them:

- `PasswordResetToken`
  - Id
  - UserId
  - TokenHash
  - ExpiresAt
  - UsedAt
  - CreatedAt

Viec doi email:

- Sinh vien co the doi email trong profile.
- Truong/Admin co the thay email neu co quyen.
- Doi email nen gui verification den email moi.

### 2.3 Upload theo mon hoc/chuong hoc va giao vien/leader mon

Thay co nhac: khi upload cho chon mon, upload cua giao vien danh cho leader mon.

Can thiet ke lai upload them Subject/Course:

- `Subject`
  - Id
  - Code
  - Name
  - CreatedAt
- `Chapter`
  - Id
  - SubjectId
  - Name
  - OrderIndex
  - CreatedAt
- `SubjectMember` hoac `TeacherSubjectAssignment`
  - SubjectId
  - UserId
  - RoleInSubject: Lecturer, Leader, Student

Quyen nghiep vu de xuat:

- Admin: tao subject, gan lecturer/leader/student.
- Lecturer: upload tai lieu cho mon duoc gan.
- Subject Leader: approve/reject tai lieu cua mon.
- Student: xem/chat voi tai lieu cua mon minh duoc gan.

Upload can them:

- Chon Subject.
- Chon Chapter optional.
- Kiem tra user co quyen upload subject do khong.
- Metadata `Document` can co `SubjectId`, `ChapterId`, `ApprovedBy`, `ApprovedAt`, `ApprovalStatus`.

### 2.4 Kiem tra tai lieu co dung mon khong

Co nhieu muc do:

MVP de demo:

- Lecturer chon mon khi upload.
- Subject Leader review va approve/reject.
- He thong hien warning neu ten file/noi dung extract khong co keyword lien quan den mon.

Ban nang cao:

- Sau extract text, chay classifier/LLM de so sanh:
  - Ten mon.
  - Mo ta mon.
  - Keyword/syllabus.
  - Noi dung tai lieu.
- Tao diem confidence.
- Neu confidence thap thi set status `needs_review`.

Can them status:

- uploaded
- processing
- needs_review
- approved
- indexed
- failed

### 2.5 Xem tai lieu goc va tai lieu da embed

Can co 2 man hinh:

1. Original file viewer:
   - Private Supabase bucket nen khong public URL.
   - Backend tao signed URL ngan han.
   - View PDF/image/video trong browser neu ho tro.
   - DOCX/PPTX co the download hoac preview qua converter sau.

2. Embedded/indexed view:
   - Hien Document status.
   - Hien extracted pages.
   - Hien chunks.
   - Hien embedding status.
   - Hien loi processing neu failed.

Can them BLL:

- `IDocumentViewService`
- `GetSignedOriginalUrlAsync(documentId, userId)`
- `GetDocumentChunksAsync(documentId, userId)`

Can them DAL:

- `DocumentPageRepository`
- `DocumentChunkRepository`

## 3. Ba workflow nen chot cho ASM 1

De bam yeu cau thầy, nen chot it nhat 3 main flows:

### Workflow 1: Quan ly tai lieu hoc tap

Trang thai hien tai: da co phan upload co ban.

Can hoan thien:

- Upload file.
- Chon mon/chuong.
- Kiem tra quyen upload.
- Xem danh sach tai lieu.
- Xem file goc.
- Xem status uploaded/processing/indexed/failed.
- Leader approve/reject neu co.

### Workflow 2: Account/Admin/Subscription

Trang thai hien tai: auth co, subscription dang co code co ban.

Can hoan thien:

- Admin import student tu Excel.
- Gui email account/password tam thoi.
- Forgot password/reset password.
- Role: Student/Lecturer/Admin.
- Subscription: Free/Basic/Premium.
- Authorization theo role va goi.

### Workflow 3: Chatbot/RAG hoi dap tai lieu

Trang thai hien tai: moi co UI preview, chua co backend.

Can hoan thien MVP:

- Chat session.
- Chat message.
- User hoi cau hoi.
- Search chunks theo vector hoac fallback keyword.
- Tra loi co citation/source.
- Luu lich su hoi dap.

Neu chua kip AI that:

- Co the lam flow MVC/BLL/DAL that, response tam la mock/fallback keyword, nhung database va UI dung.

## 4. Research/experiment flow de phu hop de tai

Yeu cau trong anh co nghien cuu/thuc nghiem so sanh model.

Nen co trang `/Research` hoac `/Benchmark`:

- Chon dataset/test set.
- Chon model embedding.
- Chon chunking strategy.
- Chay benchmark.
- Luu ket qua:
  - Accuracy/faithfulness/context precision neu dung RAGAS.
  - Latency.
  - Cost estimate.
  - Notes.

Entity goi y:

- `ExperimentRun`
  - Id
  - UserId
  - Name
  - EmbeddingModel
  - ChunkingStrategy
  - CreatedAt
- `ExperimentMetric`
  - Id
  - ExperimentRunId
  - MetricName
  - Value

## 5. Cac thieu sot/rui ro can nho

### Bao mat

- `PL/appsettings.Development.json` dang chua secret that va da nam trong `.gitignore`; tuyet doi khong push.
- Service Role Key chi duoc dung backend.
- Neu key tung bi lo trong chat/log/shared screen, nen rotate key Supabase.
- File upload nhieu dinh dang can can than voi archive va executable disguised file.
- Nen block `.exe`, `.bat`, `.cmd`, `.ps1`, `.dll`, `.msi` tru khi co ly do rat ro.

### Upload

- MIME tu browser khong du tin cay.
- Nen them magic-byte validation cho cac loai file chinh: PDF, ZIP/DOCX/PPTX/XLSX, PNG, JPG.
- Nen co virus scanning neu cho upload archive.
- Nen co signed URL thay vi public bucket.

### Database

- Supabase schema nen de EF migration quan ly, tranh tao bang tay snake_case neu code dang dung PascalCase.
- Sau khi them role/subscription can chay migration len Supabase.
- Neu DB da co user cu, migration can default:
  - Role = Student
  - SubscriptionPlan = Free

### UX

- Homepage da co chat preview nhung chua phai chat that.
- Upload page da dep hon, nhung can them subject/chapter dropdown sau.
- Can co trang detail document.

## 6. Thu tu uu tien nen lam tiep

1. Build lai va apply migration role/subscription.
2. Test Register/Login/Subscription.
3. Them Subject/Chapter/SubjectMember theo dung layer.
4. Sua upload de chon Subject/Chapter va check quyen Lecturer/Leader.
5. Them signed URL xem file goc.
6. Them Document detail de xem chunks/status.
7. Them Admin import Excel sinh vien.
8. Them EmailService va forgot password.
9. Them Chat session/message flow.
10. Them extract/chunk/embed pipeline.
11. Them Research/Benchmark dashboard.

## 7. Ghi chu ve Git

File nay phai nam trong `.gitignore`.
Chi push code/migration/doc public, khong push file nay va khong push appsettings local.
