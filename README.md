# FCG Users API — Grupo 14

Microsserviço responsável pelo cadastro, autenticação JWT e autorização de usuários da plataforma FIAP Cloud Games (FCG). Migrado do monolito `FCG_GRUPO_14` como parte da Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Domínio](#domínio)
- [Endpoints](#endpoints)
- [Eventos Publicados](#eventos-publicados)
- [Pré-requisitos](#pré-requisitos)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Rodando com Docker](#rodando-com-docker)
- [Rodando localmente](#rodando-localmente)
- [Kubernetes e CI/CD](#kubernetes-e-cicd)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Users |
| Tipo | Web API (HTTP) |
| Porta | 8080 |
| Banco de dados | PostgreSQL — `fcg_users_db` |
| Publica evento | `UserCreatedEvent` → RabbitMQ |

---

## Arquitetura

O serviço adota **Clean Architecture**, organizando o código em camadas com dependências que fluem sempre de fora para dentro.

```
fcg-users-api/
├── FCG.UsersAPI.Domain        # Entidades, value objects, enums e interfaces
├── FCG.UsersAPI.Application   # Casos de uso: AuthService, UserService, TokenService
├── FCG.UsersAPI.Infra         # EF Core, repositórios, migrations
└── FCG.UsersAPI.Api           # Controllers, middlewares, startup
```

**Fluxo de dependências:**
```
FCG.UsersAPI.Api    →  FCG.UsersAPI.Application  →  FCG.UsersAPI.Domain
FCG.UsersAPI.Infra  →  FCG.UsersAPI.Application  →  FCG.UsersAPI.Domain
```

`FCG.UsersAPI.Domain` não depende de nenhum outro projeto da solução. Note que `Infra` referencia `Application` — é de lá que vem o contrato `UserCreatedEvent` usado para registrar o consumer/publisher no MassTransit.

### Padrões aplicados

- **Repository Pattern** — abstração de acesso a dados via interfaces no domínio
- **Value Objects** — `Email` e `Password` encapsulam validação e formatação
- **DTOs** — separam o contrato HTTP das entidades de domínio
- **Middleware** — tratamento centralizado de exceções
- **Event Publishing** — `UserCreatedEvent` publicado via MassTransit após registro bem-sucedido

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Framework Web | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core + Npgsql | 10.0.5 |
| Banco de Dados | PostgreSQL | 16 |
| Autenticação | JWT Bearer + BCrypt.Net | — / 4.1.0 |
| Documentação | Swagger / Swashbuckle | 10.1.6 |
| Mensageria | MassTransit + RabbitMQ | — |
| Logging | Serilog | 10.0.0 |

---

## Domínio

### Entidade User

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | Guid | Identificador único |
| `Name` | string (máx. 150) | Nome do usuário |
| `Email` | Value Object | Endereço de e-mail validado |
| `Password` | Value Object | Senha com hash BCrypt |
| `Role` | enum (`User=0`, `Admin=1`) | Perfil de acesso |
| `IsActive` | bool | Estado da conta |
| `CreatedAt` | DateTime | Data de cadastro |

### Value Objects

| Value Object | Validação |
|---|---|
| `Email` | Formato RFC (`^[^@\s]+@[^@\s]+\.[^@\s]+$`), não vazio |
| `Password` | Mínimo 8 caracteres, letras, dígitos e caractere especial obrigatórios |

---

## Endpoints

Todas as respostas seguem o envelope padronizado `ApiResponse<T>` com os campos `data`, `success` e `errors`.

### Autenticação — `/api/v1/Auth`

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| `POST` | `/api/v1/Auth/login` | Não | Login — retorna JWT Bearer. Formato inválido → `400`; credenciais inválidas (e-mail inexistente ou senha incorreta) → `401` |

### Saúde

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/health` | Health check (API e bus do MassTransit); usado pelas probes do Kubernetes. Pelo Kong: `/health/users` |

### Usuários — `/api/v1/User`

| Método | Rota | Auth | Perfil | Descrição |
|---|---|---|---|---|
| `POST` | `/api/v1/User/register` | Não | — | Cadastro — publica `UserCreatedEvent` |
| `GET` | `/api/v1/User/get-all` | Sim | Admin | Lista todos os usuários |
| `GET` | `/api/v1/User/get-by-id` | Sim | Admin | Busca usuário por ID |
| `PATCH` | `/api/v1/User/update-by-id` | Sim | User / Admin | Atualiza nome ou status ativo |

> As rotas usam `[controller]` (nome da classe sem o sufixo `Controller`), por isso `AuthController` → `/api/v1/Auth` e `UserController` → `/api/v1/User` (singular).

### Formato do corpo — `register` e `login`

`Email` e `Password` são Value Objects, então o JSON precisa envolver o valor em um objeto:

```json
{
  "name": "Nome do Usuário",
  "email": { "value": "usuario@fcg.com" },
  "password": { "value": "Senha@123" }
}
```

### Autenticação no Swagger

1. Chame `POST /api/v1/auth/login` com e-mail e senha para obter o token.
2. Clique em **Authorize** no Swagger UI.
3. Informe o valor no formato `Bearer <token>`.

### Políticas de autorização

| Política | Requisito |
|---|---|
| `AdminOnly` | Role = Admin |
| `UserOnly` | Role = User |
| `UserOrAdmin` | Role = User ou Admin |

---

## Eventos Publicados

### UserCreatedEvent

Publicado no exchange/fila `UserCreated` via RabbitMQ após registro bem-sucedido.

```csharp
namespace FCG.Events;

public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Consumer:** NotificationsAPI

> **Namespace compartilhado (`FCG.Events`):** o MassTransit identifica o tipo de uma mensagem no wire pelo namespace + nome do tipo .NET, não pelo assembly. A classe `UserCreatedEvent` existe fisicamente em cada repositório (UsersAPI publica, NotificationsAPI consome), mas **precisa declarar o mesmo namespace `FCG.Events`** nos dois lados — caso contrário, publisher e consumer geram identidades de mensagem diferentes e o evento é descartado silenciosamente pelo consumer (fila `_skipped`, sem erro nem exceção). Ao alterar esse contrato, replique a mudança (e o namespace) em `fcg-notifications-api`.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — PostgreSQL + RabbitMQ via Docker Compose (repositório `fcg-infra`)

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL | `Host=postgres;Port=5432;Database=fcg_users_db;Username=fcg;Password=<senha-do-postgres>` |
| `Jwt__SecretKey` | Chave secreta do JWT (compartilhada com CatalogAPI) | — |
| `Jwt__Issuer` | Issuer do token | `FCG.UsersAPI` |
| `Jwt__Audience` | Audience do token | `FCG.Client` |
| `Jwt__ExpirationMinutes` | Tempo de expiração do token em minutos | `60` |
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | — |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_REGION` | Credenciais do IAM user `fcg-services` (publicação na fila SQS de notificações) | — / — / `us-east-1` |
| `Aws__Sqs__UserCreatedQueueUrl` | URL da fila SQS `fcg-user-created` | — |

Não há valores padrão para segredos: a API falha ao subir se `ConnectionStrings__Postgres` ou `Jwt__SecretKey` estiverem ausentes. Os valores vêm de variáveis de ambiente (Docker Compose via `.env` em `fcg-infra`) ou de Secrets do Kubernetes, nunca de arquivos versionados.

---

## Rodando com Docker

Suba toda a infraestrutura a partir do repositório `fcg-infra`:

```bash
docker compose up -d
```

A API estará disponível em:
- **HTTP:** `http://localhost:8080`
- **Swagger UI:** `http://localhost:8080/swagger` (no EKS não passa pelo Kong; use `kubectl port-forward svc/users-api 8081:80 -n fcg` e abra `http://localhost:8081/swagger`)

---

## Rodando localmente

### 1. Configure o PostgreSQL e RabbitMQ

Certifique-se de ter PostgreSQL 16 e RabbitMQ acessíveis localmente ou via Docker:

```bash
docker run -d --name postgres -e POSTGRES_USER=fcg -e POSTGRES_PASSWORD=<senha-do-postgres> -e POSTGRES_DB=fcg_users_db -p 5432:5432 postgres:16-alpine
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
```

### 2. Restaure dependências e aplique migrations

```bash
dotnet restore
dotnet ef database update --project FCG.UsersAPI.Infra --startup-project FCG.UsersAPI.Api
```

### 3. Execute a API

```bash
dotnet run --project FCG.UsersAPI.Api/FCG.UsersAPI.Api.csproj
```

---

## Kubernetes e CI/CD

Os manifests ficam em **`fcg-infra/k8s/`** (fonte única, namespace `fcg`); este repositório não mantém cópias. O deploy é feito pelo GitHub Actions (`.github/workflows/ci-cd.yml`, que chama o workflow reutilizável de `fcg-infra`): a cada push em `main` roda build e testes, gera a imagem com tag do commit (SHA curto), faz scan com Trivy, publica no ECR e atualiza o Deployment no EKS com rolling update sem downtime. Em pull requests roda apenas build e testes.

Variável necessária no repositório do GitHub: `AWS_ROLE_ARN` (output `github_actions_role_arn` do Terraform em `fcg-infra/terraform`), além de um *environment* `production`.

## Estrutura do Projeto

```
fcg-users-api/
├── FCG.UsersAPI.Api/
│   ├── Authorization/       # Policies (AdminOnly, UserOnly, UserOrAdmin)
│   ├── Common/               # ApiResponse<T> — envelope de resposta
│   ├── Controllers/          # AuthController, UserController, BaseController
│   ├── Extensions/           # JwtExtensions, AuthorizationPoliciesExtensions, GlobalExceptionHandlerExtensions
│   ├── Middlewares/          # GlobalExceptionHandler, RequestTimingMiddleware
│   ├── Program.cs
│   ├── Startup.cs
│   └── appsettings.json
│
├── FCG.UsersAPI.Application/
│   ├── Configuration/        # ConfigureApplication (DI)
│   ├── Services/              # AuthService, UserService, TokenService
│   ├── DTOs/                  # Login/Register request e response
│   ├── Events/
│   │   └── UserCreatedEvent.cs   # namespace FCG.Events (compartilhado com NotificationsAPI)
│   ├── Exceptions/            # RegisterException
│   └── Interfaces/            # IAuthService, IUserService, ITokenService
│
├── FCG.UsersAPI.Domain/
│   ├── Common/                # Result<T>
│   ├── Entities/               # User
│   ├── ValueObjects/           # Email, Password
│   ├── Enums/                  # UserRole
│   └── Interfaces/             # IUserRepository, IRepositoryBase
│
├── FCG.UsersAPI.Infra/
│   ├── Configuration/          # ConfigureInfra (DI + MassTransit + SetEntityName)
│   ├── Repositories/            # UserRepository, RepositoryBase
│   ├── Migrations/              # Histórico de migrations EF Core
│   ├── AppDbContext.cs
│   └── AppDbContextFactory.cs
│
├── FCG.UsersAPI.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── BDD/
│
├── .github/workflows/          # Pipeline CI/CD (chama o workflow de fcg-infra)
├── docker-compose.yml
└── Dockerfile
```

---

## Logs

Os logs são gerenciados pelo **Serilog** com comportamento diferente por ambiente:

| Ambiente | Destinos | Nível mínimo |
|---|---|---|
| Development | Console + arquivo `Logs/log-dev-<hora>.txt` | Debug |
| Production | Arquivo `Logs/log-prod-<data>.txt` (JSON) | Information |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
