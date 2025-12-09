using FTPSheep.Core.Models;

namespace FTPSheep.Tests.Models;

public class ServerConnectionTests {
    [Fact]
    public void Constructor_Default_SetsDefaultValues() {
        // Act
        var connection = new ServerConnection();

        // Assert
        Assert.Equal(string.Empty, connection.Host);
        Assert.Equal(21, connection.Port);
        Assert.Equal(ProtocolType.Ftp, connection.Protocol);
        Assert.Equal(30, connection.TimeoutSeconds);
        Assert.Equal(FtpConnectionMode.Passive, connection.ConnectionMode);
        Assert.False(connection.UseSsl);
        Assert.True(connection.ValidateSslCertificate);
    }

    [Fact]
    public void Constructor_WithHost_SetsHost() {
        // Act
        var connection = new ServerConnection("ftp.example.com");

        // Assert
        Assert.Equal("ftp.example.com", connection.Host);
        Assert.Equal(21, connection.Port);
    }

    [Fact]
    public void Constructor_WithHostPortProtocol_SetsAllValues() {
        // Act
        var connection = new ServerConnection("sftp.example.com", 22, ProtocolType.Sftp);

        // Assert
        Assert.Equal("sftp.example.com", connection.Host);
        Assert.Equal(22, connection.Port);
        Assert.Equal(ProtocolType.Sftp, connection.Protocol);
    }

    [Fact]
    public void GetConnectionString_Ftp_ReturnsCorrectFormat() {
        // Arrange
        var connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp);

        // Act
        var connectionString = connection.GetConnectionString();

        // Assert
        Assert.Equal("ftp://ftp.example.com:21", connectionString);
    }

    [Fact]
    public void GetConnectionString_Ftps_ReturnsCorrectFormat() {
        // Arrange
        var connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp) {
            UseSsl = true
        };

        // Act
        var connectionString = connection.GetConnectionString();

        // Assert
        Assert.Equal("ftps://ftp.example.com:21", connectionString);
    }

    [Fact]
    public void GetConnectionString_Sftp_ReturnsCorrectFormat() {
        // Arrange
        var connection = new ServerConnection("sftp.example.com", 22, ProtocolType.Sftp);

        // Act
        var connectionString = connection.GetConnectionString();

        // Assert
        Assert.Equal("sftp://sftp.example.com:22", connectionString);
    }

    [Fact]
    public void Validate_ValidConnection_ReturnsTrue() {
        // Arrange
        var connection = new ServerConnection("ftp.example.com", 21, ProtocolType.Ftp);

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyHost_ReturnsFalse() {
        // Arrange
        var connection = new ServerConnection { Host = "" };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Server host cannot be empty"));
    }

    [Fact]
    public void Validate_InvalidPort_ReturnsFalse() {
        // Arrange
        var connection = new ServerConnection { Host = "ftp.example.com", Port = 0 };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Port") && e.Contains("invalid"));
    }

    [Fact]
    public void Validate_InvalidTimeout_ReturnsFalse() {
        // Arrange
        var connection = new ServerConnection { Host = "ftp.example.com", TimeoutSeconds = -1 };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Timeout") && e.Contains("invalid"));
    }

    [Fact]
    public void Validate_FtpWithPort22_ReturnsWarning() {
        // Arrange
        var connection = new ServerConnection {
            Host = "ftp.example.com",
            Port = 22,
            Protocol = ProtocolType.Ftp
        };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Port 22") && e.Contains("SFTP"));
    }

    [Fact]
    public void Validate_SftpWithPort21_ReturnsWarning() {
        // Arrange
        var connection = new ServerConnection {
            Host = "sftp.example.com",
            Port = 21,
            Protocol = ProtocolType.Sftp
        };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Port 21") && e.Contains("FTP"));
    }

    [Fact]
    public void Validate_SftpWithSsl_ReturnsWarning() {
        // Arrange
        var connection = new ServerConnection {
            Host = "sftp.example.com",
            Port = 22,
            Protocol = ProtocolType.Sftp,
            UseSsl = true
        };

        // Act
        var result = connection.Validate(out var errors);

        // Assert
        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("SSL/TLS") && e.Contains("SFTP"));
    }

    [Fact]
    public void NormalizePort_SftpWithPort21_SetsTo22() {
        // Arrange
        var connection = new ServerConnection {
            Protocol = ProtocolType.Sftp,
            Port = 21
        };

        // Act
        connection.NormalizePort();

        // Assert
        Assert.Equal(22, connection.Port);
    }

    [Fact]
    public void NormalizePort_FtpWithPort22_SetsTo21() {
        // Arrange
        var connection = new ServerConnection {
            Protocol = ProtocolType.Ftp,
            Port = 22
        };

        // Act
        connection.NormalizePort();

        // Assert
        Assert.Equal(21, connection.Port);
    }

    [Fact]
    public void NormalizePort_CustomPort_DoesNotChange() {
        // Arrange
        var connection = new ServerConnection {
            Protocol = ProtocolType.Ftp,
            Port = 2121
        };

        // Act
        connection.NormalizePort();

        // Assert
        Assert.Equal(2121, connection.Port);
    }
}
