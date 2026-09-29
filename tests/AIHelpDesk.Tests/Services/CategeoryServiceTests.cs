using AIHelpDesk.Application.Dtos.Categeory;
using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpDesk.Tests.Services
{
    public class CategeoryServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, CategeoryService Service, Mock<IMapper> MapperMock)> CreateTestContextAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var mapperMock = new Mock<IMapper>();
            var service = new CategeoryService(context, mapperMock.Object);

            return (connection, context, service, mapperMock);
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldCreateCategorySuccessfully()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var dto = new CreateCategeoryDto { Name = "Technical Issue", Description = "Technical problems." };
            var expectedDto = new CategeoryDetailsDto { Name = dto.Name, Description = dto.Description, IsActive = true };

            test.MapperMock.Setup(mapper => mapper.Map<CategeoryDetailsDto>(It.IsAny<Category>())).Returns(expectedDto);

            // Act
            var result = await test.Service.CreateCategeoryAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Technical Issue", result.Name);
            Assert.Equal("Technical problems.", result.Description);
            Assert.True(result.IsActive);

            var category = await context.Categories.FirstOrDefaultAsync();
            Assert.NotNull(category);
            Assert.Equal("Technical Issue", category.Name);
            Assert.True(category.IsActive);
        }

        [Fact]
        public async Task CreateCategoryAsync_WhenCategoryNameAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var existingCategory = new Category("Technical Issue", "Technical problems.");
            context.Categories.Add(existingCategory);
            await context.SaveChangesAsync();

            var dto = new CreateCategeoryDto { Name = "Technical Issue", Description = "Another description." };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.CreateCategeoryAsync(dto));
        }

        [Fact]
        public async Task CreateCategoryAsync_WhenCategoryNameDiffersOnlyByCase_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var existingCategory = new Category("Technical Issue", "Technical problems.");
            context.Categories.Add(existingCategory);
            await context.SaveChangesAsync();

            var dto = new CreateCategeoryDto { Name = "technical issue", Description = "Another description." };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.CreateCategeoryAsync(dto));
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WhenCategoryExistsAndIsActive_ShouldReturnCategory()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var category = new Category("Billing", "Billing problems.");
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var expectedDto = new CategeoryDetailsDto { Id = category.Id, Name = category.Name, Description = category.Description, IsActive = true };

            test.MapperMock.Setup(mapper => mapper.Map<CategeoryDetailsDto>(It.IsAny<Category>())).Returns(expectedDto);

            // Act
            var result = await test.Service.GetCategeoryByIdAsync(category.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(category.Id, result.Id);
            Assert.Equal("Billing", result.Name);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var result = await test.Service.GetCategeoryByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WhenCategoryIsInactive_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var category = new Category("Billing", "Billing problems.");
            category.Deactivate();

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            // Act
            var result = await test.Service.GetCategeoryByIdAsync(category.Id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveCategoriesAsync_ShouldReturnOnlyActiveCategories()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var activeCategory1 = new Category("Billing", "Billing problems.");
            var activeCategory2 = new Category("Technical Issue", "Technical problems.");
            var inactiveCategory = new Category("Old Category", "Old category.");
            inactiveCategory.Deactivate();

            context.Categories.AddRange(activeCategory1, activeCategory2, inactiveCategory);
            await context.SaveChangesAsync();

            var expectedDtos = new List<CategeoryDetailsDto>
            {
                new CategeoryDetailsDto { Id = activeCategory1.Id, Name = activeCategory1.Name, IsActive = true },
                new CategeoryDetailsDto { Id = activeCategory2.Id, Name = activeCategory2.Name, IsActive = true }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<CategeoryDetailsDto>>(It.IsAny<IEnumerable<Category>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetActiveCategeoriesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, category => Assert.True(category.IsActive));
            Assert.DoesNotContain(result, category => category.Name == "Old Category");
        }

        [Fact]
        public async Task GetActiveCategoriesAsync_WhenNoActiveCategories_ShouldReturnEmptyList()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var inactiveCategory = new Category("Old Category", "Old category.");
            inactiveCategory.Deactivate();

            context.Categories.Add(inactiveCategory);
            await context.SaveChangesAsync();

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<CategeoryDetailsDto>>(It.IsAny<IEnumerable<Category>>())).Returns(new List<CategeoryDetailsDto>());

            // Act
            var result = await test.Service.GetActiveCategeoriesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActiveCategoriesAsync_ShouldReturnCategoriesOrderedByName()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var technicalCategory = new Category("Technical Issue", "Technical problems.");
            var billingCategory = new Category("Billing", "Billing problems.");
            var accountCategory = new Category("Account", "Account problems.");

            context.Categories.AddRange(technicalCategory, billingCategory, accountCategory);
            await context.SaveChangesAsync();

            var expectedDtos = new List<CategeoryDetailsDto>
            {
                new CategeoryDetailsDto { Id = accountCategory.Id, Name = "Account", IsActive = true },
                new CategeoryDetailsDto { Id = billingCategory.Id, Name = "Billing", IsActive = true },
                new CategeoryDetailsDto { Id = technicalCategory.Id, Name = "Technical Issue", IsActive = true }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<CategeoryDetailsDto>>(It.IsAny<IEnumerable<Category>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetActiveCategeoriesAsync();
            var resultList = result.ToList();

            // Assert
            Assert.Equal(3, resultList.Count);
            Assert.Equal("Account", resultList[0].Name);
            Assert.Equal("Billing", resultList[1].Name);
            Assert.Equal("Technical Issue", resultList[2].Name);
        }
    }
}
