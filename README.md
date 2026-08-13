OrderFlow
Sistema de gestión de pedidos e inventario basado en una arquitectura
distribuida con comunicación asíncrona mediante RabbitMQ.
El proyecto permite crear pedidos, consultar pedidos existentes y
procesar de forma asíncrona la reserva de inventario.
Arquitectura
El sistema está compuesto por:
Orders API: API REST responsable de crear y consultar pedidos.
Inventory Worker: consumidor de eventos encargado de validar y
reservar stock.
RabbitMQ: broker utilizado para la comunicación asíncrona entre
los servicios.
SQL Server: persistencia de pedidos e inventario.
Frontend: interfaz web para crear y consultar pedidos.
Contracts: proyecto compartido con los contratos de los eventos.
Flujo principal
``` text

                    ┌─────────────────┐

                    │    Frontend     │

                    │   localhost     │

                    │      :5173      │

                    └────────┬────────┘

                             │

                             │ HTTP

                             ▼

                    ┌─────────────────┐

                    │   Orders API    │

                    │      :5216      │

                    └────────┬────────┘

                             │

                    orders.created

                             │

                             ▼

                    ┌─────────────────┐

                    │    RabbitMQ     │

                    │                 │

                    │ orders.exchange  │

                    │ stock.exchange  │

                    └────────┬────────┘

                             │

                    orders.created

                             │

                             ▼

                    ┌─────────────────┐

                    │ Inventory Worker│

                    └────────┬────────┘

                             │

                       reserva stock

                             │

                    ┌────────┴────────┐

                    │                 │

             stock.reserved     stock.rejected

                    │                 │

                    └────────┬────────┘

                             ▼

                         RabbitMQ

                             │

                             ▼

                       Orders API

                             │

                    actualiza el pedido

                             │

                    ┌────────┴────────┐

                    │                 │

                Confirmed          Rejected
```
---
Funcionalidades
Orders API
Permite:
Crear pedidos.
Consultar todos los pedidos.
Consultar un pedido por ID.
Validar los datos recibidos.
Mantener el pedido inicialmente en estado `Pending`.
Publicar el evento `OrderCreated`.
Endpoints principales:
``` text

POST /orders

GET  /orders

GET  /orders/{id}
```
Estados del pedido
``` text

Pending

   │

   ├── stock disponible ──> Confirmed

   │

   └── stock insuficiente

       o producto inexistente

            │

            ▼

         Rejected
```
---
Comunicación asíncrona
La comunicación entre Orders API e Inventory Worker se realiza mediante
RabbitMQ.
Evento `OrderCreated`
Cuando se crea un pedido, Orders API publica:
``` text

Exchange: orders.exchange

Routing key: orders.created

Queue: orders.created.queue
```
El Inventory Worker consume este evento y realiza la reserva de stock.
Posteriormente publica uno de los siguientes eventos:
``` text

stock.reserved

stock.rejected
```
Esto permite desacoplar ambos servicios.
Orders API no necesita llamar directamente a Inventory Worker mediante
HTTP para realizar la reserva.
---
Idempotencia
El Inventory Worker implementa idempotencia utilizando el `EventId` del
evento.
Cada evento procesado se registra en:
``` text

ProcessedEvents
```
`EventId` está definido como clave primaria:
``` csharp

modelBuilder.Entity<ProcessedEvent>()

    .HasKey(e => e.EventId);
```
Antes de procesar un evento se comprueba si ya fue procesado:
``` csharp

var alreadyProcessed = await _context.ProcessedEvents

    .AnyAsync(

        x => x.EventId == eventId,

        cancellationToken);
```
Si el evento ya existe:
``` text

AlreadyProcessed
```
y no se vuelve a descontar el stock.
Ejemplo
Stock inicial:
``` text

ABC-01 = 98
```
Primer procesamiento:
``` text

EventId = A



98 - 1 = 97
```
El mismo evento vuelve a llegar:
``` text

EventId = A



97 → 97
```
El stock no vuelve a descontarse.
Prueba realizada
La idempotencia fue comprobada publicando exactamente el mismo evento
dos veces en RabbitMQ.
Resultado:
``` text

Primer evento       → stock 97

Evento duplicado    → stock 97
```
Por lo tanto, el mismo evento no produce dos reservas.
---
Manejo de fallos
Inventory Worker no disponible
Los pedidos se crean inicialmente con:
``` text

Pending
```
RabbitMQ mantiene el mensaje en la cola mientras el consumidor no está
disponible.
Cuando Inventory Worker vuelve a estar disponible puede procesar los
eventos pendientes.
---
RabbitMQ no disponible
El pedido se persiste primero en SQL Server con estado:
``` text

Pending
```
Posteriormente Orders API intenta publicar el evento.
Si RabbitMQ no está disponible:
``` text

Pedido guardado       ✓

Publicación           ✗

Estado                Pending

Respuesta HTTP        503 Service Unavailable
```
El usuario recibe un mensaje indicando que el pedido fue creado pero no
pudo enviarse al sistema de inventario.
Trade-off
La implementación actual prioriza la simplicidad y mantiene el pedido
persistido antes de publicar el evento.
Esto significa que si RabbitMQ está caído, el evento no se publica
automáticamente posteriormente y el pedido puede permanecer en
`Pending`.
En una implementación productiva utilizaría Outbox Pattern para
garantizar la publicación eventual del evento.
---
Persistencia y migraciones
Los servicios utilizan Entity Framework Core.
Las bases de datos se preparan automáticamente mediante migraciones al
iniciar los servicios.
Orders API ejecuta las migraciones de su contexto antes de comenzar a
atender solicitudes.
Inventory Worker realiza el mismo proceso para su base de datos.
Esto permite iniciar el proyecto desde cero sin tener que crear
manualmente las tablas.
---
Seed de inventario
Inventory Worker también incorpora datos iniciales para facilitar las
pruebas.
Productos de ejemplo:
``` text

ABC-01

KEY-01

MON-01
```
El seed se ejecuta automáticamente cuando la base de datos está
disponible.
---
Docker
El proyecto incluye Dockerfiles independientes para:
``` text

Orders API

Inventory Worker

Frontend
```
Además, `docker-compose.yml` permite levantar la infraestructura
completa.
Componentes:
``` text

Frontend

Orders API

Inventory Worker

RabbitMQ

SQL Server
```
Ejecutar el proyecto
Requisitos:
Docker Desktop
Git
Clonar el repositorio:
``` bash

git clone https://github.com/dapalamac/OrderFlow_Q10

cd OrderFlow
```
Crear el archivo de variables de entorno a partir del ejemplo:
``` bash

copy .env.example .env
```
En Linux/macOS:
``` bash

cp .env.example .env
```
Después ejecutar:
``` bash

docker compose up --build
```
Docker descargará las imágenes necesarias, construirá los servicios y
levantará toda la infraestructura.
---
URLs
Frontend:
``` text

http://localhost:5173
```
Orders API:
``` text

http://localhost:5216
```
RabbitMQ Management:
``` text

http://localhost:15672
```
RabbitMQ utiliza las credenciales configuradas mediante variables de
entorno.
---
Variables de entorno
La configuración sensible no se almacena directamente en el código.
El proyecto utiliza variables de entorno para configurar principalmente:
SQL Server
RabbitMQ
credenciales
cadenas de conexión
El archivo:
``` text

.env
```
no debe subirse al repositorio.
Para facilitar la configuración existe:
``` text

.env.example
```
El `.env.example` contiene únicamente valores de ejemplo y sirve como
plantilla para la configuración local.
---
Tests
El proyecto contiene tests backend para la lógica crítica del Inventory
Worker.
Framework utilizado:
``` text

xUnit
```
Para evitar depender de SQL Server durante los tests se utiliza:
``` text

Microsoft.EntityFrameworkCore.InMemory
```
Ejecutar tests
Desde la raíz del proyecto:
``` bash

dotnet test
```
Resultado actual:
``` text

Total:   5

Passed:  5

Failed:  0

Skipped: 0
```
Tests implementados
1. Reserva correcta
Verifica que una reserva válida descuente correctamente el stock.
``` text

Stock = 10

Cantidad = 2

Resultado = 8
```
2. Stock insuficiente
Verifica que no se descuente stock cuando la cantidad solicitada supera
el inventario disponible.
3. Producto inexistente
Verifica que el sistema devuelva `ProductNotFound` cuando el SKU no
existe.
4. Idempotencia
Verifica que el mismo `EventId` procesado dos veces solamente descuente
stock una vez.
5. Registro de eventos procesados
Verifica que un evento procesado correctamente sea registrado en
`ProcessedEvents`.
---
Decisiones de arquitectura
¿Por qué RabbitMQ?
Se utiliza RabbitMQ para desacoplar Orders API de Inventory Worker.
En lugar de:
``` text

Orders API

    │

    │ HTTP

    ▼

Inventory API
```
se utiliza:
``` text

Orders API

    │

    │ Event

    ▼

RabbitMQ

    │

    ▼

Inventory Worker
```
Esto permite que Inventory procese los pedidos de forma asíncrona.
Además, RabbitMQ permite mantener los mensajes pendientes cuando un
consumidor no está disponible.
---
¿Por qué un Worker para Inventory?
Inventory no necesita exponer una API HTTP para recibir cada pedido.
Su responsabilidad principal es consumir eventos y ejecutar lógica de
inventario.
Por ello se implementó como un Worker Service.
Esto mantiene una separación clara de responsabilidades:
``` text

Orders API

    → pedidos



Inventory Worker

    → inventario
```
---
Trade-offs
Persistencia antes de publicación
Actualmente el pedido se guarda antes de publicar `OrderCreated`.
Ventaja:
El pedido no se pierde si RabbitMQ está temporalmente caído.
Desventaja:
Si RabbitMQ falla después de guardar el pedido, el evento puede no
publicarse y el pedido puede quedar en `Pending`.
Alternativa
Implementar Outbox Pattern.
---
Polling en frontend
El frontend actualiza periódicamente la lista de pedidos.
Ventaja:
Implementación sencilla.
No requiere mantener conexiones WebSocket.
Desventaja:
Existe un pequeño retraso entre el cambio de estado y su
visualización.
Genera solicitudes periódicas.
Para esta prueba técnica se considera suficiente.
Una alternativa sería utilizar:
``` text

SignalR / WebSockets
```
para actualizaciones en tiempo real.
---
Estructura del proyecto
``` text

OrderFlow

│

├── OrderFlow.Contracts

│   └── Events

│

├── OrderFlow.Orders.Api

│   ├── Controllers

│   ├── Data

│   ├── Entities

│   ├── Messaging

│   └── Workers

│

├── OrderFlow.Inventory.Worker

│   ├── Data

│   ├── Entities

│   ├── Messaging

│   └── Services

│

├── OrderFlow.Inventory.Worker.Tests

│

├── OrderFlow.Frontend

│

├── docker-compose.yml

├── .env.example

├── .gitignore

└── README.md
```
---
Mejoras futuras
Con más tiempo, mejoraría principalmente:
1. Pruebas de integración
Ampliaría los tests para probar el flujo completo entre Orders API,
RabbitMQ, Inventory Worker y SQL Server.
2. Manejo de errores
Implementaría mecanismos adicionales para mejorar la recuperación ante
fallos temporales de los servicios.
3. Observabilidad
Mejoraría los logs y el seguimiento de las operaciones para facilitar la
detección y diagnóstico de errores.
Añadiría:
logging estructurado
correlation IDs
métricas
5. Actualizaría el frontend
Esto permitiría actualizar el estado de los pedidos en tiempo real en
lugar de utilizar polling.
6. Ampliaría la cobertura de tests
Añadiría tests de integración para:
RabbitMQ
Orders API
persistencia
flujo completo de creación y confirmación de pedidos
---
Flujo completo de ejemplo
Un pedido creado desde el frontend sigue este flujo:
``` text

1. Usuario crea pedido

        ↓

2. Frontend → Orders API

        ↓

3. Orders API valida datos

        ↓

4. Pedido se guarda como Pending

        ↓

5. Orders API publica OrderCreated

        ↓

6. RabbitMQ recibe el evento

        ↓

7. Inventory Worker consume OrderCreated

        ↓

8. Inventory valida el SKU

        ↓

9. Inventory verifica stock

        ↓

10. Se reserva el stock

        ↓

11. Se registra EventId

        ↓

12. Inventory publica StockReserved

        ↓

13. Orders API consume el resultado

        ↓

14. Pedido pasa a Confirmed

        ↓

15. Frontend actualiza la lista
```
---
Estado del proyecto
| Requisito | Estado |
|---|---|
| Frontend para crear pedidos | ✅ |
| Validaciones y errores visibles | ✅ |
| Lista de pedidos | ✅ |
| Búsqueda por ID | ✅ |
| Estados de pedido | ✅ |
| Polling | ✅ |
| Comunicación asíncrona | ✅ |
| RabbitMQ en Docker Compose | ✅ |
| Idempotencia | ✅ |
| Idempotencia demostrada | ✅ |
| Manejo de fallos | ✅ |
| Dockerfiles | ✅ |
| Docker Compose | ✅ |
| Migraciones automáticas | ✅ |
| Seed automático | ✅ |
| Variables de entorno | ✅ |
| Tests backend | ✅ 5/5 |
| README | ✅ |
Tecnologías utilizadas
C#
.NET 10
ASP.NET Core
Entity Framework Core
SQL Server
RabbitMQ
Docker
Docker Compose
Nginx
xUnit
HTML / CSS / JavaScript
