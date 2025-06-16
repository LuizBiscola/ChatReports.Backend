# WebChat - Real-time Chat Application

Uma aplicação de chat em tempo real construída com ASP.NET Core, SignalR e PostgreSQL.

## 🚀 Funcionalidades

- **Chat em tempo real** com SignalR WebSockets
- **Chats diretos** entre dois usuários
- **Chats em grupo** com múltiplos participantes
- **Status de mensagens** (enviada, entregue, lida)
- **Indicadores de digitação** em tempo real
- **Status online/offline** dos usuários
- **Persistência de mensagens** no PostgreSQL
- **API RESTful** completa
- **Arquitetura escalável** com cache e otimizações

## 🛠️ Tecnologias Utilizadas

- **Backend**: ASP.NET Core 8.0
- **Real-time**: SignalR
- **Banco de dados**: PostgreSQL
- **ORM**: Entity Framework Core
- **Cache**: Memory Cache (preparado para Redis)
- **Containerização**: Docker
- **Documentação**: Swagger/OpenAPI

## 📋 Pré-requisitos

- .NET 8.0 SDK
- PostgreSQL
- Docker (opcional)

## 🔧 Configuração

### 1. Clone o repositório
```bash
git clone https://github.com/seu-usuario/webchat.git
cd webchat
```

### 2. Configure a string de conexão
Edite o arquivo `appsettings.json` e configure sua string de conexão PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=webchat;Username=seu_usuario;Password=sua_senha"
  }
}
```

### 3. Execute as migrações
```bash
cd WebChat
dotnet ef database update
```

### 4. Execute a aplicação
```bash
dotnet run
```

A aplicação estará disponível em:
- HTTP: `http://localhost:5093`
- HTTPS: `https://localhost:7267`
- Swagger: `https://localhost:7267/swagger`

## 🐳 Docker

### Executar com Docker
```bash
docker build -t webchat .
docker run -p 8080:8080 -p 8081:8081 webchat
```

## 📡 API Endpoints

### Usuários
- `GET /api/users` - Listar todos os usuários
- `POST /api/users` - Criar novo usuário
- `GET /api/users/{id}` - Obter usuário por ID
- `PUT /api/users/{id}` - Atualizar usuário
- `DELETE /api/users/{id}` - Deletar usuário

### Chats
- `GET /api/chats` - Listar todos os chats
- `POST /api/chats` - Criar novo chat
- `POST /api/chats/direct` - Criar/obter chat direto
- `GET /api/chats/{id}` - Obter chat por ID
- `GET /api/chats/user/{userId}` - Obter chats do usuário
- `DELETE /api/chats/{id}` - Deletar chat

### Mensagens
- `GET /api/chats/{chatId}/messages` - Obter mensagens do chat
- `POST /api/chats/{chatId}/messages` - Enviar mensagem
- `GET /api/chats/{chatId}/messages/{messageId}` - Obter mensagem específica
- `PUT /api/chats/{chatId}/messages/{messageId}/status` - Atualizar status da mensagem

## 🔌 SignalR Hub

### Conexão
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();
```

### Métodos disponíveis
- `JoinUser(userId, username)` - Conectar usuário
- `JoinChat(chatId)` - Entrar em um chat
- `LeaveChat(chatId)` - Sair de um chat
- `SendTyping(chatId, isTyping)` - Indicador de digitação
- `MarkMessagesAsRead(chatId, lastReadMessageId)` - Marcar mensagens como lidas
- `GetOnlineUsers()` - Obter usuários online

### Eventos recebidos
- `ReceiveMessage` - Nova mensagem recebida
- `UserOnline/UserOffline` - Status de usuário
- `UserTyping` - Indicador de digitação
- `MessageStatusUpdated` - Status de mensagem atualizado
- `UserJoinedChat/UserLeftChat` - Usuário entrou/saiu do chat

## 🏗️ Arquitetura

### Estrutura do Projeto
```
WebChat/
├── Controllers/          # Controladores da API
├── Data/                # Contexto do banco de dados
├── DTOs/                # Data Transfer Objects
├── Hubs/                # SignalR Hubs
├── Models/              # Modelos de dados
├── Services/            # Lógica de negócio
├── Migrations/          # Migrações do EF Core
└── Program.cs           # Configuração da aplicação
```

### Padrões Utilizados
- **Repository Pattern** via Services
- **DTO Pattern** para transferência de dados
- **Dependency Injection** nativo do .NET
- **Async/Await** para operações assíncronas
- **Caching** para otimização de performance

## 🚀 Escalabilidade

### Otimizações Implementadas
- **Connection Pooling** no PostgreSQL
- **Memory Caching** para consultas frequentes
- **Async Operations** em todas as operações I/O
- **Bulk Operations** para operações em lote
- **Query Optimization** com AsNoTracking
- **Transaction Management** para operações críticas

### Para Produção
- Configure **Redis** como backplane do SignalR
- Use **Redis Cache** distribuído
- Configure **Load Balancer**
- Implemente **Health Checks**
- Configure **Logging** estruturado
- Use **HTTPS** obrigatório

## 🔒 Segurança

### Implementações de Segurança
- **CORS** configurado adequadamente
- **Headers de segurança** em produção
- **Validação de entrada** em todos os endpoints
- **Rate limiting** preparado
- **Connection timeout** configurado
- **SQL Injection** prevenido via EF Core

## 📊 Monitoramento

### Health Checks
- Endpoint: `/health`
- Verifica conectividade com banco de dados
- Status do SignalR Hub

### Logging
- Logs estruturados
- Diferentes níveis por ambiente
- Rastreamento de operações críticas

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

## 👥 Autores

- **Seu Nome** - *Desenvolvimento inicial* - [SeuGitHub](https://github.com/seu-usuario)

## 🙏 Agradecimentos

- Equipe do ASP.NET Core
- Comunidade SignalR
- Contribuidores do projeto