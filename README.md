# tracezilla-shopify-dotnet

Framework-neutral .NET templates for integrating Shopify with the tracezilla
API. The first example is the read-only **Compare Catalogs** workflow shared by
the PHP, TypeScript, Python, Ruby, and .NET projects.

## Run with Docker

```bash
cp .env.example .env
# Fill in test-account credentials
docker compose build
docker compose run --rm app
```

Optional output controls:

```bash
docker compose run --rm app --limit=25
docker compose run --rm app --json
```

The complete catalogs are compared by SKU code. `--limit` controls only the
maximum displayed rows in each category and defaults to 10. Differences return
exit code `0`; configuration and API failures return a non-zero code. Neither
API is modified.

## Tests

The .NET SDK does not need to be installed on the host:

```bash
docker compose run --rm --entrypoint dotnet app test tests/TracezillaShopify.Tests --no-restore
```

Tests use fake clients and readers and never contact either API.

## Design

```text
GraphQL query -> Shopify client -> catalog service -> mapper --+
                                                              +-> CompareCatalogs
tracezilla API -> tracezilla client -> catalog service -> mapper+
```

Queries, HTTP clients, pagination services, response mappers, comparison logic,
and rendering have separate responsibilities. This is a .NET console
application without ASP.NET or another application framework.

Canonical setup and safety guidance lives in the
[Tracezilla Integrations documentation](https://happy-bananas.github.io/tracezilla-integrations-docs/).

Never commit `.env`, print secrets, or begin with production accounts. This
read-only example needs only Shopify `read_products` access.
