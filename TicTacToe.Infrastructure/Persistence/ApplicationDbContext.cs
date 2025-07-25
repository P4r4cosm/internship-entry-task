using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Определяем таблицы в базе данных
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Move> Moves => Set<Move>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        
        builder.Entity<Game>().HasKey(g => g.Id);

        // Настраиваем связь "один-ко-многим" с ходами
        builder.Entity<Game>().HasMany(g => g.Moves)
            .WithOne() // У хода нет навигационного свойства назад к игре
            .HasForeignKey("GameId") // EF Core сам найдет это свойство в Move
            .IsRequired();
               
        // Указываем EF Core, что наш enum должен храниться в БД как строка
        builder.Entity<Game>().Property(g => g.Status)
            .HasConversion<string>();
        
        builder.Ignore("_board");
        
        
        builder.Entity<Move>().HasKey(m => m.Id);
        
        // На уровне базы данных запрещаем два хода в одну и ту же клетку одной игры
        builder.Entity<Move>().HasIndex("GameId", nameof(Move.Row), nameof(Move.Column)).IsUnique();
        
        base.OnModelCreating(builder);
    }
}