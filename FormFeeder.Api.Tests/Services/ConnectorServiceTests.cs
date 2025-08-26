using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Services;

public class ConnectorServiceTests : TestBase
{
    private readonly ConnectorService _service;
    private readonly Mock<ILogger<ConnectorService>> _loggerMock;
    private readonly Mock<IConnectorFactory> _connectorFactoryMock;

    public ConnectorServiceTests()
    {
        _loggerMock = CreateMock<ILogger<ConnectorService>>();
        _connectorFactoryMock = CreateMock<IConnectorFactory>();
        _service = new ConnectorService(_loggerMock.Object, _connectorFactoryMock.Object);
    }

    public class ExecuteConnectorsAsync : ConnectorServiceTests
    {
        [Fact]
        public async Task ExecuteConnectorsAsync_WithNullConnectorConfigs_ShouldReturn()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            await _service.ExecuteConnectorsAsync(submission, null);

            // Assert
            _connectorFactoryMock.Verify(x => x.CreateConnector(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithEmptyConnectorConfigs_ShouldCompleteWithoutExecutingAnyConnectors()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var emptyConfigs = new List<ConnectorConfiguration>();

            // Act
            await _service.ExecuteConnectorsAsync(submission, emptyConfigs);

            // Assert
            _connectorFactoryMock.Verify(x => x.CreateConnector(It.IsAny<string>(), It.IsAny<string>()), Times.Never,
                "no connectors should be created when configuration list is empty");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithSingleEnabledConnector_ShouldSuccessfullyExecuteConnector()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var mockConnector = CreateMock<IConnector>();
            var connectorConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "test-connector", true);
            var configs = new List<ConnectorConfiguration> { connectorConfig };
            var expectedResult = ConnectorResult.Successful("Email sent successfully");

            mockConnector.Setup(x => x.ExecuteAsync(submission, connectorConfig.Settings))
                        .ReturnsAsync(expectedResult);

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "test-connector"))
                               .Returns(mockConnector.Object);

            // Act
            await _service.ExecuteConnectorsAsync(submission, configs);

            // Assert - Verify the connector was properly configured and executed
            mockConnector.VerifySet(x => x.Enabled = true, Times.Once, 
                "connector should be enabled before execution");
            mockConnector.Verify(x => x.ExecuteAsync(submission, connectorConfig.Settings), Times.Once,
                "connector should be executed with correct parameters");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithMultipleEnabledConnectors_ShouldExecuteAllConnectorsSuccessfully()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var mockConnector1 = CreateMock<IConnector>();
            var mockConnector2 = CreateMock<IConnector>();
            
            var config1 = TestDataBuilder.CreateConnectorConfiguration("MailJet", "connector-1", true);
            var config2 = TestDataBuilder.CreateConnectorConfiguration("MailJet", "connector-2", true);
            var configs = new List<ConnectorConfiguration> { config1, config2 };

            mockConnector1.Setup(x => x.ExecuteAsync(submission, config1.Settings))
                         .ReturnsAsync(ConnectorResult.Successful("Success 1"));
            mockConnector2.Setup(x => x.ExecuteAsync(submission, config2.Settings))
                         .ReturnsAsync(ConnectorResult.Successful("Success 2"));

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "connector-1")).Returns(mockConnector1.Object);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "connector-2")).Returns(mockConnector2.Object);

            // Act
            await _service.ExecuteConnectorsAsync(submission, configs);

            // Assert - Verify both connectors were executed with their respective configurations
            mockConnector1.Verify(x => x.ExecuteAsync(submission, config1.Settings), Times.Once,
                "first connector should be executed with its settings");
            mockConnector2.Verify(x => x.ExecuteAsync(submission, config2.Settings), Times.Once,
                "second connector should be executed with its settings");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithDisabledConnector_ShouldSkipConnector()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var disabledConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "disabled-connector", false);
            var configs = new List<ConnectorConfiguration> { disabledConfig };

            // Act
            await _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            _connectorFactoryMock.Verify(x => x.CreateConnector(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithMixedEnabledDisabledConnectors_ShouldOnlyExecuteEnabled()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var mockConnector = CreateMock<IConnector>();
            
            var enabledConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "enabled-connector", true);
            var disabledConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "disabled-connector", false);
            var configs = new List<ConnectorConfiguration> { enabledConfig, disabledConfig };

            mockConnector.Setup(x => x.ExecuteAsync(It.IsAny<FormSubmission>(), It.IsAny<Dictionary<string, object>?>()))
                        .ReturnsAsync(ConnectorResult.Successful("Success"));

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "enabled-connector")).Returns(mockConnector.Object);

            // Act
            await _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            _connectorFactoryMock.Verify(x => x.CreateConnector("MailJet", "enabled-connector"), Times.Once);
            _connectorFactoryMock.Verify(x => x.CreateConnector("MailJet", "disabled-connector"), Times.Never);
            mockConnector.Verify(x => x.ExecuteAsync(submission, enabledConfig.Settings), Times.Once);
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WithUnsupportedConnectorType_ShouldSkipConnectorCreationAndExecution()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var unsupportedConfig = TestDataBuilder.CreateConnectorConfiguration("UnsupportedType", "test-connector", true);
            var configs = new List<ConnectorConfiguration> { unsupportedConfig };

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("UnsupportedType")).Returns(false);

            // Act
            await _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            _connectorFactoryMock.Verify(x => x.CreateConnector(It.IsAny<string>(), It.IsAny<string>()), Times.Never,
                "no connectors should be created for unsupported types");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WhenConnectorCreationReturnsNull_ShouldContinueWithoutExecuting()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var connectorConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "test-connector", true);
            var configs = new List<ConnectorConfiguration> { connectorConfig };

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "test-connector")).Returns((IConnector?)null);

            // Act
            var act = () => _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            await act.Should().NotThrowAsync("service should handle null connector creation gracefully");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WhenConnectorExecutionFails_ShouldContinueWithoutThrowing()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var mockConnector = CreateMock<IConnector>();
            var connectorConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "test-connector", true);
            var configs = new List<ConnectorConfiguration> { connectorConfig };

            var failureResult = ConnectorResult.Failed("Connection failed", new Exception("Network error"));
            mockConnector.Setup(x => x.ExecuteAsync(submission, connectorConfig.Settings))
                        .ReturnsAsync(failureResult);

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "test-connector")).Returns(mockConnector.Object);

            // Act
            var act = () => _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            await act.Should().NotThrowAsync("service should handle connector execution failures gracefully");
        }

        [Fact]
        public async Task ExecuteConnectorsAsync_WhenConnectorThrowsException_ShouldContinueWithoutPropagatingException()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var mockConnector = CreateMock<IConnector>();
            var connectorConfig = TestDataBuilder.CreateConnectorConfiguration("MailJet", "test-connector", true);
            var configs = new List<ConnectorConfiguration> { connectorConfig };

            var exception = new InvalidOperationException("Connector failed");
            mockConnector.Setup(x => x.ExecuteAsync(submission, connectorConfig.Settings))
                        .ThrowsAsync(exception);

            _connectorFactoryMock.Setup(x => x.IsConnectorTypeSupported("MailJet")).Returns(true);
            _connectorFactoryMock.Setup(x => x.CreateConnector("MailJet", "test-connector")).Returns(mockConnector.Object);

            // Act
            var act = () => _service.ExecuteConnectorsAsync(submission, configs);

            // Assert
            await act.Should().NotThrowAsync("service should handle connector exceptions gracefully and not propagate them");
        }
    }
}