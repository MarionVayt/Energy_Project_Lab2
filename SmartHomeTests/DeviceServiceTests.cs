using System;
using System.Collections.Generic;
using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;
using Xunit;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _repoMock;
        private readonly DeviceService _deviceService;

        public DeviceServiceTests()
        {
            _repoMock = new Mock<IDeviceRepository>();
            _deviceService = new DeviceService(_repoMock.Object);
        }

        /// <summary>
        /// Перевірка: [Theory] / [InlineData] та Assert.Equal
        /// Перевіряє перемикання стану пристрою для різних вхідних прапорців (включити/виключити).
        /// </summary>
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ToggleDevice_ValidId_ReturnsExpectedState(bool turnOn, bool expected)
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = !turnOn };
            _repoMock.Setup(r => r.GetById(1)).Returns(device);

            // Act
            var actual = _deviceService.ToggleDevice(1, turnOn);

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Перевірка: Assert.True
        /// Перевіряє, що увімкнений пристрій повертає true.
        /// </summary>
        [Fact]
        public void ToggleDevice_TurnOn_ReturnsTrue()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = false };
            _repoMock.Setup(r => r.GetById(1)).Returns(device);

            // Act
            var result = _deviceService.ToggleDevice(1, true);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Перевірка: Assert.False
        /// Перевіряє, що вимкнений пристрій повертає false.
        /// </summary>
        [Fact]
        public void ToggleDevice_TurnOff_ReturnsFalse()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = true };
            _repoMock.Setup(r => r.GetById(1)).Returns(device);

            // Act
            var result = _deviceService.ToggleDevice(1, false);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Перевірка: Assert.Throws<ArgumentException>
        /// Перевіряє викидання винятку при спробі переключити неіснуючий пристрій.
        /// </summary>
        [Fact]
        public void ToggleDevice_NotFound_ThrowsArgumentException()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(99)).Returns((Device?)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _deviceService.ToggleDevice(99, true));
        }

        /// <summary>
        /// Перевірка: Verify(..., Times.Never)
        /// Переконується, що метод Update репозиторію не викликається, якщо пристрій не знайдено.
        /// </summary>
        [Fact]
        public void ToggleDevice_NotFound_NeverCallsUpdate()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(99)).Returns((Device?)null);

            // Act
            Assert.Throws<ArgumentException>(() => _deviceService.ToggleDevice(99, true));

            // Assert
            _repoMock.Verify(r => r.Update(It.IsAny<Device>()), Times.Never);
        }

        /// <summary>
        /// Перевірка: Verify(..., Times.Exactly(n)) та It.Is<T>(predicate)
        /// Перевіряє, що пристрій оновлюється в репозиторії рівно один раз із правильним станом.
        /// </summary>
        [Fact]
        public void ToggleDevice_ValidDevice_CallsUpdateExactlyOnceWithCorrectState()
        {
            // Arrange
            var device = new Device { Id = 1, IsOn = false };
            _repoMock.Setup(r => r.GetById(1)).Returns(device);

            // Act
            _deviceService.ToggleDevice(1, true);

            // Assert
            _repoMock.Verify(r => r.Update(It.Is<Device>(d => d.IsOn == true)), Times.Exactly(1));
        }

        /// <summary>
        /// Перевірка: Assert.Contains
        /// Перевіряє, що активний пристрій присутній у списку активних пристроїв.
        /// </summary>
        [Fact]
        public void GetActiveDevices_ContainsActiveDevice()
        {
            // Arrange
            var activeDevice = new Device { Id = 1, IsOn = true };
            var inactiveDevice = new Device { Id = 2, IsOn = false };
            _repoMock.Setup(r => r.GetAll()).Returns(new List<Device> { activeDevice, inactiveDevice });

            // Act
            var activeDevices = _deviceService.GetActiveDevices();

            // Assert
            Assert.Contains(activeDevice, activeDevices);
        }

        /// <summary>
        /// Перевірка: Assert.Empty та Assert.NotEmpty
        /// Перевіряє порожність та не-порожність списків активних пристроїв.
        /// </summary>
        [Fact]
        public void GetActiveDevices_CheckEmptyAndNotEmpty()
        {
            // Arrange
            var inactiveDevice = new Device { Id = 1, IsOn = false };
            var activeDevice = new Device { Id = 2, IsOn = true };

            // Act 1: Усі пристрої вимкнені
            _repoMock.Setup(r => r.GetAll()).Returns(new List<Device> { inactiveDevice });
            var resultEmpty = _deviceService.GetActiveDevices();

            // Act 2: Є увімкнений пристрій
            _repoMock.Setup(r => r.GetAll()).Returns(new List<Device> { activeDevice });
            var resultNotEmpty = _deviceService.GetActiveDevices();

            // Assert
            Assert.Empty(resultEmpty);
            Assert.NotEmpty(resultNotEmpty);
        }
    }
}