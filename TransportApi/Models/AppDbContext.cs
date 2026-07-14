using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TransportApi.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Card> Cards { get; set; }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<GoalType> GoalTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }

    public virtual DbSet<RentalPlan> RentalPlans { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportResponse> ReportResponses { get; set; }

    public virtual DbSet<RewardType> RewardTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RouteHistory> RouteHistories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UsersResult> UsersResults { get; set; }

    public virtual DbSet<VehicleStatus> VehicleStatuses { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    public virtual DbSet<Zone> Zones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Card");

            entity.HasIndex(e => e.CardNumber, "card_number").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CardNumber).HasColumnName("card_number");
            entity.Property(e => e.CvvCode)
                .HasMaxLength(4)
                .HasColumnName("CVV_code");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Competition");

            entity.HasIndex(e => e.VehicleTypeId, "Vehicle_TypeID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.EndDate).HasColumnName("End_date");
            entity.Property(e => e.GoalValue).HasColumnName("Goal_value");
            entity.Property(e => e.StartDate).HasColumnName("Start_date");
            entity.Property(e => e.VehicleTypeId).HasColumnName("Vehicle_TypeID");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.VehicleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Competition_ibfk_1");
        });

        modelBuilder.Entity<GoalType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Goal_type");

            entity.HasIndex(e => e.CompetitionId, "CompetitionID");

            entity.HasIndex(e => e.Name, "Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CompetitionId).HasColumnName("CompetitionID");

            entity.HasOne(d => d.Competition).WithMany(p => p.GoalTypes)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Goal_type_ibfk_1");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.UserId, "UserID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Status).HasMaxLength(255);
            entity.Property(e => e.Text).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Notifications_ibfk_1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.RentalId, "RentalID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Amount).HasPrecision(15, 2);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Created_At");
            entity.Property(e => e.RentalId).HasColumnName("RentalID");
            entity.Property(e => e.Status).HasMaxLength(255);

            entity.HasOne(d => d.Rental).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RentalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Payment_ibfk_1");
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Rental");

            entity.HasIndex(e => e.RentalPlanId, "Rental_PlanID");

            entity.HasIndex(e => e.UserId, "UserID");

            entity.HasIndex(e => e.VehicleId, "VehicleID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Distance)
                .HasPrecision(7, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.DistanceEnd).HasColumnName("Distance_end");
            entity.Property(e => e.DistanceStart).HasColumnName("Distance_Start");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp")
                .HasColumnName("End_time");
            entity.Property(e => e.RentalPlanId).HasColumnName("Rental_PlanID");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp")
                .HasColumnName("Start_time");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.RentalPlan).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.RentalPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Rental_ibfk_3");

            entity.HasOne(d => d.User).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Rental_ibfk_1");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.Rental)
                .HasForeignKey<Rental>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Rental_ibfk_2");
        });

        modelBuilder.Entity<RentalPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Rental_Plan");

            entity.HasIndex(e => e.VehicleTypeId, "Vehicle_TypeID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description).HasMaxLength(15);
            entity.Property(e => e.Plan).HasMaxLength(255);
            entity.Property(e => e.Price).HasPrecision(15, 2);
            entity.Property(e => e.VehicleTypeId).HasColumnName("Vehicle_TypeID");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.RentalPlans)
                .HasForeignKey(d => d.VehicleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Rental_Plan_ibfk_1");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Report");

            entity.HasIndex(e => e.UserId, "UserID");

            entity.HasIndex(e => e.VehicleId, "VehicleID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Created_At");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(255);
            entity.Property(e => e.Text).HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Report_ibfk_1");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Reports)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("Report_ibfk_2");
        });

        modelBuilder.Entity<ReportResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Report_response");

            entity.HasIndex(e => e.ReportId, "ReportID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.ResponseTo)
                .HasMaxLength(255)
                .HasColumnName("Response_to");
            entity.Property(e => e.Text).HasMaxLength(500);

            entity.HasOne(d => d.Report).WithMany(p => p.ReportResponses)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Report_response_ibfk_1");
        });

        modelBuilder.Entity<RewardType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Reward_type");

            entity.HasIndex(e => e.CompetitionId, "CompetitionID");

            entity.HasIndex(e => e.Unit, "Unit").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CompetitionId).HasColumnName("CompetitionID");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.Competition).WithMany(p => p.RewardTypes)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Reward_type_ibfk_1");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "Role_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("Role_name");
        });

        modelBuilder.Entity<RouteHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Route_History");

            entity.HasIndex(e => e.RentalId, "RentalID");

            entity.HasIndex(e => e.VehicleId, "VehicleID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BatteryLevel).HasColumnName("Battery_level");
            entity.Property(e => e.PositionX)
                .HasPrecision(9, 6)
                .HasColumnName("Position_X");
            entity.Property(e => e.PositionY)
                .HasPrecision(9, 6)
                .HasColumnName("Position_Y");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("Recorded_at");
            entity.Property(e => e.RentalId).HasColumnName("RentalID");
            entity.Property(e => e.Speed)
                .HasPrecision(6, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.Rental).WithMany(p => p.RouteHistories)
                .HasForeignKey(d => d.RentalId)
                .HasConstraintName("Route_History_ibfk_2");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.RouteHistories)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Route_History_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("User");

            entity.HasIndex(e => e.CardId, "CardID");

            entity.HasIndex(e => e.Email, "Email").IsUnique();

            entity.HasIndex(e => e.ResetLink, "Reset_link").IsUnique();

            entity.HasIndex(e => e.RoleId, "RoleID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BlockedReason)
                .HasColumnType("text")
                .HasColumnName("Blocked_reason");
            entity.Property(e => e.CardId).HasColumnName("CardID");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Created_at");
            entity.Property(e => e.IsBlocked).HasColumnName("Is_Blocked");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.OustandingBalances)
                .HasPrecision(15, 2)
                .HasColumnName("Oustanding_balances");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("Password_hash");
            entity.Property(e => e.ResetLink)
                .HasMaxLength(500)
                .HasColumnName("Reset_link");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Updated_at");

            entity.HasOne(d => d.Card).WithMany(p => p.Users)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("User_ibfk_2");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_ibfk_1");
        });

        modelBuilder.Entity<UsersResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Users_result");

            entity.HasIndex(e => e.CompetitionId, "CompetitionID");

            entity.HasIndex(e => e.PaymentId, "PaymentID").IsUnique();

            entity.HasIndex(e => e.UserId, "UserID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CompetitionId).HasColumnName("CompetitionID");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.RewardAmount).HasColumnName("Reward_amount");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Competition).WithMany(p => p.UsersResults)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Users_result_ibfk_3");

            entity.HasOne(d => d.Payment).WithOne(p => p.UsersResult)
                .HasForeignKey<UsersResult>(d => d.PaymentId)
                .HasConstraintName("Users_result_ibfk_2");

            entity.HasOne(d => d.User).WithOne(p => p.UsersResult)
                .HasForeignKey<UsersResult>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Users_result_ibfk_1");
        });

        modelBuilder.Entity<VehicleStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Vehicle_Status");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Vehicle");

            entity.HasIndex(e => e.QrCode, "QR_code").IsUnique();

            entity.HasIndex(e => e.VehicleStatusId, "Vehicle_StatusID");

            entity.HasIndex(e => e.VehicleTypeId, "Vehicle_TypeID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BatteryCapacity).HasColumnName("Battery_capacity");
            entity.Property(e => e.BatteryLevel).HasColumnName("Battery_level");
            entity.Property(e => e.ElectricityConsumption).HasColumnName("Electricity_consumption");
            entity.Property(e => e.LastActivity)
                .HasColumnType("timestamp")
                .HasColumnName("Last_activity");
            entity.Property(e => e.Model).HasMaxLength(255);
            entity.Property(e => e.PositionX)
                .HasPrecision(9, 6)
                .HasColumnName("Position_X");
            entity.Property(e => e.PositionY)
                .HasPrecision(9, 6)
                .HasColumnName("Position_Y");
            entity.Property(e => e.QrCode).HasColumnName("QR_code");
            entity.Property(e => e.ScanTime)
                .HasColumnType("timestamp")
                .HasColumnName("Scan_time");
            entity.Property(e => e.VehicleStatusId).HasColumnName("Vehicle_StatusID");
            entity.Property(e => e.VehicleTypeId).HasColumnName("Vehicle_TypeID");

            entity.HasOne(d => d.VehicleStatus).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.VehicleStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Vehicle_ibfk_2");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.VehicleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Vehicle_ibfk_1");
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Vehicle_Type");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Zone");

            entity.HasIndex(e => e.VehicleTypeId, "Vehicle_TypeID");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Coordinates).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Created_at");
            entity.Property(e => e.IsRestrictedArea).HasColumnName("Is_Restricted_area");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("Updated_at");
            entity.Property(e => e.VehicleTypeId).HasColumnName("Vehicle_TypeID");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Zones)
                .HasForeignKey(d => d.VehicleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Zone_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}