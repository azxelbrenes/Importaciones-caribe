using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Caribe.AccesoDatos.Contexto;
using Caribe.AccesoDatos.Identidad;
using Caribe.Api.Middleware;
using Caribe.LogicaNegocio.Almacenamiento;
using Caribe.LogicaNegocio.Correo;
using Caribe.LogicaNegocio.Implementaciones;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.LogicaNegocio.Seguridad;
using Caribe.LogicaNegocio.Validadores;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Kestrel anuncia "Server: Kestrel" en cada respuesta. Es informacion
// gratis para quien busca vulnerabilidades de una version concreta.
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);


// ═════════════════════ SERVICIOS ═════════════════════

// ── Base de datos ──
builder.Services.AddDbContext<CaribeContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
     .UseSnakeCaseNamingConvention();

    if (builder.Environment.IsDevelopment())
    {
        // Muestra los valores de los parametros en el log. Util para
        // depurar, inaceptable en produccion: incluiria contrasenas.
        o.EnableSensitiveDataLogging();
        o.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

// ── Identity ──
builder.Services.AddIdentityCore<AppUser>(o =>
{
    o.Password.RequiredLength = 10;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.User.RequireUniqueEmail = true;

    // Bloqueo tras 5 fallos. Aplica tambien a los codigos de doble
    // factor incorrectos: sin eso, alguien con la contrasena podria
    // probar codigos de seis digitos sin limite.
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<CaribeContext>()
.AddDefaultTokenProviders();

// ── JWT ──
builder.Services.Configure<JwtOpciones>(
    builder.Configuration.GetSection(JwtOpciones.Seccion));

var jwt = builder.Configuration
    .GetSection(JwtOpciones.Seccion).Get<JwtOpciones>()
    ?? throw new InvalidOperationException("Falta la sección Jwt en la configuración.");

// Fallar al arrancar es mejor que arrancar con una llave vacia: con
// una llave vacia el sistema funcionaria, pero cualquiera podria
// firmar tokens validos.
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key falta o es muy corta. Configurala con user-secrets " +
        "en desarrollo o con la variable JWT_KEY en el servidor.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt.Issuer,
            ValidAudience            = jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwt.Key)),

            // Por defecto .NET tolera 5 minutos de desfase. Para
            // tokens de 15 minutos, eso es un tercio de su vida
            // regalado.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Almacenamiento ──
builder.Services.Configure<AlmacenamientoOpciones>(
    builder.Configuration.GetSection(AlmacenamientoOpciones.Seccion));

builder.Services.Configure<R2Opciones>(
    builder.Configuration.GetSection(R2Opciones.Seccion));

var r2 = builder.Configuration
    .GetSection(R2Opciones.Seccion).Get<R2Opciones>() ?? new R2Opciones();

// Se elige por configuracion, no por entorno: asi se puede probar R2
// en local antes de desplegar, y caer a disco si algo falla.
if (r2.EstaConfigurado)
{
    builder.Services.AddSingleton<IAmazonS3>(_ =>
        new AmazonS3Client(
            new BasicAWSCredentials(r2.AccessKey, r2.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = r2.Endpoint,

                // R2 usa rutas en vez de subdominios por bucket. Sin
                // esto, el SDK armaria direcciones que no existen.
                ForcePathStyle = true,

                // R2 ignora la region pero el SDK exige una.
                AuthenticationRegion = "auto"
            }));

    builder.Services.AddScoped<IAlmacenamiento, AlmacenamientoR2>();
}
else
{
    builder.Services.AddScoped<IAlmacenamiento, AlmacenamientoLocal>();
}

// ── Correo ──
builder.Services.Configure<CorreoOpciones>(
    builder.Configuration.GetSection(CorreoOpciones.Seccion));

var correo = builder.Configuration
    .GetSection(CorreoOpciones.Seccion).Get<CorreoOpciones>() ?? new CorreoOpciones();

if (correo.EstaConfigurado)
    builder.Services.AddHttpClient<ICorreoService, CorreoResend>();
else
    builder.Services.AddScoped<ICorreoService, CorreoConsola>();

// ── Logica de negocio ──
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IVehiculoLN, VehiculoLN>();
builder.Services.AddScoped<ICatalogoLN, CatalogoLN>();
builder.Services.AddScoped<ISolicitudLN, SolicitudLN>();
builder.Services.AddScoped<IEstadisticaLN, EstadisticaLN>();
builder.Services.AddScoped<IFotoLN, FotoLN>();
builder.Services.AddScoped<IFinanciamientoLN, FinanciamientoLN>();
builder.Services.AddScoped<IUsuarioLN, UsuarioLN>();

// ── Validaciones ──
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CrearSolicitudValidator>();

// ── API ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ──
const string PoliticaCors = "AngularLocal";

builder.Services.AddCors(o => o.AddPolicy(PoliticaCors, p => p
    .WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Necesario para que viaje la cookie del refresh token.
    .AllowCredentials()));

// ── Limite de tasa ──
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login y formularios publicos: la puerta mas expuesta.
    o.AddFixedWindowLimiter("formularios", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    // Recuperacion de contrasena: mas estricto todavia. Sin esto,
    // alguien podria llenarle el buzon a otra persona.
    o.AddFixedWindowLimiter("recuperacion", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 3;
        opt.QueueLimit = 0;
    });

    o.AddFixedWindowLimiter("general", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 120;
        opt.QueueLimit = 0;
    });
});


var app = builder.Build();


// ═════════════════════ TUBERIA ═════════════════════
// El orden importa: cada middleware envuelve a los que vienen despues.

// Primero, para atrapar lo que falle mas abajo.
app.UseMiddleware<ManejadorExcepciones>();

// Segundo: las cabeceras deben aplicarse a TODAS las respuestas,
// incluidas las de error que genera el middleware anterior.
app.UseMiddleware<CabecerasSeguridad>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(PoliticaCors);
app.UseRateLimiter();

// Autenticacion SIEMPRE antes de autorizacion: primero se sabe quien
// sos, despues si podes. Invertido, todo [Authorize] daria 401.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Para el monitoreo y la verificacion del despliegue.
app.MapGet("/health", () => Results.Ok(new
{
    estado = "ok",
    hora = DateTimeOffset.UtcNow
}));

// Aplica migraciones pendientes y crea roles + superadministrador.
await SeedInicial.EjecutarAsync(app.Services);

app.Run();
