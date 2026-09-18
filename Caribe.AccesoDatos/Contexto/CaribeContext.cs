using System.Text.Json;
using Caribe.AccesoDatos.Identidad;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Caribe.AccesoDatos.Contexto;

/// <summary>
/// Contexto unico de la aplicacion.
///
/// Hereda de IdentityDbContext para traer las tablas de usuarios,
/// roles y tokens sin escribir configuracion.
///
/// No hay repositorio generico ni unidad de trabajo: DbSet ya es un
/// repositorio y DbContext ya es una unidad de trabajo. Envolverlos
/// esconderia las capacidades de EF Core —proyecciones, AsNoTracking,
/// ExecuteUpdate— y obligaria a un metodo por cada consulta.
/// </summary>
public class CaribeContext : IdentityDbContext<AppUser>
{
    public CaribeContext(DbContextOptions<CaribeContext> options)
        : base(options) { }

    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Modelo> Modelos => Set<Modelo>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<VehiculoFoto> VehiculoFotos => Set<VehiculoFoto>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<SolicitudNota> SolicitudNotas => Set<SolicitudNota>();
    public DbSet<Invitacion> Invitaciones => Set<Invitacion>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<Auditoria> Auditoria => Set<Auditoria>();
    public DbSet<EstadisticaHistorica> EstadisticasHistoricas => Set<EstadisticaHistorica>();
    public DbSet<ConfiguracionFinanciamiento> ConfiguracionFinanciamiento
        => Set<ConfiguracionFinanciamiento>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Obligatorio y primero: sin esto Identity no configura sus
        // tablas y la migracion sale incompleta.
        base.OnModelCreating(b);

        ConfigurarIdentity(b);
        ConfigurarCatalogo(b);
        ConfigurarVehiculos(b);
        ConfigurarSolicitudes(b);
        ConfigurarSeguridad(b);
        ConfigurarAuditoria(b);
        ConfigurarFinanciamiento(b);
    }

    // ══════════════════ IDENTITY ══════════════════

    private static void ConfigurarIdentity(ModelBuilder b)
    {
        // Nombres en el mismo estilo que el resto: sin esto quedarian
        // como AspNetUsers, AspNetRoles y demas.
        b.Entity<AppUser>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.NombreCompleto).HasMaxLength(120).IsRequired();
            e.Property(x => x.InvitadoPorId).HasMaxLength(450);
        });

        b.Entity<IdentityRole>().ToTable("roles");
        b.Entity<IdentityUserRole<string>>().ToTable("usuario_roles");
        b.Entity<IdentityUserClaim<string>>().ToTable("usuario_claims");
        b.Entity<IdentityUserLogin<string>>().ToTable("usuario_logins");
        b.Entity<IdentityUserToken<string>>().ToTable("usuario_tokens");
        b.Entity<IdentityRoleClaim<string>>().ToTable("rol_claims");
    }

    // ══════════════════ CATALOGO ══════════════════

    private static void ConfigurarCatalogo(ModelBuilder b)
    {
        b.Entity<Marca>(e =>
        {
            e.ToTable("marcas");
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();

            // Dos marcas con el mismo nombre confundirian al publicar.
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        b.Entity<Modelo>(e =>
        {
            e.ToTable("modelos");
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();

            // El nombre se repite entre marcas: hay un Civic de Honda
            // y podria haber otro de otra marca. Lo unico es el par.
            e.HasIndex(x => new { x.MarcaId, x.Nombre }).IsUnique();

            e.HasOne(x => x.Marca)
             .WithMany(m => m.Modelos)
             .HasForeignKey(x => x.MarcaId)
             // Restrict y no Cascade: borrar una marca no debe arrastrar
             // sus modelos, porque hay vehiculos que los referencian.
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ══════════════════ VEHICULOS ══════════════════

    private static void ConfigurarVehiculos(ModelBuilder b)
    {
        b.Entity<Vehiculo>(e =>
        {
            e.ToTable("vehiculos");

            e.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            e.Property(x => x.Color).HasMaxLength(40);
            e.Property(x => x.CreadoPorId).HasMaxLength(450);

            e.Property(x => x.Estado).HasConversion<short>();
            e.Property(x => x.Transmision).HasConversion<short>();
            e.Property(x => x.Combustible).HasConversion<short>();
            e.Property(x => x.Traccion).HasConversion<short>();

            // 12 enteros y 2 decimales: alcanza para cualquier vehiculo
            // y evita los errores de redondeo del punto flotante.
            e.Property(x => x.CostoVehiculo).HasPrecision(12, 2);
            e.Property(x => x.CostoFlete).HasPrecision(12, 2);
            e.Property(x => x.CostoImpuestos).HasPrecision(12, 2);
            e.Property(x => x.CostoTramites).HasPrecision(12, 2);
            e.Property(x => x.Honorario).HasPrecision(12, 2);
            e.Property(x => x.PrecioPublicado).HasPrecision(12, 2);

            e.Property(x => x.VigenciaDias).HasDefaultValue((short)7);
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
            e.Property(x => x.ActualizadoEn).HasDefaultValueSql("now()");

            e.HasIndex(x => x.Slug).IsUnique();

            // Indice parcial: solo los visibles al publico. El catalogo
            // consulta miles de veces al dia y nunca mira los borradores
            // ni los archivados, asi que no tiene sentido indexarlos.
            e.HasIndex(x => new { x.Estado, x.PublicadoEn })
             .HasFilter("estado IN (1, 2)")
             .IsDescending(false, true);

            // Para los filtros del catalogo.
            e.HasIndex(x => new { x.MarcaId, x.ModeloId, x.PrecioPublicado });

            e.HasOne(x => x.Marca)
             .WithMany()
             .HasForeignKey(x => x.MarcaId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Modelo)
             .WithMany()
             .HasForeignKey(x => x.ModeloId)
             .OnDelete(DeleteBehavior.Restrict);

            // Calculadas en C#: no son columnas.
            e.Ignore(x => x.CostoTotal);
            e.Ignore(x => x.Margen);
            e.Ignore(x => x.EsVisibleAlPublico);
        });

        b.Entity<VehiculoFoto>(e =>
        {
            e.ToTable("vehiculo_fotos");

            e.Property(x => x.Url).HasMaxLength(400).IsRequired();
            e.Property(x => x.UrlThumb).HasMaxLength(400).IsRequired();
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            e.HasIndex(x => new { x.VehiculoId, x.Orden });

            // Indice unico PARCIAL: impide dos portadas del mismo
            // vehiculo, pero permite muchas fotos normales. Un unique
            // comun sobre VehiculoId solo dejaria una foto por vehiculo.
            e.HasIndex(x => x.VehiculoId)
             .IsUnique()
             .HasFilter("es_portada = true");

            e.HasOne(x => x.Vehiculo)
             .WithMany(v => v.Fotos)
             .HasForeignKey(x => x.VehiculoId)
             // Cascade: las fotos no tienen sentido sin su vehiculo.
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ══════════════════ SOLICITUDES ══════════════════

    private static void ConfigurarSolicitudes(ModelBuilder b)
    {
        b.Entity<Solicitud>(e =>
        {
            e.ToTable("solicitudes");

            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Whatsapp).HasMaxLength(20).IsRequired();
            e.Property(x => x.MarcaTexto).HasMaxLength(60);
            e.Property(x => x.ModeloTexto).HasMaxLength(80);
            e.Property(x => x.AsignadaAId).HasMaxLength(450);
            e.Property(x => x.IpOrigen).HasMaxLength(45);

            e.Property(x => x.Estado).HasConversion<short>();
            e.Property(x => x.Origen).HasConversion<short>();
            e.Property(x => x.FormaPago).HasConversion<short>();
            e.Property(x => x.Transmision).HasConversion<short?>();
            e.Property(x => x.Combustible).HasConversion<short?>();

            e.Property(x => x.PresupuestoMin).HasPrecision(12, 2);
            e.Property(x => x.PresupuestoMax).HasPrecision(12, 2);

            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            // Indice parcial de la bandeja: las archivadas no se
            // consultan a diario y con los meses son la mayoria.
            e.HasIndex(x => new { x.Estado, x.CreadoEn })
             .HasFilter("archivada_en IS NULL")
             .IsDescending(false, true);

            // Para el anti duplicado: mismo telefono en poco tiempo.
            e.HasIndex(x => new { x.Whatsapp, x.CreadoEn });

            e.HasOne(x => x.Vehiculo)
             .WithMany()
             .HasForeignKey(x => x.VehiculoId)
             // SetNull y no Cascade: si se borra el vehiculo, la
             // solicitud sobrevive. Es historial comercial y alimenta
             // las estadisticas de conversion.
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<SolicitudNota>(e =>
        {
            e.ToTable("solicitud_notas");

            e.Property(x => x.Nota).IsRequired();
            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            e.HasIndex(x => new { x.SolicitudId, x.CreadoEn })
             .IsDescending(false, true);

            e.HasOne(x => x.Solicitud)
             .WithMany(s => s.Notas)
             .HasForeignKey(x => x.SolicitudId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EstadisticaHistorica>(e =>
        {
            e.ToTable("estadisticas_historicas");

            // Una fila por mes: al purgar se acumula sobre la existente.
            e.HasIndex(x => new { x.Anio, x.Mes }).IsUnique();

            e.Property(x => x.ActualizadoEn).HasDefaultValueSql("now()");
        });
    }

    // ══════════════════ SEGURIDAD ══════════════════

    private static void ConfigurarSeguridad(ModelBuilder b)
    {
        b.Entity<Invitacion>(e =>
        {
            e.ToTable("invitaciones");

            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.InvitadoPorId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Rol).HasConversion<short>();

            // SHA-256 en hexadecimal: 64 caracteres exactos.
            e.Property(x => x.TokenHash)
             .HasMaxLength(64).IsFixedLength().IsRequired();

            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            e.HasIndex(x => x.TokenHash).IsUnique();

            // Solo las pendientes: las usadas y revocadas se acumulan
            // y no se consultan.
            e.HasIndex(x => x.Email)
             .HasFilter("usada_en IS NULL AND revocada_en IS NULL");
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");

            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TokenHash)
             .HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.ReemplazadoPor).HasMaxLength(64);
            e.Property(x => x.Ip).HasMaxLength(45);

            e.HasIndex(x => x.TokenHash).IsUnique();

            // Solo las sesiones vivas. Los tokens vencidos se acumulan
            // con los meses y no hace falta indexarlos.
            e.HasIndex(x => new { x.UsuarioId, x.ExpiraEn })
             .HasFilter("revocado_en IS NULL");
        });

        b.Entity<TokenRecuperacion>(e =>
        {
            e.ToTable("tokens_recuperacion");

            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TokenHash)
             .HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.IpSolicitud).HasMaxLength(45);

            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            e.HasIndex(x => x.TokenHash).IsUnique();

            // Para detectar abuso: muchas solicitudes seguidas desde
            // la misma cuenta o la misma IP.
            e.HasIndex(x => new { x.UsuarioId, x.CreadoEn })
             .IsDescending(false, true);
        });
    }

    // ══════════════════ AUDITORIA ══════════════════

    private static void ConfigurarAuditoria(ModelBuilder b)
    {
        b.Entity<Auditoria>(e =>
        {
            e.ToTable("auditoria");

            e.Property(x => x.Entidad).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntidadId).HasMaxLength(50).IsRequired();
            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Ip).HasMaxLength(45);
            e.Property(x => x.Accion).HasConversion<short>();

            // jsonb y no json: permite indexar y consultar dentro del
            // documento, y ocupa menos espacio.
            e.Property(x => x.DatosAntes).HasColumnType("jsonb");
            e.Property(x => x.DatosDespues).HasColumnType("jsonb");

            e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

            // "Quien cambio este precio y cuando" es la consulta que
            // justifica esta tabla.
            e.HasIndex(x => new { x.Entidad, x.EntidadId, x.CreadoEn })
             .IsDescending(false, false, true);

            e.HasIndex(x => new { x.UsuarioId, x.CreadoEn })
             .IsDescending(false, true);
        });
    }

    // ══════════════════ FINANCIAMIENTO ══════════════════

    private static void ConfigurarFinanciamiento(ModelBuilder b)
    {
        b.Entity<ConfiguracionFinanciamiento>(e =>
        {
            e.ToTable("configuracion_financiamiento");

            e.Property(x => x.PorcentajePrima).HasPrecision(5, 2);
            e.Property(x => x.TasaAnual).HasPrecision(5, 2);
            e.Property(x => x.PlazosDisponibles).HasMaxLength(60);
            e.Property(x => x.ActualizadoPorId).HasMaxLength(450);
            e.Property(x => x.ActualizadoEn).HasDefaultValueSql("now()");

            e.Ignore(x => x.EstaOperativo);

            // Fila unica creada con la migracion: el sistema siempre
            // tiene una configuracion que leer, aunque este apagada.
            e.HasData(new ConfiguracionFinanciamiento
            {
                Id = 1,
                Activo = false,
                PorcentajePrima = 50m,
                TasaAnual = 0m,
                PlazoMinimoMeses = 12,
                PlazoMaximoMeses = 36,
                PlazosDisponibles = "12,24,36",
                ActualizadoEn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        });
    }

    // ══════════════════ AUDITORIA: AYUDA ══════════════════

    /// <summary>
    /// Registra una operacion. Se llama antes de SaveChanges y la
    /// entrada se guarda en la misma transaccion: si la operacion
    /// falla, tampoco queda el registro de auditoria.
    /// </summary>
    public void Registrar(
        string entidad,
        object entidadId,
        AccionAuditoria accion,
        string usuarioId,
        object? antes = null,
        object? despues = null,
        string? ip = null)
    {
        Auditoria.Add(new Auditoria
        {
            Entidad = entidad,
            EntidadId = entidadId.ToString() ?? string.Empty,
            Accion = accion,
            UsuarioId = usuarioId,
            Ip = ip,
            DatosAntes = antes is null ? null : JsonSerializer.SerializeToDocument(antes),
            DatosDespues = despues is null ? null : JsonSerializer.SerializeToDocument(despues),
            CreadoEn = DateTimeOffset.UtcNow
        });
    }
}
