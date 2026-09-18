using Caribe.AccesoDatos.Contexto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caribe.AccesoDatos.Identidad;

public static class SeedInicial
{
    /// <summary>
    /// Aplica migraciones pendientes, crea los tres roles y el
    /// superadministrador si no existe.
    ///
    /// La contrasena inicial se lee de configuracion, nunca del
    /// codigo: una contrasena escrita aqui viviria en el repositorio
    /// publico para siempre.
    /// </summary>
    public static async Task EjecutarAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var s = scope.ServiceProvider;

        var db          = s.GetRequiredService<CaribeContext>();
        var userManager = s.GetRequiredService<UserManager<AppUser>>();
        var roleManager = s.GetRequiredService<RoleManager<IdentityRole>>();
        var config      = s.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        foreach (var rol in Roles.Todos)
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

        var email = config["Seed:AdminEmail"];
        var pass  = config["Seed:AdminPassword"];

        // Sin configuracion no se siembra nada. Es lo correcto: en un
        // despliegue donde falten esas variables, es mejor que no haya
        // administrador a que haya uno con credenciales por defecto.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            return;

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var admin = new AppUser
        {
            UserName       = email,
            Email          = email,
            EmailConfirmed = true,
            NombreCompleto = config["Seed:AdminNombre"] ?? "Administrador",
            Activo         = true
        };

        var r = await userManager.CreateAsync(admin, pass);

        if (r.Succeeded)
            await userManager.AddToRoleAsync(admin, Roles.SuperAdministrador);
    }
}
