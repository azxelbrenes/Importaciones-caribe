# Importaciones del Caribe CR

Plataforma web para un negocio de importación de vehículos desde Estados
Unidos a Costa Rica. Catálogo público con captación de clientes y panel
administrativo para gestionar inventario, solicitudes y usuarios.

Proyecto en producción, desarrollado para un cliente real.

---

## El problema

El negocio importa vehículos bajo pedido: el cliente dice qué busca, se
localiza en subastas o marketplaces de Estados Unidos, se compra, se
traslada a Puerto Caldera y se entrega nacionalizado con placas.

Antes del sistema, todo pasaba por WhatsApp: fotos sueltas, precios
calculados a mano en cada consulta, y ningún registro de quién había
preguntado qué.

El sistema resuelve tres cosas concretas:

**Un catálogo que muestra el precio final.** La competencia publica
"consultar precio". Acá el visitante ve cuánto cuesta el vehículo puesto
en Costa Rica, con el desglose de impuestos y trámites.

**Captación estructurada.** Los formularios registran qué busca cada
persona y abren WhatsApp con el mensaje ya escrito. La solicitud queda
en la base, no perdida en un chat.

**Control de costos.** El panel calcula el precio desde cinco campos de
costo y muestra el margen de cada operación. Esos números nunca salen
al sitio público.

---

## Stack

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 10, C# |
| Base de datos | PostgreSQL 17 + EF Core |
| Frontend | Angular 21 con renderizado en servidor |
| Almacenamiento | Cloudflare R2 |
| Proxy | Caddy con HTTPS automático |
| Contenedores | Docker + Docker Compose |
| Despliegue | GitHub Actions |

---

## Decisiones de arquitectura

### Sin repositorio genérico

`DbSet` ya es un repositorio y `DbContext` ya es una unidad de trabajo.
Envolverlos en otra capa esconde las capacidades de EF Core —
proyecciones, `AsNoTracking`, `ExecuteUpdate` — y obliga a escribir un
método por cada consulta.

Los servicios de negocio reciben el contexto directamente y devuelven
DTOs.

### El precio se calcula en el servidor

El frontend muestra una previsualización mientras se escriben los
costos, pero al guardar es el servidor quien recalcula desde los cinco
campos. Si el cliente enviara el precio, cualquiera podría manipularlo
desde las herramientas del navegador.

### Los DTOs públicos no tienen costos

`VehiculoDto` expone marca, modelo, año y precio final. `VehiculoAdminDto`
agrega costos, honorario y margen. Son tipos distintos, no el mismo con
campos ocultos: un descuido en una proyección no puede filtrar lo que no
existe en el tipo.

### El token de acceso vive en memoria

Nunca en `localStorage` ni `sessionStorage` — un XSS los lee en una
línea. El access token dura 15 minutos y vive en una variable de
JavaScript; el refresh token va en una cookie `HttpOnly` que el script
no puede alcanzar.

El refresh rota en cada uso y detecta reutilización: si llega un token
ya usado, se revocan todas las sesiones de ese usuario.

### Las imágenes se procesan al subirlas

Se validan por firma binaria, no por extensión — el nombre lo controla
el cliente. Se convierten a WebP en dos tamaños y se les eliminan los
metadatos EXIF, que incluyen la ubicación GPS de dónde se tomó la foto.

Una foto de celular de 5 MB queda en unos 200 KB.

### Verificación en dos pasos con TOTP

No por correo: un atacante que comprometa el buzón obtendría el código.
Con aplicación de autenticación, necesita el teléfono.

---

## Estructura

```
Caribe.Dominio/          Entidades y enums
Caribe.AccesoDatos/      DbContext e Identity
Caribe.LogicaNegocio/    Servicios, DTOs, validadores
Caribe.Utilitarios/      Result pattern
Caribe.Api/              Controladores y middleware
caribe-web/              Angular
```

---

## Configuración local

Requisitos: .NET 10 SDK, Node 22, Docker.

```bash
# Base de datos
docker compose -f docker-compose.dev.yml up -d

# Secretos de desarrollo
cd Caribe.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=caribe;Username=caribe;Password=..."
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"

# Migraciones
dotnet ef database update -p ../Caribe.AccesoDatos -s .

# API
dotnet run

# Frontend, en otra terminal
cd caribe-web
npm install
ng serve
```

El archivo `.env.example` documenta las variables que necesita el
servidor de producción.

---

## Seguridad

- Autenticación JWT con roles y verificación en dos pasos
- Refresh token rotativo con detección de reutilización
- Límite de tasa por IP y bloqueo de cuenta tras intentos fallidos
- Content Security Policy sin `unsafe-inline` en scripts
- Cabeceras HSTS, `nosniff`, `X-Frame-Options`, `Permissions-Policy`
- Auditoría de todas las operaciones de escritura
- Validación de archivos por firma binaria

---

## Licencia

Software desarrollado bajo contrato. El código se publica con fines de
portafolio; los datos, la marca y el contenido comercial pertenecen al
cliente.
