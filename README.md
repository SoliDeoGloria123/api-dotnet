# api-dotnet

API REST de inventario escrita en ASP.NET Core 8. Usa Entity Framework Core con SQLite, autenticación JWT, validación de peticiones, Swagger y pruebas de integración con `WebApplicationFactory`.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Cómo ejecutarla

```bash
dotnet run --project src/InventoryApi
```

La API queda en `http://localhost:5264` (el puerto exacto aparece en la consola). Swagger está en `/swagger`.

1. Crea un usuario en `POST /api/auth/register` o inicia sesión en `POST /api/auth/login`.
2. Copia el `token` de la respuesta.
3. Autoriza en Swagger con **Authorize** o envía `Authorization: Bearer <token>`.
4. Crea, lista, actualiza y elimina productos en `/api/products`.

Ejemplo:

```bash
curl -s -X POST http://localhost:5264/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"username":"juan","password":"Password123"}'

curl -s http://localhost:5264/api/products \
  -H "Authorization: Bearer <token>"
```

### Autenticación

| Método | Ruta | Autenticación | Descripción |
|--------|------|---------------|-------------|
| POST | `/api/auth/register` | No | Crea un usuario y devuelve un JWT |
| POST | `/api/auth/login` | No | Inicia sesión y devuelve un JWT |
| GET | `/api/auth/me` | Sí | Perfil del usuario autenticado |
| GET | `/api/health` | No | Comprobación de salud |

### Productos

Todas las rutas de productos requieren JWT.

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/products` | Listado paginado. Query: `page`, `pageSize`, `search`, `category` |
| GET | `/api/products/{id}` | Detalle |
| POST | `/api/products` | Alta. El SKU debe ser único (`A-Z`, `0-9` o `-`) |
| PUT | `/api/products/{id}` | Actualiza nombre, descripción, categoría y precio |
| PATCH | `/api/products/{id}/stock` | Ajusta existencias con `{ "delta": -3 }` |
| DELETE | `/api/products/{id}` | Elimina el producto |

El stock no puede quedar por debajo de cero. Un SKU o un nombre de usuario repetido responde `409 Conflict`. Las validaciones fallidas responden `400`.

## Configuración y secretos

La clave JWT **no es un secreto de producción**. En `appsettings.json` hay un valor de desarrollo claramente etiquetado:

```text
DEV-ONLY-DO-NOT-USE-IN-PRODUCTION-CHANGE-ME-32+
```

Sustitúyelo en cualquier entorno real con la variable `Jwt__Key` (mínimo 32 caracteres). En `Production`, la API se niega a arrancar si sigue la clave de desarrollo.

La cadena de conexión por defecto es `Data Source=inventory.db`. Puedes cambiarla con `ConnectionStrings__Default`.

## Pruebas

```bash
dotnet test
```

Las pruebas de integración levantan la API en memoria con `WebApplicationFactory` y una base SQLite temporal por ejecución.

## Docker

```bash
docker build -t inventory-api .
docker run --rm -p 8080:8080 -e Jwt__Key='una-clave-secreta-de-al-menos-32-chars' inventory-api
```

## English

ASP.NET Core 8 inventory REST API with EF Core and SQLite, JWT authentication, request validation, Swagger, and integration tests using `WebApplicationFactory`.

```bash
dotnet run --project src/InventoryApi
dotnet test
```

Register or log in at `/api/auth`, then call `/api/products` with `Authorization: Bearer <token>`. The JWT key in `appsettings.json` is a labeled development default (`DEV-ONLY-DO-NOT-USE-IN-PRODUCTION-CHANGE-ME-32+`). Override it with `Jwt__Key`. In Production the app will not start if that default is still in use.

## Autor / Author

Juan David Mahecha
[LinkedIn](https://www.linkedin.com/in/juan-david-mahecha-dev)
