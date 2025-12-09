namespace FTPSheep.Core.Exceptions;

/// <summary>
/// Base exception for all profile-related errors.
/// </summary>
public class ProfileException : Exception {
    /// <summary>
    /// Gets the name of the profile associated with this exception, if applicable.
    /// </summary>
    public string? ProfileName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class.
    /// </summary>
    public ProfileException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProfileException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message and profile name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile associated with this exception.</param>
    public ProfileException(string message, string profileName) : base(message) {
        ProfileName = profileName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProfileException(string message, Exception innerException) : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileException"/> class with a specified error message, profile name, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile associated with this exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProfileException(string message, string profileName, Exception innerException) : base(message, innerException) {
        ProfileName = profileName;
    }
}

/// <summary>
/// Exception thrown when a requested profile cannot be found.
/// </summary>
public class ProfileNotFoundException : ProfileException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class.
    /// </summary>
    public ProfileNotFoundException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a specified profile name.
    /// </summary>
    /// <param name="profileName">The name of the profile that was not found.</param>
    public ProfileNotFoundException(string profileName)
        : base($"Profile '{profileName}' was not found.", profileName) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileNotFoundException"/> class with a specified error message and profile name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile that was not found.</param>
    public ProfileNotFoundException(string message, string profileName)
        : base(message, profileName) {
    }
}

/// <summary>
/// Exception thrown when attempting to create a profile that already exists.
/// </summary>
public class ProfileAlreadyExistsException : ProfileException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileAlreadyExistsException"/> class.
    /// </summary>
    public ProfileAlreadyExistsException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileAlreadyExistsException"/> class with a specified profile name.
    /// </summary>
    /// <param name="profileName">The name of the profile that already exists.</param>
    public ProfileAlreadyExistsException(string profileName)
        : base($"Profile '{profileName}' already exists.", profileName) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileAlreadyExistsException"/> class with a specified error message and profile name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile that already exists.</param>
    public ProfileAlreadyExistsException(string message, string profileName)
        : base(message, profileName) {
    }
}

/// <summary>
/// Exception thrown when profile validation fails.
/// </summary>
public class ProfileValidationException : ProfileException {
    /// <summary>
    /// Gets the validation errors that occurred.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileValidationException"/> class.
    /// </summary>
    public ProfileValidationException() : base() {
        ValidationErrors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileValidationException"/> class with validation errors.
    /// </summary>
    /// <param name="errors">The validation errors that occurred.</param>
    public ProfileValidationException(IEnumerable<string> errors)
        : base($"Profile validation failed: {string.Join("; ", errors)}") {
        ValidationErrors = errors.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileValidationException"/> class with a profile name and validation errors.
    /// </summary>
    /// <param name="profileName">The name of the profile that failed validation.</param>
    /// <param name="errors">The validation errors that occurred.</param>
    public ProfileValidationException(string profileName, IEnumerable<string> errors)
        : base($"Profile '{profileName}' validation failed: {string.Join("; ", errors)}", profileName) {
        ValidationErrors = errors.ToList();
    }
}

/// <summary>
/// Exception thrown when an error occurs during profile storage operations (save, load, delete).
/// </summary>
public class ProfileStorageException : ProfileException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileStorageException"/> class.
    /// </summary>
    public ProfileStorageException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileStorageException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ProfileStorageException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileStorageException"/> class with a specified error message and profile name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile associated with this exception.</param>
    public ProfileStorageException(string message, string profileName) : base(message, profileName) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileStorageException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProfileStorageException(string message, Exception innerException) : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileStorageException"/> class with a specified error message, profile name, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="profileName">The name of the profile associated with this exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ProfileStorageException(string message, string profileName, Exception innerException)
        : base(message, profileName, innerException) {
    }
}

/// <summary>
/// Exception thrown when an error occurs with global configuration operations.
/// </summary>
public class ConfigurationException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) {
    }
}
