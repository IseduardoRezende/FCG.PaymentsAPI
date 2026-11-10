using FCG.Contracts.Events;
using FCG.Payments.Application.Abstractions;
using FCG.Payments.Application.DTOs;
using FCG.Payments.Application.Services;
using FCG.Payments.Domain.Commons.Result;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Repositories;
using FCG.Payments.Domain.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ContractPaymentStatus = FCG.Contracts.Events.PaymentStatus;
using DomainPaymentStatus = FCG.Payments.Domain.Enums.PaymentStatus;

namespace FCG.Payments.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService(
            _paymentRepository.Object,
            _eventPublisher.Object,
            _currentUser.Object,
            Options.Create(new PaymentSettings { MaxApprovedAmount = 100m }),
            NullLogger<PaymentService>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static OrderPlacedEvent Order(decimal price) => new(1, 2, "player@test.com", 3, "Celeste", price);

    private void SetupExistingPayment() =>
        _paymentRepository.Setup(x => x.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = 9, OrderId = 1, UserId = 2, Status = DomainPaymentStatus.Approved });

    [Theory]
    [InlineData(59.90, ContractPaymentStatus.Approved)]
    [InlineData(100.00, ContractPaymentStatus.Approved)]
    [InlineData(100.01, ContractPaymentStatus.Rejected)]
    [InlineData(199.90, ContractPaymentStatus.Rejected)]
    public async Task ProcessAsync_ForNewOrder_PublishesPaymentProcessedEventWithStatusByLimit(decimal price, ContractPaymentStatus expected)
    {
        var processed = await _paymentService.ProcessAsync(Order(price), Ct);

        Assert.True(processed);
        _eventPublisher.Verify(x => x.PublishAsync(
            It.Is<PaymentProcessedEvent>(e => e.OrderId == 1 && e.Status == expected && e.Price == price),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ForNewOrder_PersistsPaymentWithResult()
    {
        Payment? persisted = null;
        _paymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((payment, _) => persisted = payment).Returns(Task.CompletedTask);

        await _paymentService.ProcessAsync(Order(150m), Ct);

        Assert.Equal(1, persisted!.OrderId);
        Assert.Equal(150m, persisted.Amount);
        Assert.Equal(DomainPaymentStatus.Rejected, persisted.Status);
        _paymentRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ForNewOrder_PublishesUserEmailAndGameNeededByNotifications()
    {
        await _paymentService.ProcessAsync(Order(10m), Ct);

        _eventPublisher.Verify(x => x.PublishAsync(
            It.Is<PaymentProcessedEvent>(e => e.UserId == 2 && e.UserEmail == "player@test.com" && e.GameId == 3 && e.GameName == "Celeste"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ForNewOrder_PersistsBeforePublishing()
    {
        var calls = new List<string>();
        _paymentRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save")).Returns(Task.CompletedTask);
        _eventPublisher.Setup(x => x.PublishAsync(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("publish")).Returns(Task.CompletedTask);

        await _paymentService.ProcessAsync(Order(10m), Ct);

        Assert.Equal(["save", "publish"], calls);
    }

    [Fact]
    public async Task ProcessAsync_ForAlreadyProcessedOrder_DoesNotReprocessNorRepublish()
    {
        _paymentRepository.Setup(x => x.ExistsByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var processed = await _paymentService.ProcessAsync(Order(10m), Ct);

        Assert.False(processed);
        _paymentRepository.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(x => x.PublishAsync(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrderIdAsync_AsOwner_ReturnsPayment()
    {
        _currentUser.Setup(x => x.UserId).Returns(2);
        SetupExistingPayment();

        var result = await _paymentService.GetByOrderIdAsync(1, Ct);

        Assert.IsType<SuccessResult<ReadPaymentDto>>(result);
        Assert.Equal(DomainPaymentStatus.Approved, result.Value!.Status);
    }

    [Fact]
    public async Task GetByOrderIdAsync_AsAnotherUser_ReturnsForbidden()
    {
        _currentUser.Setup(x => x.UserId).Returns(99);
        SetupExistingPayment();

        var result = await _paymentService.GetByOrderIdAsync(1, Ct);

        Assert.IsType<ForbiddenResult<ReadPaymentDto>>(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_AsAdministrator_ReturnsPaymentOfAnyUser()
    {
        _currentUser.Setup(x => x.UserId).Returns(99);
        _currentUser.Setup(x => x.IsAdministrator).Returns(true);
        SetupExistingPayment();

        var result = await _paymentService.GetByOrderIdAsync(1, Ct);

        Assert.IsType<SuccessResult<ReadPaymentDto>>(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenOrderWasNotProcessed_ReturnsNotFound()
    {
        var result = await _paymentService.GetByOrderIdAsync(404, Ct);

        Assert.IsType<NotFoundResult<ReadPaymentDto>>(result);
    }
}
