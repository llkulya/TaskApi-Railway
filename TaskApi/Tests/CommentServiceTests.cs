using Moq;
using NUnit.Framework;
using TaskApi.Dto.Commands;
using TaskApi.Models;
using TaskApi.Repositories;
using TaskApi.Services;
using System.ComponentModel.DataAnnotations;

namespace TaskApi.Tests
{
    [TestFixture]
    public class CommentServiceTests
    {
        private Mock<ICommentRepository> _mockCommentRepo = null!;
        private ICommentService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockCommentRepo = new Mock<ICommentRepository>();
            // Переконайся, що CommentService приймає саме ці репозиторії
            _service = new CommentService(_mockCommentRepo.Object);
        }

        // Тест 1: Успішне створення коментаря
        [Test]
        public async Task AddAsync_ShouldReturnCommentDto_WhenDataIsValid()
        {
            // Arrange
            var command = new CommentCreateCommand
            {
                Text = "Тестовий коментар",
                TaskItemId = 1
            };

            var comment = new Comment
            {
                Id = 1,
                Text = "Тестовий коментар",
                TaskItemId = 1,
                CreatedDate = DateTime.UtcNow
            };

            _mockCommentRepo.Setup(r => r.AddAsync(It.IsAny<Comment>()))
                            .ReturnsAsync(comment);

            // Act
            var result = await _service.AddAsync(command);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Text, Is.EqualTo("Тестовий коментар"));
            _mockCommentRepo.Verify(r => r.AddAsync(It.IsAny<Comment>()), Times.Once);
        }

        // Тест 2: Валідація порожнього тексту (як у твоїх TaskServiceTests)
        [Test]
        public void AddAsync_ShouldThrowException_WhenTextIsEmpty()
        {
            // Arrange
            var command = new CommentCreateCommand
            {
                Text = "",
                TaskItemId = 1
            };

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(
                async () => await _service.AddAsync(command));
        }

        // Тест 3: Видалення коментаря
        [Test]
        public async Task DeleteAsync_ShouldReturnTrue_WhenCommentExists()
        {
            // Arrange
            _mockCommentRepo.Setup(r => r.DeleteAsync(1))
                            .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}