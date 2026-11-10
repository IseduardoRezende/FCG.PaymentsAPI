# Testes — FCG.PaymentsAPI

Este arquivo lista os cenários de teste do microsserviço de pagamentos. Os nomes seguem o padrão `Metodo_Cenario_ResultadoEsperado`.

| Suíte | Projeto | Ferramentas | Execuções |
|-------|---------|-------------|-----------|
| Unitários | `05- Tests/FCG.Payments.UnitTests` | xUnit v3, Moq | 20 |
| Integração | `05- Tests/FCG.Payments.IntegrationTests` | WebApplicationFactory, Testcontainers (PostgreSQL 17), MassTransit Test Harness | 9 |

A contagem de execuções inclui cada caso de `[Theory]`.

## Como executar

```powershell
dotnet test --project "05- Tests/FCG.Payments.UnitTests/FCG.Payments.UnitTests.csproj"
dotnet test --project "05- Tests/FCG.Payments.IntegrationTests/FCG.Payments.IntegrationTests.csproj"   # requer Docker em execução
```

---

## Testes unitários

### `Payment` (regra simulada de aprovação)

A regra é `Approved` se `valor <= Payments:MaxApprovedAmount`; caso contrário, `Rejected`. Nos testes o limite é 100,00.

| Teste | Regra de negócio verificada |
|-------|-----------------------------|
| `Process_WithAmountBelowLimit_IsApproved` (3 casos) | Valores abaixo do limite são aprovados (0,01, 59,90 e 99,99). |
| `Process_WithAmountExactlyAtLimit_IsApproved` | O valor exatamente igual ao limite também é aprovado. |
| `Process_WithAmountAboveLimit_IsRejected` (2 casos) | Valores acima do limite são recusados (100,01 e 199,90). |
| `Process_ForAnyResult_KeepsOrderDataForTraceability` | O pagamento guarda o pedido, o usuário, o e-mail, o jogo, o valor e a data. |

### `PaymentService`

| Teste | Regra de negócio verificada |
|-------|-----------------------------|
| `ProcessAsync_ForNewOrder_PublishesPaymentProcessedEventWithStatusByLimit` (4 casos) | O `PaymentProcessedEvent` sai com o status correto em relação ao limite. |
| `ProcessAsync_ForNewOrder_PersistsPaymentWithResult` | O resultado é persistido. |
| `ProcessAsync_ForNewOrder_PublishesUserEmailAndGameNeededByNotifications` | O evento leva o e-mail e o jogo, para que o Notifications não precise consultar outros serviços. |
| `ProcessAsync_ForNewOrder_PersistsBeforePublishing` | O pagamento é salvo antes da publicação. |
| `ProcessAsync_ForAlreadyProcessedOrder_DoesNotReprocessNorRepublish` | Um `OrderId` já processado não é reprocessado nem republicado (idempotência). |
| `GetByOrderIdAsync_AsOwner_ReturnsPayment` | O dono do pedido consulta o pagamento. |
| `GetByOrderIdAsync_AsAnotherUser_ReturnsForbidden` | Outro usuário não consulta o pagamento. |
| `GetByOrderIdAsync_AsAdministrator_ReturnsPaymentOfAnyUser` | O administrador consulta qualquer pagamento. |
| `GetByOrderIdAsync_WhenOrderWasNotProcessed_ReturnsNotFound` | Um pedido ainda não processado retorna `NotFound`. |

### `OrderPlacedConsumer`

| Teste | Regra verificada |
|-------|------------------|
| `Consume_WithOrderPlacedEvent_DelegatesToPaymentService` | O consumer é fino e delega a regra para o `PaymentService`. |

---

## Testes de integração

Os testes sobem a API real com `WebApplicationFactory` em um PostgreSQL do Testcontainers. O `OrderPlacedEvent` é injetado pelo MassTransit Test Harness e os testes verificam a persistência e o `PaymentProcessedEvent` publicado. O limite configurado nesta suíte é 100,00.

| Teste | Cenário ponta a ponta |
|-------|-----------------------|
| `OrderPlaced_WithPriceWithinLimit_PersistsApprovedPaymentAndPublishesApproved` | Um pedido de 59,90 é persistido como `Approved` e o evento Approved é publicado. |
| `OrderPlaced_WithPriceExactlyAtLimit_IsApproved` | Um pedido no valor do limite é aprovado. |
| `OrderPlaced_WithPriceAboveLimit_PersistsRejectedPaymentAndPublishesRejected` | Um pedido de 199,90 é persistido como `Rejected` e o evento Rejected é publicado. |
| `OrderPlaced_DeliveredTwice_ProcessesAndPublishesOnlyOnce` | Um evento duplicado gera um único registro e uma única publicação. |
| `GetPaymentByOrder_AsOwner_Returns200WithStatus` | O dono consulta o resultado do pagamento via HTTP. |
| `GetPaymentByOrder_AsAnotherUser_Returns403` | Outro usuário recebe 403. |
| `GetPaymentByOrder_WithoutToken_Returns401` | A consulta exige autenticação. |
| `GetPaymentByOrder_ForUnprocessedOrder_Returns404` | Um pedido não processado retorna 404. |
| `Health_WhenDatabaseIsReachable_Returns200` | O `/health` responde OK. |
