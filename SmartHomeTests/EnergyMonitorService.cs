using System.Collections.Generic;
using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;
using Xunit;

namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepoMock;
        private readonly Mock<IEnergyPlanRepository> _planRepoMock;
        private readonly Mock<INotificationService> _notifyMock;
        private readonly EnergyMonitorService _monitorService;

        public EnergyMonitorServiceTests()
        {
            _deviceRepoMock = new Mock<IDeviceRepository>();
            _planRepoMock = new Mock<IEnergyPlanRepository>();
            _notifyMock = new Mock<INotificationService>();

            _monitorService = new EnergyMonitorService(
                _deviceRepoMock.Object,
                _planRepoMock.Object,
                _notifyMock.Object);
        }

        /// <summary>
        /// Перевірка: Assert.Equal
        /// Перевіряє точний розрахунок поточного споживання електроенергії у кВт·год.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_CalculatesCorrectSum()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, IsOn = true, PowerUsageWatts = 1500 },
                new Device { Id = 2, IsOn = true, PowerUsageWatts = 500 },
                new Device { Id = 3, IsOn = false, PowerUsageWatts = 2000 }
            };
            _deviceRepoMock.Setup(r => r.GetAll()).Returns(devices);

            // Act
            var actualKwh = _monitorService.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(2.0, actualKwh); // (1500 + 500) / 1000 = 2.0
        }

        /// <summary>
        /// Перевірка: Assert.NotNull
        /// Перевіряє коректне отримання поточного тарифного плану з репозиторію.
        /// </summary>
        [Fact]
        public void GetCurrentPlan_ReturnsNotNullObject()
        {
            // Arrange
            _planRepoMock.Setup(r => r.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = 10 });

            // Act
            var plan = _planRepoMock.Object.GetCurrentPlan();

            // Assert
            Assert.NotNull(plan);
        }

        /// <summary>
        /// Перевірка: Verify, It.IsAny<string> та It.Is<string>(predicate)
        /// Перевіряє надсилання сповіщення про перевантаження з відповідним текстом.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageExceedsLimit_SendsAlertWithOverloadMessage()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, IsOn = true, PowerUsageWatts = 5000 } // 5.0 kWh
            };
            var plan = new EnergyPlan { DailyLimitKwh = 3.0 };

            _deviceRepoMock.Setup(r => r.GetAll()).Returns(devices);
            _planRepoMock.Setup(r => r.GetCurrentPlan()).Returns(plan);

            // Act
            _monitorService.CheckForOverload();

            // Assert
            _notifyMock.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Once);
            _notifyMock.Verify(n => n.SendAlert(It.Is<string>(msg => msg.Contains("Overload detected"))), Times.Once);
        }

        /// <summary>
        /// Перевірка: Verify(..., Times.Never)
        /// Переконується, що сповіщення НЕ надсилається, якщо споживання не перевищує ліміт.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageWithinLimit_NeverSendsAlert()
        {
            // Arrange
            var devices = new List<Device>
            {
                new Device { Id = 1, IsOn = true, PowerUsageWatts = 1000 } // 1.0 kWh
            };
            var plan = new EnergyPlan { DailyLimitKwh = 5.0 };

            _deviceRepoMock.Setup(r => r.GetAll()).Returns(devices);
            _planRepoMock.Setup(r => r.GetCurrentPlan()).Returns(plan);

            // Act
            _monitorService.CheckForOverload();

            // Assert
            _notifyMock.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Перевірка: Assert.NotEqual та Verify(..., Times.AtLeastOnce)
        /// Перевіряє оновлення ліміту споживання та збереження нового плану.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_ValidLimit_UpdatesAndSavesPlan()
        {
            // Arrange
            var plan = new EnergyPlan { DailyLimitKwh = 5.0 };
            _planRepoMock.Setup(r => r.GetCurrentPlan()).Returns(plan);

            // Act
            _monitorService.UpdateEnergyLimit(10.0);

            // Assert
            Assert.NotEqual(5.0, plan.DailyLimitKwh);
            Assert.Equal(10.0, plan.DailyLimitKwh);
            _planRepoMock.Verify(r => r.UpdatePlan(It.Is<EnergyPlan>(p => p.DailyLimitKwh == 10.0)), Times.AtLeastOnce);
        }
    }
}