using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.Categeory;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Controllers
{
    public class CategeoryControllerTests
    {
        [Fact]
        public async Task GetActiveCategeories_ShouldReturnOk()
        {
            var expected = new List<CategeoryDetailsDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Billing"
                }
            };

            var serviceMock = new Mock<ICategeoryService>();

            serviceMock.Setup(x => x.GetActiveCategeoriesAsync())
                       .ReturnsAsync(expected);

            var controller = new CategeoryController(serviceMock.Object);

            var result = await controller.GetActiveCategeories();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);

            serviceMock.Verify(x => x.GetActiveCategeoriesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCategeory_ShouldReturnOk_WhenCategeoryExists()
        {
            var id = Guid.NewGuid();

            var expected = new CategeoryDetailsDto
            {
                Id = id,
                Name = "Billing"
            };

            var serviceMock = new Mock<ICategeoryService>();

            serviceMock.Setup(x => x.GetCategeoryByIdAsync(id))
                       .ReturnsAsync(expected);

            var controller = new CategeoryController(serviceMock.Object);

            var result = await controller.GetCategeory(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);

            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task GetCategeory_ShouldReturnNotFound_WhenCategeoryDoesNotExist()
        {
            var id = Guid.NewGuid();

            var serviceMock = new Mock<ICategeoryService>();

            serviceMock.Setup(x => x.GetCategeoryByIdAsync(id))
                       .ReturnsAsync((CategeoryDetailsDto?)null);

            var controller = new CategeoryController(serviceMock.Object);

            var result = await controller.GetCategeory(id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateCategeory_ShouldReturnCreated()
        {
            var dto = new CreateCategeoryDto
            {
                Name = "Technical Issue",
                Description = "Technical problems."
            };

            var expected = new CategeoryDetailsDto
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description
            };

            var serviceMock = new Mock<ICategeoryService>();

            serviceMock.Setup(x => x.CreateCategeoryAsync(dto))
                       .ReturnsAsync(expected);

            var controller = new CategeoryController(serviceMock.Object);

            var result = await controller.CreateCategeory(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

            Assert.Equal(nameof(CategeoryController.GetCategeory), createdResult.ActionName);

            Assert.Same(expected, createdResult.Value);
        }
    }
}
