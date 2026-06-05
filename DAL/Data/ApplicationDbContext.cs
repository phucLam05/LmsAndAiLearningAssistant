using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new AuditInterceptor());
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
                
                entity.Property(e => e.UserCode).HasColumnName("user_code").IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.UserCode).IsUnique();

<<<<<<< Updated upstream
                entity.Property(e => e.EmailEncrypt).HasColumnName("email_encrypt").HasMaxLength(255);
                entity.Property(e => e.EmailHash).HasColumnName("email_hash").HasMaxLength(255);
                
                entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                
                entity.Property(e => e.Role).HasColumnName("role").HasConversion<short>().IsRequired();
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<short>().HasDefaultValue(UserStatus.Active);
                
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
=======
                entity.Property(e => e.EmailHash)
                    .IsRequired()
                    .HasMaxLength(255);

                // Add an index to the hashed email for faster lookups during login
                entity.HasIndex(e => e.EmailHash).IsUnique();

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("NOW()"); // PostgreSQL specific current timestamp

                entity.Property(e => e.Role)
                    .HasDefaultValue(UserRole.Student);
>>>>>>> Stashed changes
            });

            // Subject configuration
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subjects");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.SubjectCode).HasColumnName("subject_code").IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.SubjectCode).IsUnique();

                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
                
                entity.Property(e => e.LecturerId).HasColumnName("lecturer_id");
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<short>().HasDefaultValue(SubjectStatus.Active);
                
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(e => e.Lecturer)
                    .WithMany(u => u.AssignedSubjects)
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Updater)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Document configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("documents");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
                
                entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileUrl).HasColumnName("file_url").IsRequired().HasMaxLength(500);
                
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<short>().HasDefaultValue(DocumentStatus.Pending);
                
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Documents)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

<<<<<<< Updated upstream
                entity.HasOne(e => e.Uploader)
=======
                entity.Property(e => e.StoredFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.StoragePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.StorageUrl)
                    .IsRequired();

                entity.Property(e => e.MimeType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FileType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Ignore(e => e.Status);

                entity.Property(e => e.ProcessingStatus)
                    .HasConversion<int>();

                entity.Property(e => e.UploadedAt)
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.ChunkingJobId)
                    .HasColumnType("text");

                entity.Property(e => e.EmbeddingJobId)
                    .HasColumnType("text");

                entity.HasOne(e => e.User)
>>>>>>> Stashed changes
                    .WithMany()
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Updater)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // DocumentChunk configuration
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.ToTable("document_chunks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.DocumentId).HasColumnName("document_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                
                entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index").IsRequired();
                entity.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired();
                
                entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(3072)");
                
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Document)
                    .WithMany(d => d.Chunks)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.DocumentChunks)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
