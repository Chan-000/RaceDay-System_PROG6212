using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext: DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        // Tables(DbSets)
        public DbSet<User> Users { get; set; }
        public DbSet<EventType> EventTypes {  get; set; }
        public DbSet<Event> Events {  get; set; }
        public DbSet<Category> Categories {  get; set; }
        public DbSet<Enrolment> Enrolments {  get; set; }
        public DbSet<Result> Results {  get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", table =>
                {
                    table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
                });
                entity.HasKey(u => u.UserID);
                entity.Property(u => u.FirstName)
                     .HasMaxLength(50).IsRequired();

                entity.Property(u => u.LastName)
                     .HasMaxLength(50) .IsRequired();

                entity.Property(u => u.Email)
                     .HasMaxLength(100) .IsRequired();

                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.PasswordHash) 
                     .HasMaxLength(255) .IsRequired();

                entity.Property(u => u.Role)
                     .HasMaxLength(20) .IsRequired();

                entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            });

            // EventTypes
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("EventType");
                entity.HasKey(e => e.EventTypeID);

                entity.Property(e => e.TypeName)
                     .HasMaxLength(50).IsRequired();

                entity.Property(e => e.Description) .HasMaxLength(255);
                entity.HasIndex(e => e.TypeName).IsUnique();
            });

            //Events
            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events", table =>
                {
                    table.HasCheckConstraint("CK_Events_DistanceKm", "[Distance] > 0");
                    table.HasCheckConstraint("CK_Events_MaxParticipants", "[MaxParticipants] > 0");
                });

                entity.HasKey(e => e.EventID);

                entity.Property(e => e.EventName)
                     .HasMaxLength(150).IsRequired();

                entity.Property(e => e.Description)
                     .HasMaxLength(500);

                entity.Property(e => e.EventDate)
                     .HasColumnType("date");

                entity.Property(e => e.Location)
                     .HasMaxLength(180).IsRequired();

                entity.Property(e => e.DistanceKm)
                     .HasColumnType("decimal(6,2)");

                entity.HasOne(e => e.EventType)
                     .WithMany(e => e.Events)
                     .HasForeignKey(e => e.EventTypeID)
                     .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Organiser)
                     .WithMany()
                     .HasForeignKey(e => e.OrganiserID)
                     .OnDelete(DeleteBehavior.NoAction);
            });

            //Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category", table =>
                {
                    table.HasCheckConstraint("CK_Category_AgeRange", "[AgeMin] IS NULL OR [AgeMax] IS NULL OR [AgeMin] <= [AgeMax]");
                    table.HasCheckConstraint("CK_Category_EntryFee", "[EntryFee} >= 0");
                });

                entity.HasKey(c => c.CategoryID);

                entity.Property(c => c.CategoryName)
                     .HasMaxLength(100).IsRequired();

                entity.Property(c => c.EntryFee)
                     .HasColumnType("decimal(10,2)").HasDefaultValue(0m);

                entity.HasOne(c => c.Event)
                      .WithMany(e => e.Categories)
                      .HasForeignKey(c => c.EventID)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            //Enrolment
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolment", table =>
                {
                    table.HasCheckConstraint("CK_Enrolment_Status", "[EnrolmentStatus] IN ('Confirmed', 'Cancelled')");
                });

                entity.HasKey(e => e.EnrolmentID);

                entity.Property(e => e.EnrolmentDate)
                     .HasColumnType("date").HasDefaultValueSql("CAST(GETDATE() As date");

                entity.Property(e => e.EnrolmentStatus)
                     .HasMaxLength(20) .HasDefaultValue("Confirmed") .IsRequired();

                entity.HasIndex(e => new { e.ParticipantID, e.EventID }).IsUnique();

                entity.HasOne(e => e.Participant)
                      .WithMany()
                      .HasForeignKey(e => e.ParticipantID)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Event)
                      .WithMany(ev => ev.Enrolments)
                      .HasForeignKey(e => e.EventID)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(e => e.CategoryID)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            //Result 
            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Results");
                entity.HasKey(r => r.ResultID);

                entity.Property(r => r.FinishTime)
                     .HasColumnType("time");

                entity.HasIndex(r => r.EnrolmentID).IsUnique();

                entity.HasOne(r => r.Enrolment)
                      .WithOne(e => e.Result)
                      .HasForeignKey<Result>(r => r.EnrolmentID)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

    }
}
