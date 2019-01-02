using System;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

/// <summary>
///     Helper class for guard statements, that allow prettier code for guard clauses
/// </summary>
/// <example>
///     Sample usage:
///     <code>
/// <![CDATA[
/// Guard.Against(name.Length == 0).With<ArgumentException>("Name must have at least 1 char length");
/// Guard.AgainstNull(obj, "obj");
/// Guard.AgainstNullOrEmpty(name, "name", "Name must have a value");
/// ]]>
/// </code>
/// </example>
public static class Guard
{
    public enum TrimOptions
    {
        DoTrim = 0,
        NoTrim = 1
    }

    /// <summary>
    ///     Checks the supplied condition and act with exception if condition resolves to <c>true</c>.
    /// </summary>
    public static Act Against(bool assertion)
    {
        return new Act(assertion);
    }

    /// <summary>
    ///     Checks the value of the supplied <paramref name="value" /> and throws an
    ///     <see cref="System.ArgumentNullException" /> if it is <see langword="null" />.
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <param name="variableName">The variable name.</param>
    /// <exception cref="System.ArgumentNullException">
    ///     If the supplied <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [DebuggerStepThrough]
    [ContractAnnotation("halt <= value:null")]
    public static void AgainstNull<T>([NoEnumeration] this T value, [InvokerParameterName] string variableName)
    {
        AgainstNull(value, variableName,
            string.Format(CultureInfo.InvariantCulture, "'{0}' cannot be null.", variableName));
    }

    /// <summary>
    ///     Checks the value of the supplied <paramref name="value" /> and throws an
    ///     <see cref="System.ArgumentNullException" /> if it is <see langword="null" />.
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <param name="variableName">The variable name.</param>
    /// <param name="message">The message to include in exception description</param>
    /// <exception cref="System.ArgumentNullException">
    ///     If the supplied <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [DebuggerStepThrough]
    [ContractAnnotation("halt <= value:null")]
    public static void AgainstNull<T>([NoEnumeration] T value, [InvokerParameterName] string variableName,
        string message)
    {
        if (value == null)
            throw new ArgumentNullException(variableName, message);
    }

    /// <summary>
    ///     Checks the value of the supplied string <paramref name="value" /> and throws an
    ///     <see cref="System.ArgumentException" /> if it is <see langword="null" /> or contains only whitespace character(s).
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="variableName">The variable name.</param>
    /// <exception cref="System.ArgumentException">
    ///     If the supplied <paramref name="value" /> is <see langword="null" /> or contains only whitespace character(s).
    /// </exception>
    [ContractAnnotation("halt <= value:null")]
    public static void AgainstNullOrEmpty(this string value, [InvokerParameterName] string variableName = null)
    {
        AgainstNullOrEmpty(value, variableName, TrimOptions.DoTrim);
    }

    /// <summary>
    ///     Checks the value of the supplied string <paramref name="value" /> and throws an
    ///     <see cref="System.ArgumentException" /> if it is <see langword="null" /> or empty.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="variableName">The argument name.</param>
    /// <param name="options">The value trimming options.</param>
    /// <exception cref="System.ArgumentException">
    ///     If the supplied <paramref name="value" /> is <see langword="null" /> or empty.
    /// </exception>
    [ContractAnnotation("halt <= value:null")]
    public static void AgainstNullOrEmpty(this string value, [InvokerParameterName] string variableName,
        TrimOptions options)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "'{0}' cannot be null or resolve to an empty string : '{1}'.", variableName, value);

        AgainstNullOrEmpty(value, variableName, message, options);
    }

    /// <summary>
    ///     Checks the value of the supplied string <paramref name="value" /> and throws an
    ///     <see cref="System.ArgumentException" /> if it is <see langword="null" /> or empty.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="variableName">The variable name.</param>
    /// <param name="message">The message to include in exception description</param>
    /// <param name="options">The value trimming options.</param>
    /// <exception cref="System.ArgumentException">
    ///     If the supplied <paramref name="value" /> is <see langword="null" /> or empty.
    /// </exception>
    [ContractAnnotation("halt <= value:null")]
    public static void AgainstNullOrEmpty(this string value, [InvokerParameterName] string variableName, string message,
        TrimOptions options)
    {
        if (value != null && options == TrimOptions.DoTrim)
            value = value.Trim();

        if (string.IsNullOrEmpty(value))
            throw new ArgumentException(message, variableName);
    }

    public static void AgainstLessZerro(this int value, [InvokerParameterName] string variableName)
    {
        Against(value < 0).With<ArgumentException>(string.Format("{0} cannot be negative.", variableName));
    }

    public static void AgainstLessOrEqualsZerro(this int value, [InvokerParameterName] string variableName)
    {
        Against(value <= 0).With<ArgumentException>(string.Format("{0} cannot be negative or zerro.", variableName));
    }

    public static void AgainstLessZerro(this long value, [InvokerParameterName] string variableName)
    {
        Against(value < 0).With<ArgumentException>(string.Format("{0} cannot be negative.", variableName));
    }

    public static void AgainstLessOrEqualsZerro(this long value, [InvokerParameterName] string variableName)
    {
        Against(value <= 0).With<ArgumentException>(string.Format("{0} cannot be negative or zerro.", variableName));
    }

    /// <summary>
    ///     Represents action taken when assertion is true
    /// </summary>
    public class Act
    {
        private readonly bool _assertion;

        internal Act(bool assertion)
        {
            _assertion = assertion;
        }

        /// <summary>
        ///     Will throw an exception of type <typeparamref name="TException" />
        ///     with the specified message if the "Against" assertion is true
        /// </summary>
        /// <typeparam name="TException">Exception type</typeparam>
        /// <param name="message">Exception message</param>
        public void With<TException>(string message) where TException : Exception
        {
            if (_assertion)
                throw (TException) Activator.CreateInstance(typeof(TException), message);
        }
    }
}