# Importaciones del Caribe CR

Plataforma web para un negocio de importación de vehículos desde Estados
Unidos a Costa Rica. Catálogo público con captación de clientes y panel
administrativo para gestionar inventario, solicitudes y accesos.

En producción, desarrollado para un cliente real.

---

## El problema

El negocio importa vehículos bajo pedido: se localiza el carro en
Estados Unidos, se compra, se traslada, se nacionaliza y se entrega con
placas.

Antes del sistema todo pasaba por WhatsApp: fotos sueltas, precios
calculados a mano en cada consulta, y ningún registro de quién había
preguntado qué.

El sistema resuelve tres cosas concretas:

**Un catálogo que muestra el precio final.** La competencia publica
"consultar precio". Acá el visitante ve cuánto cuesta el vehículo puesto
en Costa Rica, con impuestos y trámites incluidos.

**Captación que no se pierde.** Cuando alguien toca "Me interesa", la
solicitud se guarda en la base **antes** de abrir WhatsApp. Si se
abriera primero, el contacto existiría solo en el chat: sin registro,
sin estadística, y sin forma de saber quién quedó sin respuesta.

**Control de costos.** El panel calcula el precio desde cinco campos de
costo y muestra el margen de cada operación. Esos números nunca salen
al sitio público.

---

## Stack

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 10, C# |
| Base de datos | PostgreSQL 17 + EF Core |
| Frontend | Angular 22 con renderizado en servidor |
| Almacenamiento | Cloudflare R2 |
| Correo | Resend |
| Proxy | Caddy con HTTPS automático |
| Contenedores | Docker + Docker Compose |
| Despliegue | GitHub Actions |

---

## Decisiones de arquitectura

### Sin repositorio genérico

`DbSet` ya es un repositorio y `DbContext` ya es una unidad de trabajo.
Envolverlos en otra capa esconde las capacidades de EF Core
—proyecciones, `AsNoTracking`, `ExecuteUpdate`— y obliga a escribir un
método por cada consulta.

Los servicios de negocio reciben el contexto directamente y devuelven
DTOs.

### El precio se calcula en el servidor

El frontend muestra una previsualización mientras se escriben los
costos, pero al guardar es el servidor quien recalcula desde los cinco
campos. El DTO de entrada ni siquiera tiene el campo de precio: si
viniera del navegador, cualquiera podría publicar un vehículo a un
dólar.

### Los DTOs públicos no tienen costos

`VehiculoDto` expone marca, modelo, año y precio final.
`VehiculoAdminDto` agrega costos, honorario y margen. Son tipos
distintos, no el mismo con campos ocultos: un descuido en una
proyección no puede filtrar lo que no existe en el tipo.

Por la misma razón, la ficha pública **no devuelve el desglose de
costos**. Ocultarlo en la página no habría servido: los montos seguirían
viajando en la respuesta del API, visibles desde las herramientas del
navegador. La página muestra qué incluye el precio, sin cifras por
línea.

### El financiamiento no publica tasas

El sitio informa la prima y los plazos disponibles. Las condiciones se
acuerdan directamente con cada cliente por WhatsApp.

Eso simplificó el modelo —no hay calculadora de cuotas ni texto legal
de crédito publicado— y elimina el riesgo de publicar condiciones
financieras que cambian caso a caso.

### El token de acceso vive en memoria

Nunca en `localStorage` ni `sessionStorage`: un XSS los lee en una
línea. El access token dura 15 minutos y vive en una variable de
JavaScript; el refresh token va en una cookie `HttpOnly` que el script
no puede alcanzar.

El refresh rota en cada uso y detecta reutilización: si llega un token
ya usado, se revocan todas las sesiones de ese usuario.

**Una sola renovación a la vez.** Cuando el token vence, una pantalla
que pide varias cosas en paralelo recibe varios 401 juntos. Si cada uno
pidiera renovar por su cuenta, el primero rotaría la cookie y el
segundo llegaría con la anterior —ya usada— disparando la detección de
robo y cerrando todas las sesiones. El servicio comparte la renovación
en curso entre todas las peticiones que esperan.

### Las imágenes se procesan al subirlas

Se validan por firma binaria, no por extensión: el nombre lo controla
el cliente. Se convierten a WebP en dos tamaños y se les eliminan los
metadatos EXIF, que incluyen la ubicación GPS de dónde se tomó la foto
—publicar una foto tomada en casa del dueño publicaría su dirección.

Una foto de celular de 5 MB queda en unos 200 KB.

### Verificación en dos pasos con TOTP

No por correo: un atacante que comprometa el buzón obtendría el código.
Con aplicación de autenticación, necesita el teléfono.

El código se exige **antes** de activar la función: si se activara sin
confirmar, alguien que configuró mal la aplicación quedaría bloqueado
fuera de su propia cuenta.

### Tres roles con alcances distintos

**Operador** solo atiende solicitudes; no ve vehículos ni márgenes.
**Administrador** gestiona el catálogo y las estadísticas.
**Propietario** además invita usuarios, elimina registros y cambia las
condiciones de financiamiento.

Los controladores son restrictivos por defecto: `[Authorize]` en la
clase y `[AllowAnonymous]` en las excepciones. Si mañana se agrega un
endpoint y se olvida el atributo, queda protegido en vez de expuesto.

### Auditoría con estado anterior y posterior

Cada cambio de precio, estado o acceso queda registrado en `jsonb` con
los valores antes y después. Sirvió para diagnosticar problemas reales
en producción: la detección de reutilización de tokens se encontró
leyendo esa tabla, no reproduciendo el error.

### El sitemap se genera, no se escribe

Un archivo fijo quedaría viejo el día que se publique un vehículo, y
los buscadores seguirían mostrando fichas de carros vendidos. El
backend lo genera con los vehículos visibles y su fecha de
modificación.

Un vehículo vendido devuelve 404 real desde el servidor, no un 200 con
un mensaje: así deja de aparecer en resultados de búsqueda.

---

## Estructura

```
Caribe.Dominio/         Entidades y enums. Sin dependencias.
Caribe.Utilitarios/     Respuesta<T>, paginación, slugs, etiquetas.
Caribe.AccesoDatos/     DbContext, Identity, migraciones.
Caribe.LogicaNegocio/   Servicios, DTOs, validadores, almacenamiento, correo.
Caribe.Api/             Controladores y middleware.
caribe-web/             Angular: sitio público y panel.
```

---

## Despliegue

Cada push a `main` construye dos imágenes en paralelo, las publica en
GHCR, y el servidor las descarga y levanta. Al arrancar, el API aplica
las migraciones pendientes.

La base de datos no expone puertos: solo es alcanzable desde la red
interna de Docker. Las credenciales viven en un archivo de entorno en
el servidor, nunca en el repositorio.

---

## Estado

En producción. El catálogo, el panel administrativo, el financiamiento,
las notificaciones por correo y los respaldos automáticos están
funcionando.

---

## Licencia

Software desarrollado para un cliente. El código se publica con fines
de referencia; todos los derechos reservados.
