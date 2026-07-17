// InvoiceServiceTests — invoice numbering (per-year sequence), GST/tax math,
// and the fixed fake GST placeholder.

using ExamPlatform.BLL.Models;
using ExamPlatform.BLL.Services;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ExamPlatform.BLL.Tests.Services
{
    [TestFixture]
    public class InvoiceServiceTests
    {
        private Mock<IInvoiceRepository> _invoiceRepoMock = null!;
        private InvoiceService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _invoiceRepoMock = new Mock<IInvoiceRepository>();
            _invoiceRepoMock.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice i) => i);
            _invoiceRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            _sut = new InvoiceService(_invoiceRepoMock.Object);
        }

        [Test]
        public async Task GenerateAsync_FirstInvoiceOfYear_NumberedOne()
        {
            _invoiceRepoMock.Setup(r => r.GetCountForYearAsync(2026)).ReturnsAsync(0);

            var invoice = await _sut.GenerateAsync(new InvoiceRequest(
                PaymentId: 1, Subtotal: 1000m, DiscountAmount: 0m,
                BillingName: "Test User", BillingEmail: "test@example.com",
                Now: new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc)));

            invoice.InvoiceNumber.Should().Be("INV-2026-000001");
        }

        [Test]
        public async Task GenerateAsync_LaterInvoiceOfYear_IncrementsSequence()
        {
            _invoiceRepoMock.Setup(r => r.GetCountForYearAsync(2026)).ReturnsAsync(41);

            var invoice = await _sut.GenerateAsync(new InvoiceRequest(
                PaymentId: 2, Subtotal: 1000m, DiscountAmount: 0m,
                BillingName: "Test User", BillingEmail: "test@example.com",
                Now: new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc)));

            invoice.InvoiceNumber.Should().Be("INV-2026-000042");
        }

        [Test]
        public async Task GenerateAsync_AppliesFlatGstOnDiscountedSubtotal()
        {
            _invoiceRepoMock.Setup(r => r.GetCountForYearAsync(It.IsAny<int>())).ReturnsAsync(0);

            var invoice = await _sut.GenerateAsync(new InvoiceRequest(
                PaymentId: 3, Subtotal: 1000m, DiscountAmount: 250m,
                BillingName: "Test User", BillingEmail: "test@example.com",
                Now: DateTime.UtcNow));

            // Taxable = 750; GST 18% = 135; Total = 885.
            invoice.TaxAmount.Should().Be(135m);
            invoice.Total.Should().Be(885m);
            invoice.DiscountAmount.Should().Be(250m);
        }

        [Test]
        public async Task GenerateAsync_NeverGeneratesARealLookingGstNumber()
        {
            _invoiceRepoMock.Setup(r => r.GetCountForYearAsync(It.IsAny<int>())).ReturnsAsync(0);

            var invoice = await _sut.GenerateAsync(new InvoiceRequest(
                PaymentId: 4, Subtotal: 500m, DiscountAmount: 0m,
                BillingName: "Test User", BillingEmail: "test@example.com",
                Now: DateTime.UtcNow));

            invoice.FakeGstNumber.Should().Be("29AAAAA0000EXM1Z5");
        }

        [Test]
        public async Task GenerateAsync_PersistsAndSaves()
        {
            _invoiceRepoMock.Setup(r => r.GetCountForYearAsync(It.IsAny<int>())).ReturnsAsync(0);

            await _sut.GenerateAsync(new InvoiceRequest(
                PaymentId: 5, Subtotal: 500m, DiscountAmount: 0m,
                BillingName: "Test User", BillingEmail: "test@example.com",
                Now: DateTime.UtcNow));

            _invoiceRepoMock.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Once);
            _invoiceRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
