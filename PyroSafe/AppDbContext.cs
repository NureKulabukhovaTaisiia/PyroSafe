using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Zone> Zones { get; set; }
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Scenario> Scenarios { get; set; }
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Для SQLite bool -> INTEGER
        modelBuilder.Entity<User>()
            .Property(u => u.UserRole)
            .HasConversion<int>();

        modelBuilder.Entity<Scenario>()
            .Property(s => s.IsActive)
            .HasConversion<int>();

        modelBuilder.Entity<Scenario>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Event>()
            .Property(e => e.EventTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Event>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

// -------------------- Zone --------------------
public class Zone
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Required, MaxLength(100)]
    public string ZoneName { get; set; }

    [Required]
    public int Floor { get; set; }

    public double Area { get; set; }  // SQLite REAL

    public ICollection<Sensor> Sensors { get; set; }
}

// -------------------- Sensor --------------------
public class Sensor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Required, MaxLength(100)]
    public string SensorName { get; set; }

    [Required, MaxLength(50)]
    public string SensorValue { get; set; }

    [Required, MaxLength(20)]
    public string SensorType { get; set; }

    [Required]
    public int ZoneID { get; set; }

    [ForeignKey("ZoneID")]
    public Zone Zone { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public ICollection<Event> Events { get; set; }
}

// -------------------- User --------------------
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; }

    public bool UserRole { get; set; } = false;

    [MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [Required, MaxLength(255)]
    public string Password { get; set; }

    public ICollection<Event> ResolvedEvents { get; set; }
}

// -------------------- Scenario --------------------
public class Scenario
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Required, MaxLength(20)]
    public string ScenarioType { get; set; }

    [MaxLength(255)]
    public string Description { get; set; }

    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Event> Events { get; set; }
}

// -------------------- Event --------------------
public class Event
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Required]
    public int SensorID { get; set; }

    [ForeignKey("SensorID")]
    public Sensor Sensor { get; set; }

    public DateTime EventTime { get; set; }

    public int? ScenarioID { get; set; }

    [ForeignKey("ScenarioID")]
    public Scenario Scenario { get; set; }

    [MaxLength(20)]
    public string Severity { get; set; } = "Low";

    [MaxLength(20)]
    public string Status { get; set; } = "New";

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedBy { get; set; }

    [ForeignKey("ResolvedBy")]
    public User ResolvedUser { get; set; }

    [Required, MaxLength(255)]
    public string Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
