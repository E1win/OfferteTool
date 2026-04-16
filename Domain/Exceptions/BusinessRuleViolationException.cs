namespace Domain.Exceptions;

public class BusinessRuleViolationException(string message) : Exception(message);
